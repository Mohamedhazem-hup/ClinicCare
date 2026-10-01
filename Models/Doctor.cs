using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class Doctor
{
    public int DoctorId { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int Id { get => DoctorId; set => DoctorId = value; }

    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

    [Required, Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Specialty { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Specialization { get => Specialty; set => Specialty = value; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
