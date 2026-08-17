using AI.MedicalCouncil.Services;

namespace AI.MedicalCouncil.Services.AiAgents;

public class PharmacologistAgent(HttpClient http, IAgentConfigProvider config)
    : ConfigurableAiAgentBase<PharmacologistAgent>(http, config), IPharmacologistAgent
{
    public override string AgentName => "AI Farmakolog";
    public override string Specialty => "Dori xavfsizligi";
    protected override string OptionName => "Pharmacology";
    protected override string SystemPrompt =>
        "You are the clinical pharmacology specialist. Screen the active medication list against allergies, chronic conditions, renal and hepatic markers and current vitals. Name the most important interaction, contraindication or dosing risk. Never prescribe.";
}
