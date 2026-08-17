namespace AI.MedicalCouncil.Services.Labs;

public record AnalyteDefinition(
    string Canonical,
    string Code,
    string Unit,
    double? RefLow,
    double? RefHigh,
    double? CriticalLow,
    double? CriticalHigh,
    string[] Synonyms);

/// <summary>
/// Built-in analyte dictionary. Synonyms cover Uzbek, Russian and English report wording plus the
/// short codes analysers print, so the local parser still works when no extraction API is configured.
/// </summary>
public static class ReferenceRanges
{
    public static readonly AnalyteDefinition[] All =
    {
        new("Gemoglobin", "718-7", "g/L", 120, 160, 70, 200,
            new[]{"gemoglobin","гемоглобин","hemoglobin","haemoglobin","hgb","hb"}),
        new("Eritrotsitlar", "789-8", "10^12/L", 3.8, 5.6, 2.0, 7.0,
            new[]{"eritrotsit","эритроциты","erythrocytes","rbc"}),
        new("Leykotsitlar", "6690-2", "10^9/L", 4.0, 9.0, 1.5, 30.0,
            new[]{"leykotsit","лейкоциты","leukocytes","wbc"}),
        new("Trombotsitlar", "777-3", "10^9/L", 150, 400, 50, 800,
            new[]{"trombotsit","тромбоциты","platelets","plt"}),
        new("EChT", "4537-7", "mm/soat", 2, 20, null, 80,
            new[]{"echt","соэ","esr","eritrotsitlar cho'kish"}),
        new("Glyukoza", "2345-7", "mmol/L", 3.9, 6.1, 2.8, 11.1,
            new[]{"glyukoza","глюкоза","glucose","glu","qand"}),
        new("HbA1c", "4548-4", "%", 4.0, 5.7, null, 10.0,
            new[]{"hba1c","гликированный","glycated","a1c"}),
        new("Umumiy xolesterin", "2093-3", "mmol/L", 3.0, 5.2, null, 8.0,
            new[]{"xolesterin","холестерин","cholesterol","chol"}),
        new("Kreatinin", "2160-0", "µmol/L", 62, 106, null, 300,
            new[]{"kreatinin","креатинин","creatinine","crea"}),
        new("Mochevina", "3094-0", "mmol/L", 2.5, 8.3, null, 30,
            new[]{"mochevina","мочевина","urea","bun"}),
        new("ALT", "1742-6", "U/L", 0, 41, null, 200,
            new[]{"alt","алт","alat","sgpt"}),
        new("AST", "1920-8", "U/L", 0, 40, null, 200,
            new[]{"ast","аст","asat","sgot"}),
        new("Umumiy bilirubin", "1975-2", "µmol/L", 3.4, 20.5, null, 100,
            new[]{"bilirubin","билирубин"}),
        new("C-reaktiv oqsil", "1988-5", "mg/L", 0, 5, null, 100,
            new[]{"crp","с-реактивный","c-reaktiv","reaktiv oqsil"}),
        new("Ferritin", "2276-4", "ng/mL", 30, 300, null, null,
            new[]{"ferritin","ферритин"}),
        new("Kaliy", "2823-3", "mmol/L", 3.5, 5.1, 2.5, 6.5,
            new[]{"kaliy","калий","potassium","k+"}),
        new("Natriy", "2951-2", "mmol/L", 136, 145, 120, 160,
            new[]{"natriy","натрий","sodium","na+"}),
        new("TSH", "3016-3", "mIU/L", 0.4, 4.0, null, 20,
            new[]{"tsh","ттг","thyrotropin","tireotrop"}),
        new("D-dimer", "48065-7", "ng/mL", 0, 500, null, 3000,
            new[]{"d-dimer","д-димер","ddimer"}),
        new("Troponin I", "10839-9", "ng/L", 0, 14, null, 100,
            new[]{"troponin","тропонин","trop"}),

        // ---- lipid panel ----
        new("HDL xolesterin", "2085-9", "mmol/L", 1.0, 2.2, null, null,
            new[]{"hdl","hdlxolesterin","hdlcholesterol","лпвп"}),
        new("LDL xolesterin", "2089-1", "mmol/L", 0, 3.0, null, null,
            new[]{"ldl","ldlxolesterin","ldlcholesterol","лпнп"}),
        new("Triglitseridlar", "2571-8", "mmol/L", 0.4, 1.7, null, null,
            new[]{"triglitserid","триглицериды","triglycerides"}),

        // ---- iron and vitamins ----
        new("Temir", "2498-4", "µmol/L", 9, 30, null, null,
            new[]{"temir","железо","iron"}),
        new("Transferrin", "3034-6", "g/L", 2.0, 3.6, null, null,
            new[]{"transferrin","трансферрин"}),
        new("Vitamin B12", "2132-9", "pg/mL", 190, 880, null, null,
            new[]{"b12","vitaminb12","витаминb12","cobalamin","kobalamin"}),
        new("Folat", "2284-8", "ng/mL", 3.1, 20, null, null,
            new[]{"folat","фолиевая","folate","folievaya"}),
        new("Vitamin D", "1989-3", "ng/mL", 30, 100, null, null,
            new[]{"vitamind","витаминd","25ohd","25oh"}),

        // ---- protein and enzymes ----
        new("Umumiy oqsil", "2885-2", "g/L", 64, 83, null, null,
            new[]{"umumiyoqsil","общийбелок","totalprotein"}),
        new("Albumin", "1751-7", "g/L", 35, 52, null, null,
            new[]{"albumin","альбумин"}),
        new("Amilaza", "1798-8", "U/L", 28, 100, null, 600,
            new[]{"amilaza","амилаза","amylase"}),
        new("LDG", "2532-0", "U/L", 135, 225, null, 1000,
            new[]{"ldg","ldh","лдг"}),
        new("KFK-MB", "13969-1", "U/L", 0, 24, null, 100,
            new[]{"kfkmb","ckmb","кфкмв","кфк"}),
        new("Siydik kislotasi", "3084-1", "µmol/L", 200, 420, null, null,
            new[]{"siydikkislotasi","мочеваякислота","uricacid"}),

        // ---- coagulation ----
        new("Fibrinogen", "3255-7", "g/L", 2.0, 4.0, null, null,
            new[]{"fibrinogen","фибриноген"}),
        new("INR", "6301-6", "", 0.8, 1.2, null, 5,
            new[]{"inr","мно"}),
        new("APTV", "3173-2", "sek", 25, 38, null, null,
            new[]{"aptv","aptt","ачтв"}),

        // ---- thyroid and electrolytes ----
        new("Erkin T4", "3024-7", "pmol/L", 9, 22, null, null,
            new[]{"erkint4","freet4","свободныйт4","ft4","t4"}),
        new("Erkin T3", "3051-0", "pmol/L", 2.6, 5.7, null, null,
            new[]{"erkint3","freet3","свободныйт3","ft3","t3"}),
        new("Xlor", "2075-0", "mmol/L", 98, 107, null, null,
            new[]{"xlor","хлор","chloride"}),
        new("Kalsiy", "17861-6", "mmol/L", 2.15, 2.55, 1.7, 3.2,
            new[]{"kalsiy","кальций","calcium"})
    };

