using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AI.MedicalCouncil.Options;

namespace AI.MedicalCouncil.Services.AiAgents;

/// <summary>One text or image block of a user message.</summary>
public record ChatPart(string Type, string? Text = null, string? MediaType = null, string? Base64 = null)
{
    public static ChatPart FromText(string text) => new("text", text);
    public static ChatPart FromImage(string mediaType, string base64) => new("image", null, mediaType, base64);
}

/// <summary>
/// Speaks to any configured vendor. Two wire formats are supported:
/// the OpenAI chat-completions schema (OpenAI, Gemini, DeepSeek, Grok, Mistral, OpenRouter, Groq)
/// and Anthropic's Messages API, which uses a different header and body shape.
/// </summary>
public static class ChatClient
{
    public static bool IsAnthropicNative(AiAgentOptions cfg) =>
        cfg.Endpoint.Contains("api.anthropic.com", StringComparison.OrdinalIgnoreCase);

    public static async Task<string> SendAsync(
        HttpClient http,
        AiAgentOptions cfg,
        string system,
        IReadOnlyList<ChatPart> parts,
        int maxTokens,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, cfg.Endpoint);

        object payload;

        if (IsAnthropicNative(cfg))
        {
            req.Headers.Add("x-api-key", cfg.ApiKey.Trim());
            req.Headers.Add("anthropic-version", "2023-06-01");

            payload = new
            {
                model = cfg.Model,
                max_tokens = maxTokens,
                temperature = cfg.Temperature,
                system,
                messages = new object[] { new { role = "user", content = AnthropicContent(parts) } }
            };
        }
        else
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey.Trim());

            var body = new Dictionary<string, object>
            {
                ["model"] = cfg.Model,
                ["messages"] = new object[]
                {
                    new { role = "system", content = system },
                    new { role = "user", content = OpenAiContent(parts) }
                }
            };

            // Reasoning models on api.openai.com reject a custom temperature and reject max_tokens,
            // so those knobs are simply left out for them.
            if (!IsOpenAiReasoning(cfg)) body["temperature"] = cfg.Temperature;

            payload = body;
        }

        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await http.SendAsync(req, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            var host = Uri.TryCreate(cfg.Endpoint, UriKind.Absolute, out var uri) ? uri.Host : cfg.Endpoint;
            var scheme = IsAnthropicNative(cfg) ? "x-api-key" : "Bearer";
            throw new HttpRequestException($"{(int)res.StatusCode} · {host} · {scheme} · {Shorten(raw)}");
        }

        return ExtractText(raw, IsAnthropicNative(cfg));
    }

    private static bool IsOpenAiReasoning(AiAgentOptions cfg)
    {
        if (!cfg.Endpoint.Contains("api.openai.com", StringComparison.OrdinalIgnoreCase)) return false;
        var m = cfg.Model.ToLowerInvariant();
        return m.StartsWith("gpt-5") || m.StartsWith("o1") || m.StartsWith("o3") || m.StartsWith("o4");
    }

    private static object OpenAiContent(IReadOnlyList<ChatPart> parts)
    {
        if (parts.Count == 1 && parts[0].Type == "text")
            return parts[0].Text ?? string.Empty;

        return parts.Select(object (p) => p.Type == "image"
            ? new { type = "image_url", image_url = new { url = $"data:{p.MediaType};base64,{p.Base64}" } }
            : new { type = "text", text = p.Text ?? string.Empty }).ToArray();
    }

    private static object AnthropicContent(IReadOnlyList<ChatPart> parts) =>
        parts.Select(object (p) => p.Type == "image"
            ? new { type = "image", source = new { type = "base64", media_type = p.MediaType, data = p.Base64 } }
            : new { type = "text", text = p.Text ?? string.Empty }).ToArray();

    private static string ExtractText(string raw, bool anthropic)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        if (anthropic)
        {
            foreach (var block in root.GetProperty("content").EnumerateArray())
            {
                if (block.TryGetProperty("type", out var t) && t.GetString() == "text")
                    return block.GetProperty("text").GetString() ?? string.Empty;
            }
            return string.Empty;
        }

        return root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    public static string Shorten(string s) => s.Length <= 200 ? s : s[..200];
}
