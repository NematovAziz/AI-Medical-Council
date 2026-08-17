using AI.MedicalCouncil.Models;

namespace AI.MedicalCouncil.ViewModels;

public class LabsIndexVm
{
    public Patient Patient { get; set; } = null!;
    public List<LabDocument> Documents { get; set; } = new();
    public List<LabResult> RecentResults { get; set; } = new();
}

public class LabDocumentVm
{
    public Patient Patient { get; set; } = null!;
    public LabDocument Document { get; set; } = null!;
    public List<LabResult> Results { get; set; } = new();
}
