using System.ComponentModel.DataAnnotations;

namespace AI.MedicalCouncil.Models;

/// <summary>One analyte extracted from a lab document, with its reference range and flag.</summary>
public class LabResult
{
    public int Id { get; set; }
    public int LabDocumentId { get; set; }
    public LabDocument Document { get; set; } = null!;
    public int PatientId { get; set; }

    [StringLength(120)] public string Analyte { get; set; } = string.Empty;
    [StringLength(40)] public string? Code { get; set; }

    public double Value { get; set; }
    [StringLength(30)] public string Unit { get; set; } = string.Empty;

    public double? RefLow { get; set; }
    public double? RefHigh { get; set; }

    /// <summary>N (norma) | L (past) | H (yuqori) | C (kritik)</summary>
    [StringLength(2)] public string Flag { get; set; } = "N";

    [StringLength(300)] public string? Comment { get; set; }
    public DateTime ObservedAtUtc { get; set; } = DateTime.UtcNow;
}
