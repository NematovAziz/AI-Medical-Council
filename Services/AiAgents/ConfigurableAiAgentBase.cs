using System.Diagnostics;
using System.Text.Json;
using AI.MedicalCouncil.Services;

namespace AI.MedicalCouncil.Services.AiAgents;

/// <summary>
/// Base class for every council member. Each agent reasons through its own vendor and model —
/// there is no rule-based substitute. When a model cannot be reached the agent reports that
/// openly instead of inventing an opinion, and its voice is excluded from the risk score.
/// </summary>
public abstract class ConfigurableAiAgentBase<TAgent>(HttpClient http, IAgentConfigProvider config) : IMedicalAiAgent
{
    public abstract string AgentName { get; }
    public abstract string Specialty { get; }
    protected abstract string OptionName { get; }
    protected abstract string SystemPrompt { get; }

    protected virtual int Round => 1;

    public async Task<AgentOutput> AnalyzeAsync(AgentInput input, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var cfg = config.Get(OptionName);

        if (!cfg.Enabled)
            return Unavailable(sw, "Agent o'chirilgan");
        if (string.IsNullOrWhiteSpace(cfg.ApiKey))
            return Unavailable(sw, "API kalit kiritilmagan");
        if (string.IsNullOrWhiteSpace(cfg.Model))
            return Unavailable(sw, "Model kiritilmagan");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, cfg.TimeoutSeconds)));

        try
        {
            var content = await ChatClient.SendAsync(
                http, cfg, BuildSystemPrompt(),
                new[] { ChatPart.FromText(BuildClinicalContext(input)) },
                maxTokens: 700,
                timeout.Token);

            content = content.Trim().Trim('`').Trim();
            if (content.StartsWith("json", StringComparison.OrdinalIgnoreCase)) content = content[4..].Trim();

            var open = content.IndexOf('{');
            var close = content.LastIndexOf('}');
            if (open >= 0 && close > open) content = content[open..(close + 1)];

            using var parsed = JsonDocument.Parse(content);
            var root = parsed.RootElement;

            var assessment = Text(root, "assessment");
            var diagnosis = Text(root, "diagnosis");
            var reasoning = Text(root, "reasoning");

            var finding = string.Join(" ", new[] { diagnosis, assessment, reasoning }.Where(x => x.Length > 0));
            if (finding.Length == 0) finding = Text(root, "finding");
            if (finding.Length == 0) finding = content;

            return new AgentOutput(
                AgentName,
                finding,
                root.TryGetProperty("confidence", out var c) && c.TryGetInt32(out var ci) ? Math.Clamp(ci, 0, 100) : 70,
                root.TryGetProperty("severity", out var s) ? NormalizeSeverity(s.GetString()) : "Info",
                $"{cfg.Provider} · {cfg.Model}",
                Round,
                (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            var reason = ex is OperationCanceledException
                ? $"Timeout {cfg.TimeoutSeconds}s"
                : ChatClient.Shorten(ex.Message);

            return Unavailable(sw, $"{cfg.Provider}: {reason}");
        }
    }

    private AgentOutput Unavailable(Stopwatch sw, string reason) => new(
        AgentName,
        $"Xulosa berilmadi — {reason}.",
        0,
        "Info",
        reason,
        Round,
        (int)sw.ElapsedMilliseconds,
        Available: false);

    private string BuildSystemPrompt() =>
        SystemPrompt + " " +
        "You are one specialist in a multi-agent clinical council supporting a physician. " +
        "Form your OWN independent assessment from the data given — do not defer, do not repeat generic advice, " +
        "and do not say the data is insufficient unless it truly is. " +
        "Name the most probable condition in your specialty and the reasoning behind it. " +
        "Answer in Uzbek (latin script). Return compact JSON only, with fields: " +
        "diagnosis (string, the most probable condition in your field, max 120 chars), " +
        "reasoning (string, which findings led you there, max 200 chars), " +
        "confidence (integer 0-100), severity (Info|Warning|Critical). " +
        "The physician confirms every conclusion, so never present it as final and never prescribe treatment.";

    private static string Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()?.Trim() ?? string.Empty
            : string.Empty;

    private static string NormalizeSeverity(string? s) => s?.ToLowerInvariant() switch
    {
        "critical" => "Critical",
        "warning" => "Warning",
        _ => "Info"
    };

    protected static string BuildClinicalContext(AgentInput i)
    {
        var meds = i.Medications.Count == 0
            ? "yo'q"
            : string.Join(", ", i.Medications.Select(m => $"{m.Name} {m.Dose}"));

        var history = string.Join(" | ", i.History.TakeLast(8).Select(h =>
            $"{h.OccurredAtUtc:yyyy-MM-dd}: Hb={h.Hemoglobin}; Glu={h.Glucose}; BP={h.SystolicBp}/{h.DiastolicBp}; HR={h.HeartRate}; SpO2={h.SpO2}; simptom={h.Symptoms}"));

        var labs = i.LabResults is { Count: > 0 }
            ? "\n\nLaboratoriya paneli (fayldan ajratilgan):\n" +
              string.Join("\n", i.LabResults.Select(r =>
                  $"- {r.Analyte}: {r.Value} {r.Unit} (referens {r.RefLow?.ToString() ?? "?"}-{r.RefHigh?.ToString() ?? "?"}, bayroq {r.Flag})"))
            : "";

        var peers = i.PeerFindings is { Count: > 0 }
            ? "\n\nROUND 1 — boshqa agentlar xulosalari (ularni tanqidiy baholang):\n" +
              string.Join("\n", i.PeerFindings.Where(p => p.Available).Select(p =>
                  $"- {p.AgentName} [{p.Severity} / {p.Confidence}%]: {p.Finding}"))
            : "";

        return $"Bemor: {i.Patient.FullName}; tug'ilgan={i.Patient.BirthDate:yyyy-MM-dd}; jins={i.Patient.Sex}; " +
               $"allergiya={i.Patient.Allergies}; surunkali={i.Patient.ChronicConditions}. " +
               $"Joriy tashrif: simptomlar={i.Encounter.Symptoms}; anamnez={i.Encounter.Anamnesis}; " +
               $"Hb={i.Encounter.Hemoglobin}; glyukoza={i.Encounter.Glucose}; " +
               $"BP={i.Encounter.SystolicBp}/{i.Encounter.DiastolicBp}; HR={i.Encounter.HeartRate}; SpO2={i.Encounter.SpO2}; " +
               $"harorat={i.Encounter.Temperature}; nafas={i.Encounter.RespiratoryRate}; " +
               $"EKG={i.Encounter.EcgSummary}; tasvir={i.Encounter.ImagingSummary}; dorilar={meds}. " +
               $"Oldingi tarix: {history}{labs}{peers}";
    }
}
