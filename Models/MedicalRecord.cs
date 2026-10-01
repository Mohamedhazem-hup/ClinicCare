using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class MedicalRecord
{
    public int MedicalRecordId { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int Id { get => MedicalRecordId; set => MedicalRecordId = value; }

    [Required, Display(Name = "Patient")]
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    [Required, Display(Name = "Doctor")]
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    [Display(Name = "Visit Date")]
    [DataType(DataType.Date)]
    public DateTime VisitDate { get; set; } = DateTime.Now;

    [Required]
    public string Diagnosis { get; set; } = string.Empty;

    [Display(Name = "Chief Complaint")]
    public string? ChiefComplaint { get; set; }

    public string? Symptoms { get; set; }

    [Display(Name = "Blood Pressure")]
    public string? BloodPressure { get; set; }

    [Display(Name = "Temperature (°C)")]
    public decimal? Temperature { get; set; }

    [Display(Name = "Weight (kg)")]
    public decimal? Weight { get; set; }

    public string? Treatment { get; set; }

    public string? Notes { get; set; }

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
