using System.ComponentModel.DataAnnotations;

namespace AI.MedicalCouncil.Models;

/// <summary>An uploaded lab report. The AI extractor turns it into structured <see cref="LabResult"/> rows.</summary>
public class LabDocument
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [StringLength(260)] public string FileName { get; set; } = string.Empty;
    [StringLength(120)] public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    [StringLength(300)] public string StoredPath { get; set; } = string.Empty;

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Collection date parsed from the document, or the upload time when absent.</summary>
    public DateTime CollectedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Kutilmoqda | Tahlil qilinmoqda | Tayyor | Xatolik</summary>
    [StringLength(30)] public string Status { get; set; } = "Kutilmoqda";

    /// <summary>"AI · model" or "Lokal parser" — every row stays auditable.</summary>
    [StringLength(120)] public string ExtractionSource { get; set; } = "Lokal parser";

    [StringLength(600)] public string Summary { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;

    public int ExtractedCount { get; set; }
    public int AbnormalCount { get; set; }
    public int DurationMs { get; set; }

    public int? EncounterId { get; set; }

    public List<LabResult> Results { get; set; } = new();
}
