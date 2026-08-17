using AI.MedicalCouncil.Models;

namespace AI.MedicalCouncil.ViewModels;

public class PatientTimelineVm
{
    public Patient Patient { get; set; } = null!;
    public List<PatientTimelineItemVm> Items { get; set; } = new();

    /// <summary>Chronological vitals used by the client-side trend chart.</summary>
    public List<TrendPointVm> Trend { get; set; } = new();
}

public class PatientTimelineItemVm
{
    public DateTime DateUtc { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "normal";
    public int? RelatedId { get; set; }

    /// <summary>Short vitals chips rendered under the timeline entry.</summary>
    public List<string> Vitals { get; set; } = new();
    public List<string> CriticalVitals { get; set; } = new();
}

public class TrendPointVm
{
    public DateTime DateUtc { get; set; }
    public double? Hemoglobin { get; set; }
    public double? Glucose { get; set; }
    public int? SystolicBp { get; set; }
    public int? HeartRate { get; set; }
}
