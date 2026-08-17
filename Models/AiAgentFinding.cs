namespace AI.MedicalCouncil.Models;

public class AiAgentFinding
{
    public int Id { get; set; }
    public int AiCouncilSessionId { get; set; }
    public AiCouncilSession Session { get; set; } = null!;

    public string AgentName { get; set; } = string.Empty;
    public string Finding { get; set; } = string.Empty;
    public int Confidence { get; set; }
    public string Severity { get; set; } = "Info";

    /// <summary>"Provider · model", or the reason the agent could not answer.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>False when the model was unreachable; such rows never influence the risk score.</summary>
    public bool Available { get; set; } = true;
    public int Round { get; set; } = 1;
    public int LatencyMs { get; set; }
}
