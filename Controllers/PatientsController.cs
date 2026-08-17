using AI.MedicalCouncil.Data;
using AI.MedicalCouncil.Services;
using AI.MedicalCouncil.Models;
using AI.MedicalCouncil.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.MedicalCouncil.Controllers;

public class PatientsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        var query = db.Patients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => EF.Functions.ILike(p.FullName, $"%{q}%") || (p.Phone != null && p.Phone.Contains(q)));

        ViewBag.Query = q;
        return View(await query.OrderBy(p => p.FullName).ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new Patient
    {
        BirthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-30))
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Patient patient)
    {
        if (!ModelState.IsValid) return View(patient);

        patient.CreatedAtUtc = DateTime.UtcNow;
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        db.AuditLogs.Add(new AuditLog
        {
            Action = "PATIENT_CREATE",
            Entity = "Patient",
            Details = $"Bemor #{patient.Id} ro'yxatga olindi."
        });
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = patient.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (patient is null) return NotFound();

        var vm = new PatientDetailVm
        {
            Patient = patient,
            Encounters = await db.Encounters.Where(e => e.PatientId == id).OrderByDescending(e => e.OccurredAtUtc).ToListAsync(),
            Medications = await db.Medications.Where(m => m.PatientId == id).OrderByDescending(m => m.IsActive).ThenBy(m => m.Name).ToListAsync(),
            CouncilSessions = await db.AiCouncilSessions.Where(s => s.PatientId == id).OrderByDescending(s => s.CreatedAtUtc).ToListAsync()
        };
        return View(vm);
    }

    public async Task<IActionResult> Timeline(int id)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(x => x.Id == id);
        if (patient is null) return NotFound();

        var encounters = await db.Encounters.Where(x => x.PatientId == id).OrderBy(x => x.OccurredAtUtc).ToListAsync();
        var councils = await db.AiCouncilSessions.Where(x => x.PatientId == id).ToListAsync();

        var items = new List<PatientTimelineItemVm>();

        foreach (var e in encounters)
        {
            var vitals = new List<string>();
            var critical = new List<string>();

            void Add(string label, string? value, bool isCritical = false)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var chip = $"{label} {value}";
                vitals.Add(chip);
                if (isCritical) critical.Add(chip);
            }

            if (e.SystolicBp.HasValue || e.DiastolicBp.HasValue)
                Add("BP", $"{e.SystolicBp?.ToString() ?? "—"}/{e.DiastolicBp?.ToString() ?? "—"}",
                    e.SystolicBp is >= 180 or < 90);
            Add("YUT", e.HeartRate?.ToString(), e.HeartRate is > 130 or < 40);
            Add("SpO₂", e.SpO2?.ToString("0.#"), e.SpO2 is < 90);
            Add("Hb", e.Hemoglobin?.ToString("0.#"), e.Hemoglobin is < 90);
            Add("Glu", e.Glucose?.ToString("0.0"), e.Glucose is >= 11.1);

            items.Add(new PatientTimelineItemVm
            {
                DateUtc = e.OccurredAtUtc,
                Type = "Encounter",
                Title = "Klinik tashrif",
                Description = string.IsNullOrWhiteSpace(e.Symptoms) ? "Simptomlar ko'rsatilmagan" : e.Symptoms,
                Status = critical.Count > 0 ? "critical" : "normal",
                RelatedId = e.Id,
                Vitals = vitals,
                CriticalVitals = critical
            });
        }

        foreach (var c in councils)
        {
            items.Add(new PatientTimelineItemVm
            {
                DateUtc = c.EncounterDateUtc,
                Type = "AI Council",
                Title = $"AI konsilium · Risk {c.RiskScore}/100",
                Description = c.MainHypothesis,
                Status = c.RiskLevel == "Kritik" ? "critical" : "ai",
                RelatedId = c.Id,
                Vitals = new List<string>
                {
                    $"Daraja {c.RiskLevel}",
                    $"Konsensus {c.ConsensusScore}%",
                    $"O'tkazilgan {c.CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}"
                }
            });
        }

        return View(new PatientTimelineVm
        {
            Patient = patient,
            Items = items.OrderByDescending(x => x.DateUtc).ToList(),
            Trend = encounters.Select(e => new TrendPointVm
            {
                DateUtc = e.OccurredAtUtc,
                Hemoglobin = e.Hemoglobin,
                Glucose = e.Glucose,
                SystolicBp = e.SystolicBp,
                HeartRate = e.HeartRate
            }).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> AddEncounter(int id)
    {
        if (!await db.Patients.AnyAsync(p => p.Id == id)) return NotFound();
        return View(new NewEncounterVm { PatientId = id, OccurredAt = DateTime.Now });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEncounter(NewEncounterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var encounter = new Encounter
        {
            PatientId = vm.PatientId,
            Symptoms = vm.Symptoms,
            Anamnesis = vm.Anamnesis,
            Hemoglobin = vm.Hemoglobin,
            Glucose = vm.Glucose,
            SystolicBp = vm.SystolicBp,
            DiastolicBp = vm.DiastolicBp,
            HeartRate = vm.HeartRate,
            SpO2 = vm.SpO2,
            Temperature = vm.Temperature,
            RespiratoryRate = vm.RespiratoryRate,
            HeightCm = vm.HeightCm,
            WeightKg = vm.WeightKg,
            PainScore = vm.PainScore,
            Triage = string.IsNullOrWhiteSpace(vm.Triage) ? "Yashil" : vm.Triage,
            Icd10 = vm.Icd10,
            EcgSummary = vm.EcgSummary,
            ImagingSummary = vm.ImagingSummary,
            Notes = vm.Notes,
            OccurredAtUtc = DateTime.SpecifyKind(vm.OccurredAt, DateTimeKind.Local).ToUniversalTime()
        };

        db.Encounters.Add(encounter);
        await db.SaveChangesAsync();

        db.AuditLogs.Add(new AuditLog
        {
            Action = "ENCOUNTER_ADD",
            Entity = "Encounter",
            Details = $"Bemor #{vm.PatientId} uchun tashrif #{encounter.Id} ({encounter.OccurredAtUtc:yyyy-MM-dd}) qo'shildi."
        });
        await db.SaveChangesAsync();

        return RedirectToAction("Live", "Council", new { patientId = vm.PatientId, encounterId = encounter.Id });
    }
}
