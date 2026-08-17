using System.ComponentModel.DataAnnotations;

namespace AI.MedicalCouncil.ViewModels;

public class NewEncounterVm
{
    public int PatientId { get; set; }

    /// <summary>
    /// Clinical date of the visit or of the lab panel. Defaults to now, but a physician entering
    /// results from an earlier date sets it here — the council result is then filed on the timeline
    /// at that date, not at the moment the analysis was run.
    /// </summary>
    [Display(Name = "Tashrif sanasi")]
    public DateTime OccurredAt { get; set; } = DateTime.Now;

    [Required, StringLength(1500)] public string Symptoms { get; set; } = string.Empty;
    public string? Anamnesis { get; set; }
    public double? Hemoglobin { get; set; }
    public double? Glucose { get; set; }
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public int? HeartRate { get; set; }
    public double? SpO2 { get; set; }
    public double? Temperature { get; set; }
    public int? RespiratoryRate { get; set; }
    public double? HeightCm { get; set; }
    public double? WeightKg { get; set; }
    public int? PainScore { get; set; }
    public string Triage { get; set; } = "Yashil";
    public string? Icd10 { get; set; }
    public string? EcgSummary { get; set; }
    public string? ImagingSummary { get; set; }
    public string? Notes { get; set; }
}
