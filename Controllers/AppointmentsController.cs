using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers;

[Authorize]
public class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AppointmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Roles = "Admin,Receptionist,Doctor")]
    public async Task<IActionResult> Index()
    {
        var query = _context.Appointments.Include(a => a.Patient).Include(a => a.Doctor).AsQueryable();

        if (User.IsInRole("Doctor"))
        {
            var userId = _userManager.GetUserId(User);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == userId);
            if (doctor != null) query = query.Where(a => a.DoctorId == doctor.DoctorId);
        }

        return View(await query.OrderBy(a => a.AppointmentDate).ToListAsync());
    }

    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> MyAppointments()
    {
        var userId = _userManager.GetUserId(User);
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        if (patient == null) return View(new List<Appointment>());

        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == patient.PatientId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        return View(appointments);
    }

    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public async Task<IActionResult> Details(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient).Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);

        if (appointment == null) return NotFound();
        return View(appointment);
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(Appointment appointment)
    {
        if (await HasConflictAsync(appointment, null))
        {
            ModelState.AddModelError(string.Empty, "This doctor already has an appointment at that date/time.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(appointment);
        }

        appointment.Status = "Scheduled";
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (await HasConflictAsync(appointment, null))
            {
                ModelState.AddModelError(nameof(Appointment.AppointmentDate), "This doctor or patient already has an appointment that overlaps this time.");
                await transaction.RollbackAsync();
                await PopulateDropdownsAsync();
                return View(appointment);
            }

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        TempData["Success"] = "Appointment booked successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound();
        await PopulateDropdownsAsync();
        return View(appointment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id, Appointment appointment)
    {
        if (id != appointment.AppointmentId) return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(appointment);
        }

        var existing = await _context.Appointments.FindAsync(id);
        if (existing == null) return NotFound();

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await HasConflictAsync(appointment, id))
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(nameof(Appointment.AppointmentDate), "This doctor or patient already has an appointment that overlaps this time.");
            await PopulateDropdownsAsync();
            return View(appointment);
        }

        existing.PatientId = appointment.PatientId;
        existing.DoctorId = appointment.DoctorId;
        existing.AppointmentDate = appointment.AppointmentDate;
        existing.DurationMinutes = appointment.DurationMinutes;
        existing.Status = appointment.Status;
        existing.Notes = appointment.Notes;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "Appointment updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Delete(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound();

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Appointment removed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Doctor,Receptionist")]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var allowedStatuses = new[] { "Scheduled", "Waiting", "Checked-In", "Completed", "Cancelled", "No Show" };
        if (!allowedStatuses.Contains(status)) return BadRequest();

        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == id);
        if (appointment == null) return NotFound();

        if (User.IsInRole("Receptionist") && status != "Waiting" && status != "Cancelled" && status != "No Show")
            return Forbid();

        if (User.IsInRole("Doctor"))
        {
            var userId = _userManager.GetUserId(User);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == userId);
            if (doctor == null || appointment.DoctorId != doctor.DoctorId) return Forbid();
            if (status != "Checked-In" && status != "Completed") return Forbid();
        }

        appointment.Status = status;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasConflictAsync(Appointment appointment, int? excludeId)
    {
        var start = appointment.AppointmentDate;
        var end = start.AddMinutes(appointment.DurationMinutes);
        var query = _context.Appointments.Where(a =>
            a.Status != "Cancelled" &&
            a.AppointmentDate < end &&
            start < a.AppointmentDate.AddMinutes(a.DurationMinutes) &&
            (a.DoctorId == appointment.DoctorId || a.PatientId == appointment.PatientId));

        if (excludeId.HasValue) query = query.Where(a => a.AppointmentId != excludeId.Value);

        return await query.AnyAsync();
    }

    private async Task PopulateDropdownsAsync()
    {
        ViewBag.Patients = await _context.Patients.OrderBy(p => p.FullName).ToListAsync();
        ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.FullName).ToListAsync();
    }
}
