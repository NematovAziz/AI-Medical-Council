using AI.MedicalCouncil.Services;

namespace AI.MedicalCouncil.Services.AiAgents;

public class SafetyAgent(HttpClient http, IAgentConfigProvider config)
    : ConfigurableAiAgentBase<SafetyAgent>(http, config), ISafetyAgent
{
    public override string AgentName => "Safety Agent";
    public override string Specialty => "Ikkinchi raund · qizil bayroq";
    protected override string OptionName => "Safety";
    protected override int Round => 2;
    protected override string SystemPrompt =>
        "You are the safety gatekeeper and you see the round-1 conclusions. Decide whether this patient needs emergency escalation right now. Name the specific red flag and the time frame. Err on the side of escalation when severe objective abnormalities are present.";
}
