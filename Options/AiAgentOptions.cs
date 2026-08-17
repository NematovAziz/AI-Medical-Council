namespace AI.MedicalCouncil.Options;

public class AiAgentOptions
{
    /// <summary>Vendor label shown in the UI: OpenAI, Gemini, DeepSeek, Grok, Claude, Mistral…</summary>
    public string Provider { get; set; } = "OpenAI";

    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.1;

    /// <summary>Hard timeout for a single agent call. A slow endpoint must never block the whole council.</summary>
    public int TimeoutSeconds { get; set; } = 25;
}
