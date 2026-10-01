using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers;

[Authorize]
public class BillsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BillsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Index()
    {
        var bills = await _context.Bills.Include(b => b.Patient).Include(b => b.Items).Include(b => b.Payments)
            .OrderByDescending(b => b.BillDate).ToListAsync();

        ViewBag.TotalRevenue = await _context.Payments.SumAsync(payment => (decimal?)payment.Amount) ?? 0;
        ViewBag.PendingAmount = bills.Sum(bill => Math.Max(0, bill.Amount - bill.Payments.Sum(payment => payment.Amount)));

        return View(bills);
    }

    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> MyBills()
    {
        var userId = _userManager.GetUserId(User);
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        if (patient == null) return View(new List<Bill>());

        var bills = await _context.Bills
            .Include(b => b.Items)
            .Include(b => b.Payments)
            .Where(b => b.PatientId == patient.PatientId)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync();

        return View(bills);
    }

    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> MyPayments()
    {
        var userId = _userManager.GetUserId(User);
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
        if (patient == null) return View(new List<Payment>());

        var payments = await _context.Payments
            .Include(payment => payment.Bill)
            .Where(payment => payment.Bill!.PatientId == patient.PatientId)
            .OrderByDescending(payment => payment.PaymentDate)
            .ToListAsync();
        return View(payments);
    }

    public async Task<IActionResult> Details(int id)
    {
        var bill = await _context.Bills.Include(b => b.Patient).Include(b => b.Items)
            .Include(b => b.Payments).ThenInclude(payment => payment.RecordedBy)
            .FirstOrDefaultAsync(b => b.BillId == id);
        if (bill == null) return NotFound();

        if (User.IsInRole("Patient"))
        {
            var userId = _userManager.GetUserId(User);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == userId);
            if (patient == null || bill.PatientId != patient.Id) return Forbid();
        }

        return View(bill);
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Patients = await _context.Patients.OrderBy(p => p.FullName).ToListAsync();
        ViewBag.DueDate = DateTime.Today.AddDays(14);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Create(Bill bill)
    {
        var items = bill.Items?.Where(HasBillItemData).ToList() ?? new List<BillItem>();
        ValidateBillItems(items);
        if (items.Count == 0 && bill.Amount > 0 && !string.IsNullOrWhiteSpace(bill.Description))
            items.Add(new BillItem { Description = bill.Description.Trim(), Quantity = 1, UnitPrice = bill.Amount });
        if (items.Count == 0)
            ModelState.AddModelError(nameof(Bill.Items), "Add at least one bill item.");

        if (!ModelState.IsValid)
        {
            ViewBag.Patients = await _context.Patients.OrderBy(p => p.FullName).ToListAsync();
            ViewBag.DueDate = bill.DueDate ?? DateTime.Today.AddDays(14);
            return View(bill);
        }

        bill.Items = items;
        bill.Amount = items.Sum(item => item.Quantity * item.UnitPrice);
        bill.PaymentStatus = "Unpaid";
        bill.BillDate = DateTime.Now;
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Bill created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id)
    {
        var bill = await _context.Bills.Include(b => b.Items).Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == id);
        if (bill == null) return NotFound();
        ViewBag.Patients = await _context.Patients.OrderBy(p => p.FullName).ToListAsync();
        return View(bill);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Edit(int id, Bill bill)
    {
        if (id != bill.Id) return NotFound();

        var items = bill.Items?.Where(HasBillItemData).ToList() ?? new List<BillItem>();
        ValidateBillItems(items);
        if (items.Count == 0)
            ModelState.AddModelError(nameof(Bill.Items), "Add at least one bill item.");

        if (!ModelState.IsValid)
        {
            ViewBag.Patients = await _context.Patients.OrderBy(p => p.FullName).ToListAsync();
            return View(bill);
        }

        var existing = await _context.Bills.Include(b => b.Items).Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == id);
        if (existing == null) return NotFound();

        var newAmount = items.Sum(item => item.Quantity * item.UnitPrice);
        var paidAmount = existing.Payments.Sum(payment => payment.Amount);
        if (newAmount < paidAmount)
        {
            ModelState.AddModelError(nameof(Bill.Amount), "The bill total cannot be lower than payments already received.");
            ViewBag.Patients = await _context.Patients.OrderBy(p => p.FullName).ToListAsync();
            return View(bill);
        }

        existing.PatientId = bill.PatientId;
        existing.Description = bill.Description;
        existing.Amount = newAmount;
        existing.DueDate = bill.DueDate;
        _context.BillItems.RemoveRange(existing.Items);
        existing.Items.Clear();
        foreach (var item in items)
            existing.Items.Add(new BillItem
            {
                Description = item.Description.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        UpdatePaymentStatus(existing, paidAmount);

        await _context.SaveChangesAsync();
        TempData["Success"] = "Bill updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var bill = await _context.Bills.Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == id);
        if (bill == null) return NotFound();

        var remaining = bill.Amount - bill.Payments.Sum(payment => payment.Amount);
        if (remaining > 0)
        {
            _context.Payments.Add(new Payment
            {
                BillId = bill.BillId,
                Amount = remaining,
                Method = "Cash",
                PaymentDate = DateTime.Now,
                RecordedByUserId = _userManager.GetUserId(User)
            });
            UpdatePaymentStatus(bill, bill.Amount);
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Bill marked as paid.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> RecordPayment(int id, decimal amount, string method)
    {
        var bill = await _context.Bills.Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BillId == id);
        if (bill == null) return NotFound();

        var methods = new[] { "Cash", "Card", "Bank Transfer", "Insurance" };
        var remaining = bill.Amount - bill.Payments.Sum(payment => payment.Amount);
        if (amount <= 0 || amount > remaining || !methods.Contains(method))
        {
            TempData["Error"] = "Enter a valid payment amount and method.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _context.Payments.Add(new Payment
        {
            BillId = bill.BillId,
            Amount = amount,
            Method = method,
            PaymentDate = DateTime.Now,
            RecordedByUserId = _userManager.GetUserId(User)
        });
        UpdatePaymentStatus(bill, bill.Payments.Sum(payment => payment.Amount) + amount);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Payment recorded.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Receptionist")]
    public async Task<IActionResult> Delete(int id)
    {
        var bill = await _context.Bills.FindAsync(id);
        if (bill == null) return NotFound();

        _context.Bills.Remove(bill);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Bill deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static bool HasBillItemData(BillItem item) =>
        !string.IsNullOrWhiteSpace(item.Description) || item.Quantity != 0 || item.UnitPrice != 0;

    private void ValidateBillItems(IEnumerable<BillItem> items)
    {
        var index = 0;
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
                ModelState.AddModelError($"Items[{index}].Description", "Item description is required.");
            if (item.Quantity <= 0)
                ModelState.AddModelError($"Items[{index}].Quantity", "Quantity must be greater than zero.");
            if (item.UnitPrice < 0)
                ModelState.AddModelError($"Items[{index}].UnitPrice", "Unit price cannot be negative.");
            index++;
        }
    }

    private static void UpdatePaymentStatus(Bill bill, decimal paidAmount)
    {
        bill.PaymentStatus = paidAmount >= bill.Amount ? "Paid" : paidAmount > 0 ? "Partial" : "Unpaid";
        bill.PaidDate = bill.PaymentStatus == "Paid" ? DateTime.Now : null;
    }
}
