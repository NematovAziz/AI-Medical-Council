using System.ComponentModel.DataAnnotations;

namespace AI.MedicalCouncil.Models;

/// <summary>Append-only trace of every clinically meaningful action in the system.</summary>
public class AuditLog
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [StringLength(60)] public string Action { get; set; } = string.Empty;
    [StringLength(60)] public string Entity { get; set; } = string.Empty;
    [StringLength(80)] public string Actor { get; set; } = "clinician";
    [StringLength(600)] public string Details { get; set; } = string.Empty;
}
