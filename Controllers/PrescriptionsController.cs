using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers;

[Authorize]
public class PrescriptionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PrescriptionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Index()
    {
        var query = _context.Prescriptions.Include(p => p.Patient).Include(p => p.Doctor).AsQueryable();

        if (User.IsInRole("Doctor"))
        {
            var userId = _userManager.GetUserId(User);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == userId);
            if (doctor == null) return View(new List<Prescription>());
            query = query.Where(p => p.DoctorId == doctor.DoctorId);
        }

        return View(await query.Include(p => p.Items).OrderByDescending(p => p.Date).ToListAsync());
    }

    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> MyPrescriptions()
    {
        var userId = _userManager.GetUserId(User);
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        if (patient == null) return View(new List<Prescription>());

        var prescriptions = await _context.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Items)
            .Where(p => p.PatientId == patient.PatientId)
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        return View(prescriptions);
    }

    public async Task<IActionResult> Details(int id)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Patient).Include(p => p.Doctor).Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);

        if (prescription == null) return NotFound();

        if (User.IsInRole("Patient"))
        {
            var userId = _userManager.GetUserId(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            if (patient == null || prescription.PatientId != patient.Id) return Forbid();
        }

        return View(prescription);
    }

    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create(Prescription prescription)
    {
        if (User.IsInRole("Doctor"))
        {
            var userId = _userManager.GetUserId(User);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == userId);
            if (doctor == null) return Forbid();
            prescription.DoctorId = doctor.DoctorId;
        }

        var items = prescription.Items?.Where(HasMedicineData).ToList() ?? new List<PrescriptionItem>();
        ValidateItems(items);
        if (items.Count == 0)
            ModelState.AddModelError(nameof(Prescription.Items), "Add at least one medicine.");

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(prescription);
        }

        prescription.Items = items;
        prescription.MedicineName = string.Join(", ", items.Select(item => item.MedicineName.Trim()));
        prescription.Date = DateTime.Now;
        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Prescription issued successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Edit(int id)
    {
        var prescription = await _context.Prescriptions.Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);
        if (prescription == null) return NotFound();
        await PopulateDropdownsAsync();
        return View(prescription);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Edit(int id, Prescription prescription)
    {
        if (id != prescription.Id) return NotFound();

        var items = prescription.Items?.Where(HasMedicineData).ToList() ?? new List<PrescriptionItem>();
        ValidateItems(items);
        if (items.Count == 0)
            ModelState.AddModelError(nameof(Prescription.Items), "Add at least one medicine.");

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(prescription);
        }

        var existing = await _context.Prescriptions.Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);
        if (existing == null) return NotFound();

        existing.PatientId = prescription.PatientId;
        existing.DoctorId = prescription.DoctorId;
        existing.DateIssued = prescription.DateIssued;
        _context.PrescriptionItems.RemoveRange(existing.Items);
        existing.Items.Clear();
        foreach (var item in items)
        {
            existing.Items.Add(new PrescriptionItem
            {
                MedicineName = item.MedicineName.Trim(),
                Dosage = item.Dosage.Trim(),
                Frequency = item.Frequency.Trim(),
                Duration = item.Duration.Trim(),
                Instructions = item.Instructions?.Trim()
            });
        }
        existing.MedicineName = string.Join(", ", items.Select(item => item.MedicineName.Trim()));
        existing.Notes = prescription.Notes;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Prescription updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Delete(int id)
    {
        var prescription = await _context.Prescriptions.FindAsync(id);
        if (prescription == null) return NotFound();

        _context.Prescriptions.Remove(prescription);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Prescription deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync()
    {
        ViewBag.Patients = await _context.Patients.OrderBy(p => p.FullName).ToListAsync();
        ViewBag.Doctors = await _context.Doctors.OrderBy(d => d.FullName).ToListAsync();
    }

    private static bool HasMedicineData(PrescriptionItem item) =>
        !string.IsNullOrWhiteSpace(item.MedicineName) ||
        !string.IsNullOrWhiteSpace(item.Dosage) ||
        !string.IsNullOrWhiteSpace(item.Frequency) ||
        !string.IsNullOrWhiteSpace(item.Duration) ||
        !string.IsNullOrWhiteSpace(item.Instructions);

    private void ValidateItems(IReadOnlyList<PrescriptionItem> items)
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(items[index].MedicineName))
                ModelState.AddModelError($"Items[{index}].MedicineName", "Medicine name is required.");
            if (string.IsNullOrWhiteSpace(items[index].Dosage))
                ModelState.AddModelError($"Items[{index}].Dosage", "Dosage is required.");
            if (string.IsNullOrWhiteSpace(items[index].Frequency))
                ModelState.AddModelError($"Items[{index}].Frequency", "Frequency is required.");
            if (string.IsNullOrWhiteSpace(items[index].Duration))
                ModelState.AddModelError($"Items[{index}].Duration", "Duration is required.");
        }
    }
}
