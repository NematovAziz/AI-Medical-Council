namespace AI.MedicalCouncil.ViewModels;

public class CouncilIndexVm
{
    public string? Query { get; set; }
    public string? Risk { get; set; }
    public List<CouncilIndexRowVm> Rows { get; set; } = new();
}

public class CouncilIndexRowVm
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime EncounterDateUtc { get; set; }
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string MainHypothesis { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ConsensusScore { get; set; }
    public int DurationMs { get; set; }
}
