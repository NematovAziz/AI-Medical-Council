using System.Diagnostics;
using AI.MedicalCouncil.Data;
using AI.MedicalCouncil.Models;
using AI.MedicalCouncil.Services.AiAgents;
using Microsoft.EntityFrameworkCore;

namespace AI.MedicalCouncil.Services;

public record CouncilProgress(string Event, object Payload);

public interface IAiCouncilService
{
    Task<AiCouncilSession> RunAsync(int patientId, int encounterId, CancellationToken ct = default);

    /// <summary>Runs the council and reports every agent the moment it finishes, for live streaming.</summary>
    Task<AiCouncilSession> RunAsync(
        int patientId,
        int encounterId,
        Func<CouncilProgress, Task>? onProgress,
        CancellationToken ct = default);

    IReadOnlyList<AgentRoster> Roster { get; }
}

public record AgentRoster(string AgentName, string Specialty, int Round);

public class AiCouncilService(
    AppDbContext db,
    IEnumerable<IMedicalAiAgent> agents,
    IRiskEngine riskEngine,
    ILogger<AiCouncilService> logger) : IAiCouncilService
{
    private IReadOnlyList<IMedicalAiAgent> Round1 =>
        agents.Where(a => a is not ICriticAgent && a is not ISafetyAgent).ToList();

    private IReadOnlyList<IMedicalAiAgent> Round2 =>
        agents.Where(a => a is ICriticAgent or ISafetyAgent).ToList();

    public IReadOnlyList<AgentRoster> Roster =>
        Round1.Select(a => new AgentRoster(a.AgentName, a.Specialty, 1))
              .Concat(Round2.Select(a => new AgentRoster(a.AgentName, a.Specialty, 2)))
              .ToList();

    public Task<AiCouncilSession> RunAsync(int patientId, int encounterId, CancellationToken ct = default)
        => RunAsync(patientId, encounterId, null, ct);

    public async Task<AiCouncilSession> RunAsync(
        int patientId,
        int encounterId,
        Func<CouncilProgress, Task>? onProgress,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var patient = await db.Patients.FirstAsync(p => p.Id == patientId, ct);
        var encounter = await db.Encounters.FirstAsync(e => e.Id == encounterId && e.PatientId == patientId, ct);
        var history = await db.Encounters.Where(e => e.PatientId == patientId).OrderBy(e => e.OccurredAtUtc).ToListAsync(ct);
        var meds = await db.Medications.Where(m => m.PatientId == patientId && m.IsActive).ToListAsync(ct);

        // Lab rows collected on the same day as this encounter, so the council reasons over the panel too.
        var day = encounter.OccurredAtUtc.Date;
        var labs = await db.LabResults
            .Where(r => r.PatientId == patientId && r.ObservedAtUtc >= day && r.ObservedAtUtc < day.AddDays(1))
            .OrderBy(r => r.Analyte)
            .ToListAsync(ct);

        var results = new List<AgentOutput>();

        // ---------- round 1: independent specialists ----------
        await Notify(onProgress, "phase", new { round = 1, label = "1-RAUND · MUSTAQIL TAHLIL" });

        var input = new AgentInput(patient, encounter, history, meds, LabResults: labs);
        await RunRoundAsync(Round1, input, results, onProgress, ct);

        // ---------- round 2: review of round 1 ----------
        await Notify(onProgress, "phase", new { round = 2, label = "2-RAUND · O'ZARO NAZORAT" });

        var reviewInput = input with { PeerFindings = results.ToList() };
        await RunRoundAsync(Round2, reviewInput, results, onProgress, ct);

        // ---------- scoring ----------
        var assessment = riskEngine.Evaluate(patient, encounter, results);
        sw.Stop();

        var session = new AiCouncilSession
        {
            PatientId = patientId,
            EncounterId = encounterId,
            EncounterDateUtc = encounter.OccurredAtUtc,
            MainHypothesis = assessment.MainHypothesis,
            AlternativeHypotheses = assessment.AlternativeHypotheses,
            RecommendedTests = assessment.RecommendedTests,
            RedFlags = assessment.RedFlags,
            Summary = assessment.Summary,
            RiskScore = assessment.Score,
            RiskLevel = assessment.Level,
            ConsensusScore = assessment.ConsensusScore,
            DurationMs = (int)sw.ElapsedMilliseconds,
            EngineVersion = riskEngine.Version,
            Findings = results.Select(o => new AiAgentFinding
            {
                AgentName = o.AgentName,
                Finding = o.Finding,
                Confidence = o.Confidence,
                Severity = o.Severity,
                Source = o.Source,
                Round = o.Round,
                LatencyMs = o.LatencyMs,
                Available = o.Available
            }).ToList()
        };

        db.AiCouncilSessions.Add(session);
        db.AuditLogs.Add(new AuditLog
        {
            Action = "COUNCIL_RUN",
            Entity = "AiCouncilSession",
            Details = $"Patient #{patientId}, encounter #{encounterId}, risk {assessment.Score} ({assessment.Level}), {results.Count} agents, {sw.ElapsedMilliseconds} ms"
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Council session {Id} finished in {Ms} ms with risk {Score}", session.Id, sw.ElapsedMilliseconds, session.RiskScore);
        return session;
    }

    private static async Task RunRoundAsync(
        IReadOnlyList<IMedicalAiAgent> roundAgents,
        AgentInput input,
        List<AgentOutput> results,
        Func<CouncilProgress, Task>? onProgress,
        CancellationToken ct)
    {
        var pending = roundAgents.Select(a => a.AnalyzeAsync(input, ct)).ToList();

        while (pending.Count > 0)
        {
            var finished = await Task.WhenAny(pending);
            pending.Remove(finished);

            AgentOutput output;
            try
            {
                output = await finished;
            }
            catch (Exception)
            {
                continue;
            }

            results.Add(output);
            await Notify(onProgress, "agent", new
            {
                agent = output.AgentName,
                available = output.Available,
                finding = output.Finding,
                confidence = output.Confidence,
                severity = output.Severity,
                source = output.Source,
                round = output.Round,
                latencyMs = output.LatencyMs
            });
        }
    }

    private static Task Notify(Func<CouncilProgress, Task>? onProgress, string name, object payload)
        => onProgress is null ? Task.CompletedTask : onProgress(new CouncilProgress(name, payload));
}
