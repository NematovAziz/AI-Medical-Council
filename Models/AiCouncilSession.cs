namespace AI.MedicalCouncil.Models;

public class AiCouncilSession
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public int EncounterId { get; set; }
    public Encounter Encounter { get; set; } = null!;

    /// <summary>When the council was executed.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The clinical date of the analysed encounter. The timeline is ordered by this, not by
    /// CreatedAtUtc, so back-entered lab results land at the point in history where they belong.
    /// </summary>
    public DateTime EncounterDateUtc { get; set; } = DateTime.UtcNow;

    public string MainHypothesis { get; set; } = string.Empty;
    public string AlternativeHypotheses { get; set; } = string.Empty;
    public string RecommendedTests { get; set; } = string.Empty;
    public string RedFlags { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;

    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = "Past";

    /// <summary>How tightly the council agreed, 0-100.</summary>
    public int ConsensusScore { get; set; } = 100;
    public int DurationMs { get; set; }
    public string EngineVersion { get; set; } = "risk-engine/4.0";

    public string Status { get; set; } = "AI qoralama — shifokor ko'rib chiqishi shart";

    public List<AiAgentFinding> Findings { get; set; } = new();
}
