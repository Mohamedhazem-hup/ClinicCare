using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers;

[Authorize(Roles = "Admin,Receptionist,Doctor")]
public class PatientsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Patients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.FullName.Contains(search) || (p.Phone ?? "").Contains(search) || (p.Email ?? "").Contains(search));
        ViewBag.Search = search;
        return View(await query.OrderBy(p => p.FullName).ToListAsync());
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.Email))
            ModelState.AddModelError(nameof(Patient.Email), "Email is required so the patient can log in to their portal.");
        else if (await _userManager.FindByEmailAsync(patient.Email) != null)
            ModelState.AddModelError(nameof(Patient.Email), "An account with this email already exists.");

        if (!ModelState.IsValid)
            return View(patient);

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var temporaryPassword = TemporaryPasswordGenerator.Generate();
        var user = new ApplicationUser
        {
            UserName = patient.Email,
            Email = patient.Email,
            FullName = patient.FullName,
            EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(user, temporaryPassword);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Patient");
            patient.ApplicationUserId = user.Id;
            await _context.SaveChangesAsync();
            TempData["AccountNotice"] = $"Patient portal account created for {patient.Email}. Temporary password: {temporaryPassword} (share this with the patient and ask them to change it after first login).";
        }
        else
        {
            TempData["AccountNotice"] = "The patient record was saved, but the login account could not be created: " + string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var patient = await _context.Patients
            .Include(p => p.Appointments).ThenInclude(a => a.Doctor)
            .Include(p => p.MedicalRecords).ThenInclude(m => m.Doctor)
            .Include(p => p.Prescriptions).ThenInclude(p => p.Doctor)
            .Include(p => p.Prescriptions).ThenInclude(p => p.Items)
            .Include(p => p.Bills)
            .FirstOrDefaultAsync(p => p.PatientId == id);

        if (patient == null)
            return NotFound();

        return View(patient);
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == id);
        if (patient == null)
            return NotFound();

        return View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id, Patient patient)
    {
        if (id != patient.PatientId)
            return NotFound();

        if (!ModelState.IsValid)
            return View(patient);

        var existing = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == id);
        if (existing == null)
            return NotFound();

        existing.FullName = patient.FullName;
        existing.DateOfBirth = patient.DateOfBirth;
        existing.Gender = patient.Gender;
        existing.Phone = patient.Phone;
        existing.Address = patient.Address;
        existing.BloodType = patient.BloodType;
        existing.Email = patient.Email;
        existing.EmergencyContact = patient.EmergencyContact;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Delete(int id)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == id);
        if (patient == null) return NotFound();

        _context.Patients.Remove(patient);
        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Patient deleted.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Cannot delete: patient has linked appointments, records, or bills.";
        }

        return RedirectToAction(nameof(Index));
    }
}
