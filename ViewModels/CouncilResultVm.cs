using AI.MedicalCouncil.Models;

namespace AI.MedicalCouncil.ViewModels;

public class CouncilResultVm
{
    public Patient Patient { get; set; } = null!;
    public Encounter Encounter { get; set; } = null!;
    public AiCouncilSession Session { get; set; } = null!;
}
