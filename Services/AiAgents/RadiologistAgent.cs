using AI.MedicalCouncil.Services;

namespace AI.MedicalCouncil.Services.AiAgents;

public class RadiologistAgent(HttpClient http, IAgentConfigProvider config)
    : ConfigurableAiAgentBase<RadiologistAgent>(http, config), IRadiologistAgent
{
    public override string AgentName => "AI Radiolog";
    public override string Specialty => "Tasvir tekshiruvlari";
    protected override string OptionName => "Radiology";
    protected override string SystemPrompt =>
        "You are the radiology specialist. Interpret only the imaging description supplied by the clinician — never invent findings. Say what the description most likely represents and which imaging study should follow.";
}
