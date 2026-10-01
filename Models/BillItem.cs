using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models;

public class BillItem
{
    public int BillItemId { get; set; }

    public int BillId { get; set; }
    public Bill? Bill { get; set; }

    [Required, Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; } = 1;

    [Range(0, 100000000)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal LineTotal => Quantity * UnitPrice;
}