using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AI.MedicalCouncil.Models;
using AI.MedicalCouncil.Options;
using AI.MedicalCouncil.Services;
using AI.MedicalCouncil.Services.AiAgents;

namespace AI.MedicalCouncil.Services.Labs;

public record LabAnalysis(
    List<LabResult> Results,
    string Summary,
    string Source,
    string RawText,
    DateTime? CollectedAtUtc,
    int DurationMs);

public interface ILabDocumentAnalyzer
{
    Task<LabAnalysis> AnalyzeAsync(byte[] bytes, string fileName, string contentType, Patient patient, CancellationToken ct = default);
}

/// <summary>
/// Reads an uploaded lab report and returns structured analyte rows.
/// Strategy: extract text, ask the configured extraction model for JSON, and fall back to a
/// deterministic parser when no API is available or the response cannot be used.
/// </summary>
public class LabDocumentAnalyzer(HttpClient http, IAgentConfigProvider config, ILogger<LabDocumentAnalyzer> logger)
    : ILabDocumentAnalyzer
{
    private const string OptionName = "LabExtractor";

    public async Task<LabAnalysis> AnalyzeAsync(byte[] bytes, string fileName, string contentType, Patient patient, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var extracted = LabTextExtractor.Extract(bytes, fileName, contentType);
        var cfg = config.Get(OptionName);
        var apiReady = cfg.Enabled && !string.IsNullOrWhiteSpace(cfg.ApiKey) && !string.IsNullOrWhiteSpace(cfg.Model);

        if (apiReady)
        {
            try
            {
                var parsed = await CallExtractionModelAsync(cfg, extracted, bytes, contentType, patient, ct);
                if (parsed.Count > 0)
                {
                    sw.Stop();
                    return Finish(parsed, extracted.Text, $"{cfg.Provider} · {cfg.Model}", extracted, sw.ElapsedMilliseconds);
                }
                logger.LogInformation("Extraction model returned no analytes for {File}; using local parser.", fileName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lab extraction API failed for {File}; using local parser.", fileName);
            }
        }

        var local = ParseLocally(extracted.Text);
        sw.Stop();

        var source = apiReady ? "Lokal parser · API javob bermadi" : "Lokal parser";
        if (extracted.IsImage && local.Count == 0)
            source = "Matn ajratilmadi — vision modelini yoqing";

        return Finish(local, extracted.Text, source, extracted, sw.ElapsedMilliseconds);
    }

    // ---------- API path ----------

    private async Task<List<LabResult>> CallExtractionModelAsync(
        AiAgentOptions cfg, ExtractedText extracted, byte[] bytes, string contentType, Patient patient, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(20, cfg.TimeoutSeconds)));

        const string system =
            "You extract laboratory analytes from a clinical report. " +
            "Return ONLY a JSON object: {\"collectedAt\":\"YYYY-MM-DD or null\",\"results\":[" +
            "{\"analyte\":\"name as printed\",\"value\":number,\"unit\":\"string\"," +
            "\"refLow\":number|null,\"refHigh\":number|null}]}. " +
            "Use a dot as the decimal separator. Never invent values that are not in the document. " +
            "If nothing can be read, return an empty results array.";

        var parts = extracted.IsImage
            ? new[]
            {
                ChatPart.FromText($"Patient: {patient.FullName}, sex {patient.Sex}. Extract every analyte from this lab report image."),
                ChatPart.FromImage(contentType, Convert.ToBase64String(bytes))
            }
            : new[]
            {
                ChatPart.FromText($"Patient: {patient.FullName}, sex {patient.Sex}.\n\nLab report text:\n{Truncate(extracted.Text, 12000)}")
            };

        var content = await ChatClient.SendAsync(http, cfg, system, parts, maxTokens: 4000, timeout.Token);

        content = content.Trim().Trim('`').Trim();
        if (content.StartsWith("json", StringComparison.OrdinalIgnoreCase)) content = content[4..].Trim();

        var open = content.IndexOf('{');
        var close = content.LastIndexOf('}');
        if (open >= 0 && close > open) content = content[open..(close + 1)];

        using var parsed = JsonDocument.Parse(content);
        var results = new List<LabResult>();

        if (!parsed.RootElement.TryGetProperty("results", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return results;

        foreach (var item in arr.EnumerateArray())
        {
            var name = item.TryGetProperty("analyte", out var n) ? n.GetString() : null;
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!item.TryGetProperty("value", out var v) || !v.TryGetDouble(out var value)) continue;

            var unit = item.TryGetProperty("unit", out var u) ? u.GetString() ?? "" : "";
            double? refLow = item.TryGetProperty("refLow", out var rl) && rl.TryGetDouble(out var rlv) ? rlv : null;
            double? refHigh = item.TryGetProperty("refHigh", out var rh) && rh.TryGetDouble(out var rhv) ? rhv : null;

            results.Add(Build(name!, value, unit, refLow, refHigh));
        }

        return results;
    }

    // ---------- local parser ----------

    // The name may contain digits (HbA1c, Troponin I, CA 125), so it is matched lazily and then
    // validated against the analyte dictionary — a false capture simply finds no match and is dropped.
    private static readonly Regex LineValue = new(
        @"^[\s\-*•]*(?<name>[^|;:\t]*?[A-Za-zА-Яа-яЎўҚқҒғҲҳ][^|;:\t]*?)[\s:|;\t]+(?<value>-?\d+(?:[.,]\d+)?)(?![\d.,])\s*(?<unit>[^\s\d,;|]{0,14})?(?<rest>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RefRange = new(
        @"(?<low>\d+(?:[.,]\d+)?)\s*[-–—]\s*(?<high>\d+(?:[.,]\d+)?)",
        RegexOptions.Compiled);

    public static List<LabResult> ParseLocally(string text)
    {
        var results = new List<LabResult>();
        if (string.IsNullOrWhiteSpace(text)) return results;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Replace('\t', ' ').Trim();
            if (line.Length < 4) continue;

            var m = LineValue.Match(line);
            if (!m.Success) continue;

            var name = m.Groups["name"].Value.Trim(' ', '-', '·', '.', '|');
            if (name.Count(char.IsLetter) < 3) continue;

            var def = ReferenceRanges.Match(name);
            if (def is null) continue;
            if (!seen.Add(def.Canonical)) continue;

            if (!double.TryParse(m.Groups["value"].Value.Replace(',', '.'),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) continue;

            var unit = m.Groups["unit"].Value.Trim();
            if (string.IsNullOrWhiteSpace(unit) || unit.Length < 2) unit = def.Unit;

            double? low = null, high = null;
            var rr = RefRange.Match(m.Groups["rest"].Value);
            if (rr.Success
                && double.TryParse(rr.Groups["low"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var l)
                && double.TryParse(rr.Groups["high"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var h))
            {
                low = l; high = h;
            }

            results.Add(Build(name, value, unit, low, high));
        }

        return results;
    }

    // ---------- shared ----------

    private static LabResult Build(string rawName, double value, string unit, double? refLow, double? refHigh)
    {
        var def = ReferenceRanges.Match(rawName);
        return new LabResult
        {
            Analyte = def?.Canonical ?? rawName.Trim(),
            Code = def?.Code,
            Value = Math.Round(value, 3),
            Unit = string.IsNullOrWhiteSpace(unit) ? def?.Unit ?? "" : unit,
            RefLow = refLow ?? def?.RefLow,
            RefHigh = refHigh ?? def?.RefHigh,
            Flag = ReferenceRanges.Flag(def, value, refLow, refHigh)
        };
    }

    private static LabAnalysis Finish(List<LabResult> results, string rawText, string source, ExtractedText extracted, long ms)
    {
        var abnormal = results.Count(r => r.Flag != "N");
        var critical = results.Count(r => r.Flag == "C");

        var summary = results.Count == 0
            ? $"Ko'rsatkich ajratilmadi ({extracted.Method})."
            : critical > 0
                ? $"{results.Count} ta ko'rsatkich ajratildi, {abnormal} tasi me'yordan chetda, {critical} tasi kritik darajada."
                : abnormal > 0
                    ? $"{results.Count} ta ko'rsatkich ajratildi, {abnormal} tasi me'yordan chetda."
                    : $"{results.Count} ta ko'rsatkich ajratildi, barchasi referens oralig'ida.";

        return new LabAnalysis(results, summary, source, Truncate(rawText, 20000), TryFindDate(rawText), (int)ms);
    }

    private static readonly Regex DatePattern = new(
        @"(?<d>\d{1,2})[./-](?<m>\d{1,2})[./-](?<y>\d{4})|(?<y2>\d{4})-(?<m2>\d{1,2})-(?<d2>\d{1,2})",
        RegexOptions.Compiled);

    private static DateTime? TryFindDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var m = DatePattern.Match(text);
        if (!m.Success) return null;

        try
        {
            var (y, mo, d) = m.Groups["y"].Success
                ? (int.Parse(m.Groups["y"].Value), int.Parse(m.Groups["m"].Value), int.Parse(m.Groups["d"].Value))
                : (int.Parse(m.Groups["y2"].Value), int.Parse(m.Groups["m2"].Value), int.Parse(m.Groups["d2"].Value));

            if (y < 1900 || y > DateTime.UtcNow.Year + 1 || mo is < 1 or > 12 || d is < 1 or > 31) return null;
            return new DateTime(y, mo, d, 0, 0, 0, DateTimeKind.Utc);
        }
        catch { return null; }
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? string.Empty : s.Length <= max ? s : s[..max];
}
