using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class PrescriptionItem
{
    public int PrescriptionItemId { get; set; }

    public int PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }

    [Required, Display(Name = "Medicine")]
    public string MedicineName { get; set; } = string.Empty;

    [Required]
    public string Dosage { get; set; } = string.Empty;

    [Required]
    public string Frequency { get; set; } = string.Empty;

    [Required]
    public string Duration { get; set; } = string.Empty;

    public string? Instructions { get; set; }
}