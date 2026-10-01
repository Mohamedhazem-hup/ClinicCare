using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Patients = await _context.Patients.CountAsync();
        ViewBag.Doctors = await _context.Doctors.CountAsync();
        ViewBag.Appointments = await _context.Appointments.CountAsync();
        ViewBag.TotalRevenue = (await _context.Payments
            .SumAsync(payment => (decimal?)payment.Amount)) ?? 0;

        return View();
    }

    // Simple staff management: create Receptionist accounts
    [HttpGet]
    public IActionResult CreateReceptionist() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReceptionist(string fullName, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(string.Empty, "All fields are required.");
            return View();
        }

        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View();
        }

        await _userManager.AddToRoleAsync(user, "Receptionist");
        TempData["Success"] = "Receptionist account created successfully.";
        return RedirectToAction(nameof(Index));
    }
}
