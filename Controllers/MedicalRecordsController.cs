using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers;

[Authorize]
public class MedicalRecordsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MedicalRecordsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Index()
    {
        var query = _context.MedicalRecords.Include(m => m.Patient).Include(m => m.Doctor).AsQueryable();

        if (User.IsInRole("Doctor"))
        {
            var userId = _userManager.GetUserId(User);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == userId);
            if (doctor == null) return View(new List<MedicalRecord>());
            query = query.Where(m => m.DoctorId == doctor.DoctorId);
        }

        return View(await query.OrderByDescending(m => m.VisitDate).ToListAsync());
    }

    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> MyRecords()
    {
        var userId = _userManager.GetUserId(User);
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        if (patient == null) return View(new List<MedicalRecord>());

        var records = await _context.MedicalRecords
            .Include(m => m.Doctor)
            .Include(m => m.Prescriptions).ThenInclude(p => p.Items)
            .Where(m => m.PatientId == patient.PatientId)
            .OrderByDescending(m => m.VisitDate)
            .ToListAsync();

        return View(records);
    }

    [Authorize(Roles = "Patient")]
    public IActionResult MyMedicalHistory() => RedirectToAction(nameof(MyRecords));

    public async Task<IActionResult> Details(int id)
    {
        var record = await _context.MedicalRecords
            .Include(m => m.Patient).Include(m => m.Doctor)
            .Include(m => m.Prescriptions).ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(m => m.MedicalRecordId == id);

        if (record == null) return NotFound();

        if (User.IsInRole("Patient"))
        {
            var userId = _userManager.GetUserId(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            if (patient == null || record.PatientId != patient.Id) return Forbid();
        }

        return View(record);
    }

    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create(int? patientId, int? appointmentId)
    {
        await PopulateDropdownsAsync();
        var record = new MedicalRecord
        {
            PatientId = patientId ?? 0,
            AppointmentId = appointmentId
        };

        if (User.IsInRole("Doctor"))
        {
            var userId = _userManager.GetUserId(User);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == userId);
            if (doctor == null) return Forbid();
            record.DoctorId = doctor.DoctorId;
        }

        return View(record);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create(MedicalRecord record)
    {
        Appointment? appointment = null;
        if (User.IsInRole("Doctor"))
        {
            var userId = _userManager.GetUserId(User);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == userId);
            if (doctor == null) return Forbid();
            record.DoctorId = doctor.DoctorId;
        }

        if (record.AppointmentId.HasValue)
        {
            appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == record.AppointmentId.Value);
            if (appointment == null)
                ModelState.AddModelError(nameof(record.AppointmentId), "The linked appointment was not found.");
            else if (appointment.Status != "Checked-In")
                ModelState.AddModelError(nameof(record.AppointmentId), "The appointment must be checked in before recording an examination.");
            else if (appointment.DoctorId != record.DoctorId && !User.IsInRole("Admin"))
                return Forbid();
            else
            {
                record.PatientId = appointment.PatientId;
                record.DoctorId = appointment.DoctorId;
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(record);
        }

        _context.MedicalRecords.Add(record);
        if (appointment != null)
            appointment.Status = "Completed";
        await _context.SaveChangesAsync();
        TempData["Success"] = "Medical record added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Edit(int id)
    {
        var record = await _context.MedicalRecords.FindAsync(id);
        if (record == null) return NotFound();
        await PopulateDropdownsAsync();
        return View(record);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Edit(int id, MedicalRecord record)
    {
        if (id != record.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(record);
        }

        var existing = await _context.MedicalRecords.FindAsync(id);
        if (existing == null) return NotFound();

        existing.PatientId = record.PatientId;
        existing.DoctorId = record.DoctorId;
        existing.VisitDate = record.VisitDate;
        existing.Diagnosis = record.Diagnosis;
        existing.ChiefComplaint = record.ChiefComplaint;
        existing.Symptoms = record.Symptoms;
        existing.BloodPressure = record.BloodPressure;
        existing.Temperature = record.Temperature;
        existing.Weight = record.Weight;
        existing.Treatment = record.Treatment;
        existing.Notes = record.Notes;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Medical record updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _context.MedicalRecords.FindAsync(id);
        if (record == null) return NotFound();

        _context.MedicalRecords.Remove(record);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Medical record deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync()
    {
        ViewBag.Patients = await _context.Patients.OrderBy(p => p.FullName).ToListAsync();
        ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.FullName).ToListAsync();
    }
}
