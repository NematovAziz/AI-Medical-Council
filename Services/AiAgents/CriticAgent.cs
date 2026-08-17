using AI.MedicalCouncil.Services;

namespace AI.MedicalCouncil.Services.AiAgents;

public class CriticAgent(HttpClient http, IAgentConfigProvider config)
    : ConfigurableAiAgentBase<CriticAgent>(http, config), ICriticAgent
{
    public override string AgentName => "AI Kritik";
    public override string Specialty => "Ikkinchi raund · nazorat";
    protected override string OptionName => "Critic";
    protected override int Round => 2;
    protected override string SystemPrompt =>
        "You are the council's critic and you see the round-1 conclusions of the other specialists. Judge whether their reasoning holds: name the weakest conclusion, the contradiction between them, or the missing data that would change the picture, and give your own corrected reading.";
}
