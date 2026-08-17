using AI.MedicalCouncil.Data;
using AI.MedicalCouncil.Models;
using AI.MedicalCouncil.Options;
using Microsoft.EntityFrameworkCore;

namespace AI.MedicalCouncil.Services;

public interface IAgentConfigProvider
{
    /// <summary>Effective configuration for a slot: database override on top of appsettings.</summary>
    AiAgentOptions Get(string key);

    /// <summary>All slots the platform knows about, in display order.</summary>
    IReadOnlyList<string> Keys { get; }

    void Invalidate();
}

public class AgentConfigProvider(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<AgentConfigProvider> logger)
    : IAgentConfigProvider
{
    public static readonly string[] AllKeys =
    {
        "Therapist", "Lab", "Cardiology", "Radiology", "Pharmacology", "Critic", "Safety", "LabExtractor"
    };

    private readonly object _gate = new();
    private Dictionary<string, AgentSetting>? _cache;

    public IReadOnlyList<string> Keys => AllKeys;

    public void Invalidate()
    {
        lock (_gate) { _cache = null; }
    }

    public AiAgentOptions Get(string key)
    {
        var stored = Load().GetValueOrDefault(key);

        if (stored is not null)
        {
            return new AiAgentOptions
            {
                Provider = stored.Provider,
                Enabled = stored.Enabled,
                Endpoint = stored.Endpoint,
                ApiKey = stored.ApiKey,
                Model = stored.Model,
                Temperature = stored.Temperature,
                TimeoutSeconds = stored.TimeoutSeconds
            };
        }

        var section = configuration.GetSection($"AiAgents:{key}");
        var fallback = new AiAgentOptions();
        section.Bind(fallback);
        return fallback;
    }

    private Dictionary<string, AgentSetting> Load()
    {
        lock (_gate)
        {
            if (_cache is not null) return _cache;

            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                _cache = db.AgentSettings.AsNoTracking().ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Agent settings could not be read; falling back to configuration.");
                _cache = new Dictionary<string, AgentSetting>(StringComparer.OrdinalIgnoreCase);
            }

            return _cache;
        }
    }
}
