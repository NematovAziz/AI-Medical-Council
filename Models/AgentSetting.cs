using System.ComponentModel.DataAnnotations;

namespace AI.MedicalCouncil.Models;

/// <summary>
/// Runtime configuration of one AI slot, editable from the settings screen.
/// Overrides appsettings.json / environment variables when present.
/// </summary>
public class AgentSetting
{
    public int Id { get; set; }

    /// <summary>Therapist | Lab | Cardiology | Radiology | Pharmacology | Critic | Safety | LabExtractor</summary>
    [Required, StringLength(40)] public string Key { get; set; } = string.Empty;

    /// <summary>Free-text vendor label shown in the UI: OpenAI, Gemini, DeepSeek, Grok, Claude, OpenRouter…</summary>
    [StringLength(40)] public string Provider { get; set; } = "OpenAI";

    public bool Enabled { get; set; }

    [StringLength(300)] public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    [StringLength(300)] public string ApiKey { get; set; } = string.Empty;
    [StringLength(120)] public string Model { get; set; } = string.Empty;

    public double Temperature { get; set; } = 0.1;
    public int TimeoutSeconds { get; set; } = 25;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
