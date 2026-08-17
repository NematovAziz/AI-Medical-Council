using AI.MedicalCouncil.Models;
using Microsoft.EntityFrameworkCore;

namespace AI.MedicalCouncil.Data;

/// <summary>
/// Demo cohort designed for a live walkthrough: one stable patient, one trending-worse patient,
/// and one patient who will immediately trigger a critical council verdict.
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.Patients.AnyAsync()) return;

        var now = DateTime.UtcNow;

        // ---------- 1. stable ----------
        var stable = new Patient
        {
            FullName = "Aziza Rahimova",
            BirthDate = new DateOnly(1992, 4, 3),
            Sex = "Ayol",
            Phone = "+998 90 111 22 33",
            Allergies = "Ma'lum emas",
            ChronicConditions = null,
            CreatedAtUtc = now.AddDays(-120)
        };

        // ---------- 2. deteriorating trend ----------
        var trending = new Patient
        {
            FullName = "Bekzod Yusupov",
            BirthDate = new DateOnly(1968, 11, 21),
            Sex = "Erkak",
            Phone = "+998 90 444 55 66",
            Allergies = "Penitsillin",
            ChronicConditions = "2-tip qandli diabet, arterial gipertenziya",
            CreatedAtUtc = now.AddDays(-400)
        };

        // ---------- 3. acute ----------
        var acute = new Patient
        {
            FullName = "Sardor Karimov",
            BirthDate = new DateOnly(1957, 2, 9),
            Sex = "Erkak",
            Phone = "+998 90 777 88 99",
            Allergies = "Ma'lum emas",
            ChronicConditions = "Yurak ishemik kasalligi",
            CreatedAtUtc = now.AddDays(-30)
        };

        db.Patients.AddRange(stable, trending, acute);
        await db.SaveChangesAsync();

        db.Encounters.AddRange(
            new Encounter
            {
                PatientId = stable.Id, OccurredAtUtc = now.AddDays(-118),
                Symptoms = "Profilaktik ko'rik", Anamnesis = "Shikoyatlar mavjud emas",
                Hemoglobin = 134, Glucose = 4.9, SystolicBp = 118, DiastolicBp = 76,
                HeartRate = 68, SpO2 = 99, EcgSummary = "Sinus ritmi", Notes = "Norma"
            },
            new Encounter
            {
                PatientId = stable.Id, OccurredAtUtc = now.AddDays(-14),
                Symptoms = "Yengil charchoq", Anamnesis = "Ish yuklamasi ortgan",
                Hemoglobin = 128, Glucose = 5.1, SystolicBp = 121, DiastolicBp = 78,
                HeartRate = 74, SpO2 = 98, EcgSummary = "Sinus ritmi", Notes = "Kuzatuv"
            },

            new Encounter
            {
                PatientId = trending.Id, OccurredAtUtc = now.AddDays(-360),
                Symptoms = "Reja bo'yicha nazorat", Anamnesis = "Diabet kompensatsiyada",
                Hemoglobin = 141, Glucose = 6.4, SystolicBp = 138, DiastolicBp = 86,
                HeartRate = 78, SpO2 = 97, EcgSummary = "Sinus ritmi"
            },
            new Encounter
            {
                PatientId = trending.Id, OccurredAtUtc = now.AddDays(-180),
                Symptoms = "Bosh og'rig'i, charchoq", Anamnesis = "Dori rejimi buzilgan",
                Hemoglobin = 132, Glucose = 7.8, SystolicBp = 149, DiastolicBp = 92,
                HeartRate = 84, SpO2 = 96, EcgSummary = "Chap qorincha gipertrofiyasi belgilari"
            },
            new Encounter
            {
                PatientId = trending.Id, OccurredAtUtc = now.AddDays(-21),
                Symptoms = "Bosh og'rig'i, bosh aylanish, hansirash", Anamnesis = "Bosim ko'tarilishi tez-tez",
                Hemoglobin = 124, Glucose = 9.2, SystolicBp = 166, DiastolicBp = 98,
                HeartRate = 96, SpO2 = 95, EcgSummary = "Chap qorincha gipertrofiyasi",
                ImagingSummary = "Ko'krak qafasi rentgeni: yurak soyasi kengaygan"
            },

            new Encounter
            {
                PatientId = acute.Id, OccurredAtUtc = now.AddDays(-210),
                Symptoms = "Arxivdan kiritilgan laboratoriya paneli",
                Anamnesis = "Tahlil 7 oy oldin topshirilgan, tizimga keyinroq kiritildi",
                Hemoglobin = 143, Glucose = 5.1, SystolicBp = 134, DiastolicBp = 84,
                HeartRate = 76, SpO2 = 97, EcgSummary = "Sinus ritmi",
                Notes = "Retrospektiv yozuv"
            },
            new Encounter
            {
                PatientId = acute.Id, OccurredAtUtc = now.AddDays(-25),
                Symptoms = "Yurish paytida ko'krak siqilishi", Anamnesis = "IHD anamnezi",
                Hemoglobin = 138, Glucose = 5.6, SystolicBp = 142, DiastolicBp = 88,
                HeartRate = 82, SpO2 = 96, EcgSummary = "Sinus ritmi, ST segmenti chegaraviy"
            },
            new Encounter
            {
                PatientId = acute.Id, OccurredAtUtc = now.AddHours(-2),
                Symptoms = "To'satdan ko'krak og'rig'i, kuchli hansirash, sovuq ter",
                Anamnesis = "Og'riq 40 daqiqadan beri davom etmoqda",
                Hemoglobin = 129, Glucose = 7.4, SystolicBp = 92, DiastolicBp = 58,
                HeartRate = 134, SpO2 = 88,
                EcgSummary = "ST segmenti elevatsiyasi, taxikardiya",
                ImagingSummary = "Ko'krak qafasi: o'pkada dimlanish belgilari",
                Temperature = 36.9, RespiratoryRate = 28, HeightCm = 176, WeightKg = 88, PainScore = 8,
                Triage = "Qizil", Icd10 = "I21.9",
                Notes = "Shoshilinch qabul"
            });

        db.Medications.AddRange(
            new Medication { PatientId = trending.Id, Name = "Metformin", Dose = "1000 mg 2 mahal", IsActive = true },
            new Medication { PatientId = trending.Id, Name = "Lizinopril", Dose = "10 mg", IsActive = true },
            new Medication { PatientId = trending.Id, Name = "Amlodipin", Dose = "5 mg", IsActive = true },
            new Medication { PatientId = trending.Id, Name = "Atorvastatin", Dose = "20 mg", IsActive = true },
            new Medication { PatientId = trending.Id, Name = "Aspirin", Dose = "75 mg", IsActive = true },
            new Medication { PatientId = acute.Id, Name = "Bisoprolol", Dose = "5 mg", IsActive = true },
            new Medication { PatientId = acute.Id, Name = "Klopidogrel", Dose = "75 mg", IsActive = true });

        await db.SaveChangesAsync();

        // Demo lab panel so the analyte table is populated before the first upload.
        var panelDate = now.AddDays(-21);
        var document = new LabDocument
        {
            PatientId = trending.Id,
            FileName = "umumiy-qon-tahlili-demo.txt",
            ContentType = "text/plain",
            SizeBytes = 512,
            StoredPath = "/uploads/demo/umumiy-qon-tahlili-demo.txt",
            CollectedAtUtc = panelDate,
            UploadedAtUtc = panelDate,
            Status = "Tayyor",
            ExtractionSource = "Lokal parser",
            Summary = "8 ta ko'rsatkich ajratildi, 3 tasi me'yordan chetda.",
            RawText = "Gemoglobin 124 g/L (120-160)\nLeykotsitlar 9.8 10^9/L (4.0-9.0)\nGlyukoza 9.2 mmol/L (3.9-6.1)",
            ExtractedCount = 8,
            AbnormalCount = 3,
            DurationMs = 42,
            Results = new List<LabResult>
            {
                new() { PatientId = trending.Id, Analyte = "Gemoglobin",    Code = "718-7",  Value = 124,  Unit = "g/L",     RefLow = 120, RefHigh = 160, Flag = "N", ObservedAtUtc = panelDate },
                new() { PatientId = trending.Id, Analyte = "Leykotsitlar",  Code = "6690-2", Value = 9.8,  Unit = "10^9/L",  RefLow = 4,   RefHigh = 9,   Flag = "H", ObservedAtUtc = panelDate },
                new() { PatientId = trending.Id, Analyte = "Trombotsitlar", Code = "777-3",  Value = 244,  Unit = "10^9/L",  RefLow = 150, RefHigh = 400, Flag = "N", ObservedAtUtc = panelDate },
                new() { PatientId = trending.Id, Analyte = "Glyukoza",      Code = "2345-7", Value = 9.2,  Unit = "mmol/L",  RefLow = 3.9, RefHigh = 6.1, Flag = "H", ObservedAtUtc = panelDate },
                new() { PatientId = trending.Id, Analyte = "HbA1c",         Code = "4548-4", Value = 8.1,  Unit = "%",       RefLow = 4,   RefHigh = 5.7, Flag = "H", ObservedAtUtc = panelDate },
                new() { PatientId = trending.Id, Analyte = "Kreatinin",     Code = "2160-0", Value = 96,   Unit = "µmol/L",  RefLow = 62,  RefHigh = 106, Flag = "N", ObservedAtUtc = panelDate },
                new() { PatientId = trending.Id, Analyte = "ALT",           Code = "1742-6", Value = 33,   Unit = "U/L",     RefLow = 0,   RefHigh = 41,  Flag = "N", ObservedAtUtc = panelDate },
                new() { PatientId = trending.Id, Analyte = "Umumiy xolesterin", Code = "2093-3", Value = 5.0, Unit = "mmol/L", RefLow = 3, RefHigh = 5.2, Flag = "N", ObservedAtUtc = panelDate }
            }
        };
        db.LabDocuments.Add(document);

        db.AuditLogs.Add(new AuditLog
        {
            Action = "SEED",
            Entity = "Database",
            Details = "Demo klinik ma'lumotlar yuklandi: 3 bemor, 8 tashrif, 7 dori."
        });

        await db.SaveChangesAsync();
    }
}
