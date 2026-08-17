using System.ComponentModel.DataAnnotations;

namespace AI.MedicalCouncil.Models;

public class Medication
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Dose { get; set; }

    public bool IsActive { get; set; } = true;
}
