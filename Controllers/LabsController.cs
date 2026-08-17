using AI.MedicalCouncil.Data;
using AI.MedicalCouncil.Models;
using AI.MedicalCouncil.Services.Labs;
using AI.MedicalCouncil.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.MedicalCouncil.Controllers;

public class LabsController(
    AppDbContext db,
    ILabDocumentAnalyzer analyzer,
    IWebHostEnvironment env,
    ILogger<LabsController> logger) : Controller
{
    private const long MaxBytes = 15 * 1024 * 1024;

    private static readonly string[] Allowed =
        { ".pdf", ".txt", ".csv", ".json", ".png", ".jpg", ".jpeg", ".webp" };

    public async Task<IActionResult> Index(int patientId)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
        if (patient is null) return NotFound();

        return View(new LabsIndexVm
        {
            Patient = patient,
            Documents = await db.LabDocuments
                .Where(d => d.PatientId == patientId)
                .OrderByDescending(d => d.CollectedAtUtc)
                .ToListAsync(),
            RecentResults = await db.LabResults
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.ObservedAtUtc)
                .Take(40)
                .ToListAsync()
        });
    }

    /// <summary>
    /// Drag and drop target. Accepts the file, runs extraction, writes the analytes straight into the
    /// database and creates a matching encounter so the council can pick the panel up immediately.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> Upload(int patientId, IFormFile? file, bool autoCouncil = true, CancellationToken ct = default)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == patientId, ct);
        if (patient is null) return NotFound();

        if (file is null || file.Length == 0)
            return Json(new { ok = false, message = "Fayl tanlanmadi." });

        if (file.Length > MaxBytes)
            return Json(new { ok = false, message = "Fayl hajmi 15 MB dan oshmasligi kerak." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext))
            return Json(new { ok = false, message = $"Qo'llab-quvvatlanmaydigan format: {ext}. Ruxsat: PDF, PNG, JPG, TXT, CSV." });

        byte[] bytes;
        await using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }

        var folder = Path.Combine(env.WebRootPath, "uploads", patientId.ToString());
        Directory.CreateDirectory(folder);
        var stored = $"{Guid.NewGuid():N}{ext}";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(folder, stored), bytes, ct);

        var document = new LabDocument
        {
            PatientId = patientId,
            FileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType ?? "application/octet-stream",
            SizeBytes = file.Length,
            StoredPath = $"/uploads/{patientId}/{stored}",
            Status = "Tahlil qilinmoqda"
        };

        try
        {
            var analysis = await analyzer.AnalyzeAsync(bytes, file.FileName, document.ContentType, patient, ct);

            document.CollectedAtUtc = analysis.CollectedAtUtc ?? DateTime.UtcNow;
            document.RawText = analysis.RawText;
            document.Summary = analysis.Summary;
            document.ExtractionSource = analysis.Source;
            document.DurationMs = analysis.DurationMs;
            document.ExtractedCount = analysis.Results.Count;
            document.AbnormalCount = analysis.Results.Count(r => r.Flag != "N");
            document.Status = analysis.Results.Count > 0 ? "Tayyor" : "Xatolik";

            foreach (var r in analysis.Results)
            {
                r.PatientId = patientId;
                r.ObservedAtUtc = document.CollectedAtUtc;
            }
            document.Results = analysis.Results;

            db.LabDocuments.Add(document);
            await db.SaveChangesAsync(ct);

            // Map the panel onto an encounter so it lands on the timeline at the collection date.
            var encounter = BuildEncounterFromPanel(patientId, document);
            if (encounter is not null)
            {
                db.Encounters.Add(encounter);
                await db.SaveChangesAsync(ct);

                document.EncounterId = encounter.Id;
                foreach (var r in document.Results) r.ObservedAtUtc = encounter.OccurredAtUtc;
            }

            db.AuditLogs.Add(new AuditLog
            {
                Action = "LAB_UPLOAD",
                Entity = "LabDocument",
                Details = $"Bemor #{patientId}, fayl \"{document.FileName}\", {document.ExtractedCount} ko'rsatkich, manba: {document.ExtractionSource}."
            });
            await db.SaveChangesAsync(ct);

            return Json(new
            {
                ok = true,
                documentId = document.Id,
                extracted = document.ExtractedCount,
                abnormal = document.AbnormalCount,
                critical = document.Results.Count(r => r.Flag == "C"),
                source = document.ExtractionSource,
                summary = document.Summary,
                durationMs = document.DurationMs,
                documentUrl = Url.Action(nameof(Document), "Labs", new { id = document.Id }),
                councilUrl = autoCouncil && encounter is not null
                    ? Url.Action("Live", "Council", new { patientId, encounterId = encounter.Id })
                    : null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lab upload failed for patient {PatientId}", patientId);
            return Json(new { ok = false, message = "Faylni tahlil qilishda xatolik yuz berdi." });
        }
    }

    public async Task<IActionResult> Document(int id)
    {
        var document = await db.LabDocuments
            .Include(d => d.Results)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (document is null) return NotFound();

        var patient = await db.Patients.FirstAsync(p => p.Id == document.PatientId);

        return View(new LabDocumentVm
        {
            Patient = patient,
            Document = document,
            Results = document.Results.OrderBy(r => r.Analyte).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var document = await db.LabDocuments.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null) return NotFound();

        var patientId = document.PatientId;
        db.LabDocuments.Remove(document);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { patientId });
    }

    /// <summary>Maps the analytes this system already understands onto encounter fields.</summary>
    private static Encounter? BuildEncounterFromPanel(int patientId, LabDocument document)
    {
        if (document.Results.Count == 0) return null;

        double? Value(string analyte) =>
            document.Results.FirstOrDefault(r => r.Analyte.Equals(analyte, StringComparison.OrdinalIgnoreCase))?.Value;

        var hb = Value("Gemoglobin");
        var glucose = Value("Glyukoza");
        if (hb is null && glucose is null) return null;

        var abnormal = document.Results.Where(r => r.Flag != "N").Select(r => r.Analyte).Take(6).ToList();

        return new Encounter
        {
            PatientId = patientId,
            OccurredAtUtc = document.CollectedAtUtc,
            Symptoms = $"Laboratoriya paneli: {document.FileName}",
            Anamnesis = abnormal.Count > 0
                ? $"Me'yordan chetda: {string.Join(", ", abnormal)}."
                : "Barcha ajratilgan ko'rsatkichlar referens oralig'ida.",
            Hemoglobin = hb,
            Glucose = glucose,
            Notes = document.Summary,
            Triage = document.Results.Any(r => r.Flag == "C") ? "Qizil"
                : document.AbnormalCount > 0 ? "Sariq" : "Yashil",
            SourceLabDocumentId = document.Id
        };
    }
}
