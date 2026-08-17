using System.ComponentModel.DataAnnotations;

namespace AI.MedicalCouncil.Models;

public class Patient
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateOnly BirthDate { get; set; }

    [StringLength(20)]
    public string Sex { get; set; } = "Noma'lum";

    [StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Allergies { get; set; }

    [StringLength(1000)]
    public string? ChronicConditions { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<Encounter> Encounters { get; set; } = new();
    public List<Medication> Medications { get; set; } = new();
    public List<AiCouncilSession> CouncilSessions { get; set; } = new();
}
