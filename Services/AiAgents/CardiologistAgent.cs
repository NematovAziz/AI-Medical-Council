using AI.MedicalCouncil.Services;

namespace AI.MedicalCouncil.Services.AiAgents;

public class CardiologistAgent(HttpClient http, IAgentConfigProvider config)
    : ConfigurableAiAgentBase<CardiologistAgent>(http, config), ICardiologistAgent
{
    public override string AgentName => "AI Kardiolog";
    public override string Specialty => "Yurak-qon tomir tizimi";
    protected override string OptionName => "Cardiology";
    protected override string SystemPrompt =>
        "You are the cardiology specialist. Judge blood pressure, heart rate, SpO2, ECG summary and cardiac markers together, name the most probable cardiovascular condition and say whether it needs emergency handling.";
}
