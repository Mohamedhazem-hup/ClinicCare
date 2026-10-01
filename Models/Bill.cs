using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models;

public class Bill
{
    public int BillId { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int Id { get => BillId; set => BillId = value; }

    [Required, Display(Name = "Patient")]
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string? Description { get; set; }

    [Required, Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    public string PaymentStatus { get; set; } = "Unpaid";

    [NotMapped]
    public BillStatus Status
    {
        get => Enum.TryParse<BillStatus>(PaymentStatus, out var status) ? status : BillStatus.Pending;
        set => PaymentStatus = value.ToString();
    }

    public DateTime BillDate { get; set; } = DateTime.Now;

    [NotMapped]
    public DateTime IssueDate { get => BillDate; set => BillDate = value; }

    public DateTime? PaidDate { get; set; }

    public DateTime? DueDate { get; set; }

    public ICollection<BillItem> Items { get; set; } = new List<BillItem>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

}
