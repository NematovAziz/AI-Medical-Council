using AI.MedicalCouncil.Models;
using AI.MedicalCouncil.Services;

namespace AI.MedicalCouncil.ViewModels;

public class CouncilLiveVm
{
    public Patient Patient { get; set; } = null!;
    public Encounter Encounter { get; set; } = null!;
    public IReadOnlyList<AgentRoster> Roster { get; set; } = Array.Empty<AgentRoster>();
}
