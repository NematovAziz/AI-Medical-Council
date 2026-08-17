using AI.MedicalCouncil.Models;

namespace AI.MedicalCouncil.ViewModels;

public class PatientDetailVm
{
    public Patient Patient { get; set; } = null!;
    public List<Encounter> Encounters { get; set; } = new();
    public List<Medication> Medications { get; set; } = new();
    public List<AiCouncilSession> CouncilSessions { get; set; } = new();
}
