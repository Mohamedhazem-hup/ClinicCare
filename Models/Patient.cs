using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class Patient
{
    public int PatientId { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int Id { get => PatientId; set => PatientId = value; }

    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

    [Required, Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    public string? Address { get; set; }

    [Display(Name = "Blood Type")]
    public string? BloodType { get; set; }

    public string? Email { get; set; }

    [Display(Name = "Emergency Contact")]
    public string? EmergencyContact { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [Display(Name = "Patient ID")]
    public string PatientNumber => $"P-{10024 + PatientId}";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
}
