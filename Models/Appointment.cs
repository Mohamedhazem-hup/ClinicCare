using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class Appointment
{
    public int AppointmentId { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int Id { get => AppointmentId; set => AppointmentId = value; }

    [Required, Display(Name = "Patient")]
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    [Required, Display(Name = "Doctor")]
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    [Required, Display(Name = "Date & Time")]
    [DataType(DataType.DateTime)]
    public DateTime AppointmentDate { get; set; }

    [Required, Range(5, 240), Display(Name = "Duration (minutes)")]
    public int DurationMinutes { get; set; } = 30;

    [Required]
    public string Status { get; set; } = "Scheduled";

    public string? Notes { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? Reason { get => Notes; set => Notes = value; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
