using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagementSystem.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
            if (User.IsInRole("Doctor")) return RedirectToAction("Dashboard", "Doctors");
            if (User.IsInRole("Receptionist")) return RedirectToAction("Index", "Patients");
            if (User.IsInRole("Patient")) return RedirectToAction("MyAppointments", "Appointments");
        }

        return View();
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    public IActionResult Error() => Content("An error occurred.");
}
