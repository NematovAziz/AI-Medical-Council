using AI.MedicalCouncil.Models;

namespace AI.MedicalCouncil.Services.AiAgents;

/// <summary>
/// Everything an agent is allowed to see. <see cref="PeerFindings"/> is only populated in round 2,
/// where reviewing agents (Critic, Safety) can challenge what the specialists concluded in round 1.
/// </summary>
public record AgentInput(
    Patient Patient,
    Encounter Encounter,
    IReadOnlyList<Encounter> History,
    IReadOnlyList<Medication> Medications,
    IReadOnlyList<AgentOutput>? PeerFindings = null,
    IReadOnlyList<LabResult>? LabResults = null);

public record AgentOutput(
    string AgentName,
    string Finding,
    int Confidence,
    string Severity = "Info",
    string Source = "",
    int Round = 1,
    int LatencyMs = 0,
    /// <summary>False when the model could not be reached — the finding is then a status, not an opinion.</summary>
    bool Available = true);

public interface IMedicalAiAgent
{
    string AgentName { get; }
    string Specialty { get; }
    /// <summary>Extra instruction appended to the shared diagnostic prompt.</summary>
    Task<AgentOutput> AnalyzeAsync(AgentInput input, CancellationToken ct = default);
}

public interface ITherapistAgent : IMedicalAiAgent { }
public interface ILabAgent : IMedicalAiAgent { }
public interface ICardiologistAgent : IMedicalAiAgent { }
public interface IRadiologistAgent : IMedicalAiAgent { }
public interface IPharmacologistAgent : IMedicalAiAgent { }
public interface ICriticAgent : IMedicalAiAgent { }
public interface ISafetyAgent : IMedicalAiAgent { }
