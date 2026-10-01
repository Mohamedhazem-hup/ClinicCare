namespace ClinicManagementSystem.ViewModels;

public class DoctorAppointmentReportItem
{
    public string DoctorName { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
}

public class MonthlyRevenueReportItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalRevenue { get; set; }
    public string MonthLabel => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
}

public class OverdueBillReportItem
{
    public int BillId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal Balance { get; set; }
}

public class ReportsViewModel
{
    public List<DoctorAppointmentReportItem> AppointmentsByDoctor { get; set; } = new();
    public List<MonthlyRevenueReportItem> RevenueByMonth { get; set; } = new();
    public List<OverdueBillReportItem> OverdueBills { get; set; } = new();
    public decimal RevenueToday { get; set; }
    public decimal OverdueBalance { get; set; }
    public int NewPatientsToday { get; set; }
}