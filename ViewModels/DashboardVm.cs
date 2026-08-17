namespace AI.MedicalCouncil.ViewModels;

public class DashboardVm
{
    public int TotalPatients { get; set; }
    public int TotalEncounters { get; set; }
    public int TotalCouncilSessions { get; set; }
    public int CriticalCount { get; set; }
    public int ActiveMedications { get; set; }
    public int AgentsOnline { get; set; }
    public int AgentsTotal { get; set; }
    public double AverageRisk { get; set; }
    public List<DashboardPatientVm> RecentPatients { get; set; } = new();
    public List<DashboardCouncilVm> RecentCouncils { get; set; } = new();
    public List<AuditRowVm> RecentAudit { get; set; } = new();
}

public class DashboardPatientVm
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class DashboardCouncilVm
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string MainHypothesis { get; set; } = string.Empty;
}

public class AuditRowVm
{
    public DateTime CreatedAtUtc { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
