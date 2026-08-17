using AI.MedicalCouncil.Models;
using AI.MedicalCouncil.Services.AiAgents;

namespace AI.MedicalCouncil.Services;

public record RiskAssessment(
    int Score,
    string Level,
    int ConsensusScore,
    string MainHypothesis,
    string AlternativeHypotheses,
    string RecommendedTests,
    string RedFlags,
    string Summary);

public interface IRiskEngine
{
    string Version { get; }
    RiskAssessment Evaluate(Patient patient, Encounter encounter, IReadOnlyList<AgentOutput> outputs);
}

/// <summary>
/// Deterministic, auditable scoring layer. Kept separate from the agents so the clinical logic
/// can be reviewed, versioned and tested without touching any model integration.
/// </summary>
public class RiskEngine : IRiskEngine
{
    public string Version => "risk-engine/4.0";

    public RiskAssessment Evaluate(Patient patient, Encounter encounter, IReadOnlyList<AgentOutput> allOutputs)
    {
        // Agents that could not reach their model have no opinion, so they carry no weight.
        var outputs = allOutputs.Where(o => o.Available).ToList();
        var offline = allOutputs.Count - outputs.Count;

        var criticals = outputs.Count(o => o.Severity == "Critical");
        var warnings = outputs.Count(o => o.Severity == "Warning");

        // 1. objective vital-sign component (0-55)
        var vitals = 0;
        if (encounter.SpO2 is { } spo2)
        {
            if (spo2 < 88) vitals += 30; else if (spo2 < 92) vitals += 18; else if (spo2 < 95) vitals += 8;
        }
        if (encounter.HeartRate is { } hr)
        {
            if (hr > 140 || hr < 35) vitals += 25; else if (hr > 120 || hr < 45) vitals += 14; else if (hr > 100) vitals += 6;
        }
        if (encounter.SystolicBp is { } sbp)
        {
            if (sbp > 200 || sbp < 80) vitals += 25; else if (sbp >= 180) vitals += 15; else if (sbp >= 160) vitals += 8;
        }
        if (encounter.Hemoglobin is { } hb)
        {
            if (hb < 90) vitals += 20; else if (hb < 110) vitals += 10; else if (hb < 130) vitals += 4;
        }
        if (encounter.Glucose is { } glu)
        {
            if (glu >= 11.1) vitals += 18; else if (glu >= 7.0) vitals += 9; else if (glu >= 6.1) vitals += 4;
        }
        if (encounter.Temperature is { } t)
        {
            if (t >= 39.5 || t <= 35.0) vitals += 14; else if (t >= 38.5) vitals += 7;
        }
        if (encounter.RespiratoryRate is { } rr)
        {
            if (rr > 30 || rr < 8) vitals += 16; else if (rr > 24) vitals += 8;
        }
        vitals = Math.Min(55, vitals);

        // 2. agent consensus component (0-40)
        var agentPart = Math.Min(40, criticals * 18 + warnings * 7);

        // 3. patient risk profile (0-10)
        var age = CalculateAge(patient.BirthDate);
        var profile = 0;
        if (age >= 65) profile += 5; else if (age >= 50) profile += 3;
        if (!string.IsNullOrWhiteSpace(patient.ChronicConditions)) profile += 4;
        profile = Math.Min(10, profile);

        var score = Math.Clamp(vitals + agentPart + profile, 0, 100);
        if (criticals > 0) score = Math.Max(score, 85);

        var level = score >= 85 ? "Kritik" : score >= 60 ? "Yuqori" : score >= 35 ? "O'rta" : "Past";

        // consensus = how tightly the council agrees (100 = full agreement)
        var consensus = 100;
        if (outputs.Count > 1)
        {
            var spread = outputs.Max(o => o.Confidence) - outputs.Min(o => o.Confidence);
            var severities = outputs.Select(o => o.Severity).Distinct().Count();
            consensus = Math.Clamp(100 - spread / 2 - (severities - 1) * 12, 20, 100);
        }

        var (main, alt, tests) = BuildHypotheses(encounter, criticals);

        var red = string.Join(" ", outputs.Where(o => o.Severity == "Critical").Select(o => o.Finding));
        if (string.IsNullOrWhiteSpace(red)) red = "Kritik qizil bayroq aniqlanmadi.";

        var summary = outputs.Count == 0
            ? "Birorta agent javob bermadi — API kalitlarini Sozlamalar bo'limida tekshiring. " +
              "Xavf bahosi faqat obyektiv ko'rsatkichlar asosida hisoblandi."
            : $"{outputs.Count} ta AI agent mustaqil tashxis qo'ydi" +
              (offline > 0 ? $" ({offline} ta agent ulanmadi)" : "") + ". " +
              $"Kritik signal: {criticals}, ogohlantirish: {warnings}. Konsensus darajasi {consensus}%. " +
              "Yakuniy klinik qaror faqat shifokorga tegishli.";

        return new RiskAssessment(score, level, consensus, main, alt, tests, red, summary);
    }

    private static (string Main, string Alt, string Tests) BuildHypotheses(Encounter e, int criticals)
    {
        if (criticals > 0 && (e.SpO2 is < 90 || e.HeartRate is > 130 or < 40 || e.SystolicBp is > 200))
            return (
                "Shoshilinch kardiopulmonal holat ehtimolini birinchi navbatda istisno qilish zarur.",
                "O'tkir yurak yetishmovchiligi; aritmiya; nafas yetishmovchiligi; sepsis boshlanishi; boshqa shoshilinch sabablar.",
                "Shoshilinch EKG, arterial qon gazlari, troponin, ko'krak qafasi tasviri va monitoring — shifokor ko'rsatmasi bo'yicha.");

        if (e.Hemoglobin is < 120)
            return (
                "Anemiya sindromi ehtimolini shifokor aniqlashtirishi zarur.",
                "Temir tanqisligi; B12/folat tanqisligi; surunkali kasallik anemiyasi; yashirin qon yo'qotish.",
                "Ferritin, transferrin to'yinishi, B12, folat, retikulotsitlar va klinik ko'rsatmaga mos tekshiruvlar.");

        if (e.Glucose is >= 6.1)
            return (
                "Glyukoza almashinuvi buzilishi ehtimolini aniqlashtirish zarur.",
                "Prediabet; qandli diabet; stress giperglikemiyasi; metabolik sindrom komponentlari.",
                "Takroriy och qoringa glyukoza, HbA1c, lipid profil va shifokor ko'rsatmasiga mos tekshiruvlar.");

        if (e.SystolicBp is >= 140 || e.DiastolicBp is >= 90)
            return (
                "Arterial gipertenziya ehtimolini dinamik nazorat bilan tasdiqlash zarur.",
                "Birlamchi gipertenziya; ikkilamchi sabablar; \"oq xalat\" effekti.",
                "Sutkalik qon bosimi monitoringi, kreatinin, elektrolitlar, siydik tahlili, EKG.");

        return (
            "Mavjud ma'lumotlarda bitta ustun tashxis uchun dalil yetarli emas.",
            "Simptomlarga mos alternativ sabablar differensial tartibda baholanadi.",
            "Simptom, anamnez va xavf profiliga mos maqsadli tekshiruvlar.");
    }

    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age)) age--;
        return Math.Max(0, age);
    }
}
