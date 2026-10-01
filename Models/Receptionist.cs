using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class Receptionist
{
    public int ReceptionistId { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }
}