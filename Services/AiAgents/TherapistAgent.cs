using AI.MedicalCouncil.Services;

namespace AI.MedicalCouncil.Services.AiAgents;

public class TherapistAgent(HttpClient http, IAgentConfigProvider config)
    : ConfigurableAiAgentBase<TherapistAgent>(http, config), ITherapistAgent
{
    public override string AgentName => "AI Terapevt";
    public override string Specialty => "Umumiy klinik ko'rinish";
    protected override string OptionName => "Therapist";
    protected override string SystemPrompt =>
        "You are the general internal-medicine specialist of the council. Integrate symptoms, history, vitals and labs into one working diagnosis of the overall clinical picture, and state which system is most likely driving it.";
}
