using AI.MedicalCouncil.Data;
using AI.MedicalCouncil.Models;
using AI.MedicalCouncil.Services;
using AI.MedicalCouncil.Services.AiAgents;
using AI.MedicalCouncil.Services.Localization;
using AI.MedicalCouncil.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.MedicalCouncil.Controllers;

public class SettingsController(
    AppDbContext db,
    IAgentConfigProvider config,
    IHttpClientFactory httpFactory,
    ILogger<SettingsController> logger) : Controller
{
    private static readonly Dictionary<string, string> Display = new()
    {
        ["Therapist"] = "AI Terapevt",
        ["Lab"] = "AI Laborant",
        ["Cardiology"] = "AI Kardiolog",
        ["Radiology"] = "AI Radiolog",
        ["Pharmacology"] = "AI Farmakolog",
        ["Critic"] = "AI Kritik",
        ["Safety"] = "Safety Agent",
        ["LabExtractor"] = "AI Lab Extractor"
    };

    /// <summary>Ready-made endpoints so a key can be pasted without looking anything up.</summary>
    public static readonly (string Name, string Endpoint, string Model)[] Presets =
    {
        ("OpenAI",     "https://api.openai.com/v1/chat/completions",                          "gpt-5.6-terra"),
        ("Gemini",     "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", "gemini-3.6-flash"),
        ("DeepSeek",   "https://api.deepseek.com/v1/chat/completions",                        "deepseek-v4-flash"),
        ("Grok (xAI)", "https://api.x.ai/v1/chat/completions",                                "grok-4.6"),
        ("Claude",     "https://api.anthropic.com/v1/messages",                               "claude-sonnet-5"),
        ("OpenRouter", "https://openrouter.ai/api/v1/chat/completions",                       "openai/gpt-5.6-terra"),
        ("Groq",       "https://api.groq.com/openai/v1/chat/completions",                     "llama-3.3-70b-versatile"),
        ("Mistral",    "https://api.mistral.ai/v1/chat/completions",                          "mistral-large-latest")
    };

    /// <summary>
    /// Switches the UI language and stores it in the custom AMC language cookie.
    /// Supported values: uz-Latn, uz-Cyrl, ru.
    /// </summary>
    [HttpGet]
    public IActionResult Language(string? lang, string? returnUrl)
    {
        var selected = lang switch
        {
            Localizer.Cyrillic => Localizer.Cyrillic,
            Localizer.Russian => Localizer.Russian,
            _ => Localizer.Latin
        };

        Response.Cookies.Append(
            Localizer.CookieName,
            selected,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> AiAgents()
    {
        var stored = await db.AgentSettings.AsNoTracking().ToDictionaryAsync(x => x.Key, StringComparer.OrdinalIgnoreCase);

        var rows = AgentConfigProvider.AllKeys.Select(key =>
        {
            var effective = config.Get(key);
            stored.TryGetValue(key, out var row);

            return new AgentSettingVm
            {
                Key = key,
                Display = Display.GetValueOrDefault(key, key),
                Provider = row?.Provider ?? effective.Provider,
                Enabled = effective.Enabled,
                Endpoint = effective.Endpoint,
                Model = effective.Model,
                Temperature = effective.Temperature,
                TimeoutSeconds = effective.TimeoutSeconds,
                HasApiKey = !string.IsNullOrWhiteSpace(effective.ApiKey),
                KeyHint = Mask(effective.ApiKey)
            };
        }).ToList();

        return View(new AgentSettingsVm { Rows = rows, Presets = Presets });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AiAgents(List<AgentSettingVm> rows)
    {
        foreach (var input in rows ?? new List<AgentSettingVm>())
        {
            if (!AgentConfigProvider.AllKeys.Contains(input.Key)) continue;

            var row = await db.AgentSettings.FirstOrDefaultAsync(x => x.Key == input.Key);
            if (row is null)
            {
                row = new AgentSetting { Key = input.Key };
                db.AgentSettings.Add(row);
            }

            row.Provider = string.IsNullOrWhiteSpace(input.Provider) ? "OpenAI" : input.Provider.Trim();
            row.Enabled = input.Enabled;
            row.Endpoint = (input.Endpoint ?? string.Empty).Trim();
            row.Model = (input.Model ?? string.Empty).Trim();
            row.Temperature = Math.Clamp(input.Temperature, 0, 2);
            row.TimeoutSeconds = Math.Clamp(input.TimeoutSeconds, 5, 180);
            row.UpdatedAtUtc = DateTime.UtcNow;

            // An empty key field means "keep the stored key".
            if (!string.IsNullOrWhiteSpace(input.ApiKey))
                row.ApiKey = input.ApiKey.Trim();
        }

        await db.SaveChangesAsync();
        config.Invalidate();

        db.AuditLogs.Add(new AuditLog
        {
            Action = "AGENT_CONFIG",
            Entity = "AgentSetting",
            Details = "AI agent sozlamalari yangilandi."
        });
        await db.SaveChangesAsync();

        TempData["saved"] = true;
        return RedirectToAction(nameof(AiAgents));
    }

    /// <summary>Fires one tiny request so the physician can see the key works before a real council runs.</summary>
    [HttpPost]
    public async Task<IActionResult> Test(string key, CancellationToken ct)
    {
        if (!AgentConfigProvider.AllKeys.Contains(key))
            return Json(new { ok = false, message = "Noma'lum agent." });

        var cfg = config.Get(key);
        if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.ApiKey) || string.IsNullOrWhiteSpace(cfg.Model))
            return Json(new { ok = false, message = "Agent yoqilmagan yoki kalit/model kiritilmagan." });

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));

            var http = httpFactory.CreateClient();
            var reply = await ChatClient.SendAsync(
                http, cfg,
                "Reply with the single word OK.",
                new[] { ChatPart.FromText("ping") },
                maxTokens: 16,
                timeout.Token);

            var shown = reply.Trim();
            if (shown.Length > 40) shown = shown[..40];

            return Json(new { ok = true, message = $"{cfg.Provider} · {cfg.Model} · {shown}" });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Agent test failed for {Key}", key);
            var message = ex is OperationCanceledException ? "Timeout" : ChatClient.Shorten(ex.Message);
            return Json(new { ok = false, message });
        }
    }

    private static string Mask(string? key) =>
        string.IsNullOrWhiteSpace(key) ? "" :
        key.Length <= 8 ? "••••" : $"{key[..4]}••••{key[^4..]}";

    private static string Trim(string s) => s.Length <= 160 ? s : s[..160];
}
