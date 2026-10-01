using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers;

[Authorize(Roles = "Admin,Receptionist,Doctor")]
public class DoctorsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DoctorsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Doctors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.FullName.Contains(search) || d.Specialty.Contains(search));

        ViewBag.Search = search;
        return View(await query.OrderByDescending(d => d.CreatedAt).ToListAsync());
    }

    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Dashboard()
    {
        var userId = _userManager.GetUserId(User);
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.ApplicationUserId == userId);
        if (doctor == null) return Forbid();

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == doctor.DoctorId && a.AppointmentDate >= today && a.AppointmentDate < tomorrow)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync();

        ViewBag.DoctorName = doctor.FullName;
        ViewBag.Today = today;
        ViewBag.TotalToday = appointments.Count;
        ViewBag.WaitingCount = appointments.Count(a => a.Status == "Waiting");
        ViewBag.CompletedCount = appointments.Count(a => a.Status == "Completed");
        return View(appointments);
    }

    public async Task<IActionResult> Details(int id)
    {
        var doctor = await _context.Doctors
            .Include(d => d.Appointments).ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(d => d.DoctorId == id);

        if (doctor == null) return NotFound();
        return View(doctor);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View();

    // Creates a login account + doctor profile together
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Doctor doctor)
    {
        if (string.IsNullOrWhiteSpace(doctor.Email))
            ModelState.AddModelError(nameof(Doctor.Email), "Email is required so the doctor can log in to the system.");
        else if (await _userManager.FindByEmailAsync(doctor.Email) != null)
            ModelState.AddModelError(nameof(Doctor.Email), "An account with this email already exists.");

        if (!ModelState.IsValid) return View(doctor);

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();

        var temporaryPassword = TemporaryPasswordGenerator.Generate();
        var user = new ApplicationUser
        {
            UserName = doctor.Email,
            Email = doctor.Email,
            FullName = doctor.FullName,
            EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(user, temporaryPassword);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Doctor");
            doctor.ApplicationUserId = user.Id;
            await _context.SaveChangesAsync();
            TempData["AccountNotice"] = $"Doctor login account created for {doctor.Email}. Temporary password: {temporaryPassword} (share this with the doctor and ask them to change it after first login).";
        }
        else
        {
            TempData["AccountNotice"] = "The doctor record was saved, but the login account could not be created: " + string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor == null) return NotFound();
        return View(doctor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, Doctor doctor)
    {
        if (id != doctor.Id) return NotFound();

        var existing = await _context.Doctors.FindAsync(id);
        if (existing == null) return NotFound();

        existing.FullName = doctor.FullName;
        existing.Specialty = doctor.Specialty;
        existing.Phone = doctor.Phone;
        existing.Email = doctor.Email;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Doctor updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor == null) return NotFound();

        _context.Doctors.Remove(doctor);
        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Doctor deleted.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Cannot delete: doctor has linked appointments/records.";
        }

        return RedirectToAction(nameof(Index));
    }
}
