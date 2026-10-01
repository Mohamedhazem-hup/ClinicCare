using ClinicManagementSystem.Data;
using ClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers;

[Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var appointmentsByDoctor = await _context.Appointments
            .Include(a => a.Doctor)
            .Where(a => a.Doctor != null)
            .GroupBy(a => a.Doctor!.FullName)
            .Select(group => new DoctorAppointmentReportItem
            {
                DoctorName = group.Key,
                TotalAppointments = group.Count(),
                Completed = group.Count(a => a.Status == "Completed"),
                Cancelled = group.Count(a => a.Status == "Cancelled")
            })
            .OrderByDescending(item => item.TotalAppointments)
            .ToListAsync();

        var revenueByMonth = await _context.Payments
            .GroupBy(payment => new { payment.PaymentDate.Year, payment.PaymentDate.Month })
            .Select(group => new MonthlyRevenueReportItem
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                TotalRevenue = group.Sum(payment => payment.Amount)
            })
            .OrderBy(item => item.Year)
            .ThenBy(item => item.Month)
            .ToListAsync();

        var today = DateTime.Today;
        var overdueBills = await _context.Bills
            .Include(bill => bill.Patient)
            .Include(bill => bill.Payments)
            .Where(bill => bill.DueDate < today && bill.PaymentStatus != "Paid" && bill.PaymentStatus != "Cancelled")
            .OrderBy(bill => bill.DueDate)
            .ToListAsync();

        var overdueItems = overdueBills.Select(bill => new OverdueBillReportItem
        {
            BillId = bill.BillId,
            PatientName = bill.Patient?.FullName ?? "Unknown",
            DueDate = bill.DueDate!.Value,
            Balance = Math.Max(0, bill.Amount - bill.Payments.Sum(payment => payment.Amount))
        }).Where(item => item.Balance > 0).ToList();

        var revenueToday = await _context.Payments
            .Where(payment => payment.PaymentDate >= today && payment.PaymentDate < today.AddDays(1))
            .SumAsync(payment => (decimal?)payment.Amount) ?? 0;

        var newPatientsToday = await _context.Patients.CountAsync(patient => patient.CreatedAt >= today && patient.CreatedAt < today.AddDays(1));

        return View(new ReportsViewModel
        {
            AppointmentsByDoctor = appointmentsByDoctor,
            RevenueByMonth = revenueByMonth,
            OverdueBills = overdueItems,
            RevenueToday = revenueToday,
            OverdueBalance = overdueItems.Sum(item => item.Balance),
            NewPatientsToday = newPatientsToday
        });
    }
}