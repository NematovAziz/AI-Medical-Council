using AI.MedicalCouncil.Services;

namespace AI.MedicalCouncil.Services.AiAgents;

public class LabAgent(HttpClient http, IAgentConfigProvider config)
    : ConfigurableAiAgentBase<LabAgent>(http, config), ILabAgent
{
    public override string AgentName => "AI Laborant";
    public override string Specialty => "Laboratoriya ko'rsatkichlari";
    protected override string OptionName => "Lab";
    protected override string SystemPrompt =>
        "You are the laboratory-medicine specialist. Interpret the analyte pattern as a whole, not value by value: name the syndrome the numbers describe (anaemia type, glycaemic disorder, inflammation, organ dysfunction) and which further test would confirm it.";
}
