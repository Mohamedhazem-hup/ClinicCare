using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class Prescription
{
    public int PrescriptionId { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int Id { get => PrescriptionId; set => PrescriptionId = value; }

    public int? MedicalRecordId { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }

    [Required, Display(Name = "Patient")]
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    [Required, Display(Name = "Doctor")]
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateTime DateIssued { get => Date; set => Date = value; }

    public string MedicineName { get; set; } = string.Empty;

    public string? Dosage { get; set; }

    public string? Instructions { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Medications { get => MedicineName; set => MedicineName = value; }

    public string? Notes { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();

}
