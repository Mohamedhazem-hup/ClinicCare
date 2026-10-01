namespace ClinicManagementSystem.Models;

public enum AppointmentStatus
{
    Scheduled = 0,
    Completed = 1,
    Cancelled = 2,
    Waiting = 3,
    CheckedIn = 4,
    NoShow = 5
}

public enum BillStatus
{
    Unpaid,
    Pending,
    Partial,
    Paid,
    Cancelled
}
