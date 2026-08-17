using System.ComponentModel.DataAnnotations;

namespace AI.MedicalCouncil.Models;

public class Encounter
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    [StringLength(1500)] public string Symptoms { get; set; } = string.Empty;
    [StringLength(2000)] public string? Anamnesis { get; set; }

    // core vitals
    public double? Hemoglobin { get; set; }
    public double? Glucose { get; set; }
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public int? HeartRate { get; set; }
    public double? SpO2 { get; set; }

    // extended vitals
    public double? Temperature { get; set; }
    public int? RespiratoryRate { get; set; }
    public double? HeightCm { get; set; }
    public double? WeightKg { get; set; }
    public int? PainScore { get; set; }

    /// <summary>Triage category: Yashil | Sariq | Qizil</summary>
    [StringLength(20)] public string Triage { get; set; } = "Yashil";

    [StringLength(20)] public string? Icd10 { get; set; }
    [StringLength(1500)] public string? EcgSummary { get; set; }
    [StringLength(1500)] public string? ImagingSummary { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }

    /// <summary>Set when the encounter was generated from an uploaded lab document.</summary>
    public int? SourceLabDocumentId { get; set; }

    public double? Bmi => HeightCm is > 0 && WeightKg is > 0
        ? Math.Round(WeightKg!.Value / Math.Pow(HeightCm!.Value / 100.0, 2), 1)
        : null;
}
