using System.Text.Json;
using System.Text.Json.Serialization;
using AI.MedicalCouncil.Data;
using AI.MedicalCouncil.Services;
using AI.MedicalCouncil.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.MedicalCouncil.Controllers;

public class CouncilController(AppDbContext db, IAiCouncilService council, ILogger<CouncilController> logger) : Controller
{
    private static readonly JsonSerializerOptions SseJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<IActionResult> Index(string? q, string? risk)
    {
        var query = db.AiCouncilSessions.Include(x => x.Patient).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => EF.Functions.ILike(x.Patient.FullName, $"%{q}%")
                                  || EF.Functions.ILike(x.MainHypothesis, $"%{q}%"));

        if (!string.IsNullOrWhiteSpace(risk))
            query = query.Where(x => x.RiskLevel == risk);

        var vm = new CouncilIndexVm
        {
            Query = q,
            Risk = risk,
            Rows = await query.OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new CouncilIndexRowVm
                {
                    Id = x.Id,
                    PatientId = x.PatientId,
                    PatientName = x.Patient.FullName,
                    CreatedAtUtc = x.CreatedAtUtc,
                    EncounterDateUtc = x.EncounterDateUtc,
                    RiskScore = x.RiskScore,
                    RiskLevel = x.RiskLevel,
                    MainHypothesis = x.MainHypothesis,
                    Status = x.Status,
                    ConsensusScore = x.ConsensusScore,
                    DurationMs = x.DurationMs
                })
                .ToListAsync()
        };
        return View(vm);
    }

    /// <summary>Live console: renders the arena, then the browser opens the SSE stream.</summary>
    public async Task<IActionResult> Live(int patientId, int encounterId)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
        var encounter = await db.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId && e.PatientId == patientId);
        if (patient is null || encounter is null) return NotFound();

        return View(new CouncilLiveVm
        {
            Patient = patient,
            Encounter = encounter,
            Roster = council.Roster
        });
    }

    /// <summary>Server-sent events: every agent is pushed to the UI the moment it finishes.</summary>
    [HttpGet]
    public async Task Stream(int patientId, int encounterId, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        async Task Send(string eventName, object payload)
        {
            var data = JsonSerializer.Serialize(payload, SseJson);
            await Response.WriteAsync($"event: {eventName}\ndata: {data}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        try
        {
            var session = await council.RunAsync(
                patientId,
                encounterId,
                progress => Send(progress.Event, progress.Payload),
                ct);

            await Send("done", new
            {
                sessionId = session.Id,
                riskScore = session.RiskScore,
                riskLevel = session.RiskLevel,
                consensus = session.ConsensusScore,
                durationMs = session.DurationMs,
                resultUrl = Url.Action(nameof(Result), "Council", new { id = session.Id })
            });
        }
        catch (OperationCanceledException)
        {
            // client navigated away — nothing to report
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Council stream failed for patient {PatientId}", patientId);
            await Send("failed", new { message = "Konsilium yakunlanmadi. Qayta urinib ko'ring." });
        }
    }

    /// <summary>Non-streaming fallback, kept for direct links and for clients without EventSource.</summary>
    public async Task<IActionResult> Run(int patientId, int encounterId, CancellationToken ct)
    {
        var session = await council.RunAsync(patientId, encounterId, ct);
        return RedirectToAction(nameof(Result), new { id = session.Id });
    }

    public async Task<IActionResult> Result(int id)
    {
        var vm = await BuildResultAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    public async Task<IActionResult> Print(int id)
    {
        var vm = await BuildResultAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    private async Task<CouncilResultVm?> BuildResultAsync(int id)
    {
        var session = await db.AiCouncilSessions.Include(s => s.Findings).FirstOrDefaultAsync(s => s.Id == id);
        if (session is null) return null;

        var patient = await db.Patients.FirstAsync(p => p.Id == session.PatientId);
        var encounter = await db.Encounters.FirstAsync(e => e.Id == session.EncounterId);
        return new CouncilResultVm { Patient = patient, Encounter = encounter, Session = session };
    }
}
