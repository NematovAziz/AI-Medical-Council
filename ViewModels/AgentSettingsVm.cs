namespace AI.MedicalCouncil.ViewModels;

public class AgentSettingsVm
{
    public List<AgentSettingVm> Rows { get; set; } = new();
    public (string Name, string Endpoint, string Model)[] Presets { get; set; } = Array.Empty<(string, string, string)>();
}

public class AgentSettingVm
{
    public string Key { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
    public string Provider { get; set; } = "OpenAI";
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.1;
    public int TimeoutSeconds { get; set; } = 25;

    public bool HasApiKey { get; set; }
    public string KeyHint { get; set; } = string.Empty;
}
