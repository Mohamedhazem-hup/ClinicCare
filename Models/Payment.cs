using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models;

public class Payment
{
    public int PaymentId { get; set; }

    public int BillId { get; set; }
    public Bill? Bill { get; set; }

    [Range(0.01, 100000000)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    public string Method { get; set; } = "Cash";

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    public string? RecordedByUserId { get; set; }
    public ApplicationUser? RecordedBy { get; set; }
}