    public static AnalyteDefinition? Match(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return null;

        var name = Normalize(rawName);

        foreach (var d in All)
        {
            if (Normalize(d.Canonical) == name) return d;
            if (d.Synonyms.Any(sy => Normalize(sy) == name)) return d;
        }

        // Fall back to a containment match, longest synonym first so "hb" cannot hit "hba1c".
        // Short codes (tsh, alt, ast, hb, wbc) are deliberately excluded here: as substrings they
        // fire on ordinary words — an address line containing "Toshkent" would otherwise be read
        // as a TSH result. Short codes still match exactly, through the loop above.
        foreach (var d in All.OrderByDescending(x => x.Synonyms.Max(s => s.Length)))
        {
            if (d.Synonyms.OrderByDescending(s => s.Length)
                          .Any(sy => sy.Length >= 5 && name.Contains(Normalize(sy))))
                return d;
        }

        return null;
    }

    /// <summary>N normal, L low, H high, C critical.</summary>
    public static string Flag(AnalyteDefinition? d, double value, double? refLow, double? refHigh)
    {
        var low = refLow ?? d?.RefLow;
        var high = refHigh ?? d?.RefHigh;

        if (d is not null)
        {
            if (d.CriticalLow is { } cl && value <= cl) return "C";
            if (d.CriticalHigh is { } ch && value >= ch) return "C";
        }

        if (low is { } l && value < l) return "L";
        if (high is { } h && value > h) return "H";
        return "N";
    }

    private static string Normalize(string s) =>
        new string(s.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == '+').ToArray());
}
