using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMgmt.Models;
using HRMgmt.ViewModels;
using Microsoft.AspNetCore.Authorization; // ADDED

namespace HRMgmt.Controllers
{
    [Authorize] // ADDED: Forces user to be logged in
    public class PayrollController : Controller
    {
        private readonly OrgDbContext _context;

        public PayrollController(OrgDbContext context)
        {
            _context = context;
        }

        // GET: Payroll
        [Authorize(Roles = "Admin,HR")] // ADDED: Replaces manual HasPayrollAccess check
        public async Task<IActionResult> Index()
        {
            return View(await _context.Payrolls.ToListAsync());
        }

        // GET: Payroll/Details/5
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payroll = await _context.Payrolls.FirstOrDefaultAsync(m => m.Id == id);
            if (payroll == null) return NotFound();

            return View(payroll);
        }

        // GET: Payroll/Create
        [Authorize(Roles = "Admin,HR")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create([Bind("Id,UserId,PayPeriodStart,PayPeriodEnd,RegularHours,OvertimeHours,SickHours,TotalHours,GrossPay,Status,CreatedDate")] Payroll payroll)
        {
            if (ModelState.IsValid)
            {
                _context.Add(payroll);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(payroll);
        }

        // GET: Payroll/Edit/5
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var payroll = await _context.Payrolls.FindAsync(id);
            if (payroll == null) return NotFound();

            return View(payroll);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,PayPeriodStart,PayPeriodEnd,RegularHours,OvertimeHours,SickHours,TotalHours,GrossPay,Status,CreatedDate")] Payroll payroll)
        {
            if (id != payroll.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payroll);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PayrollExists(payroll.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(payroll);
        }

        // GET: Payroll/Delete/5
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var payroll = await _context.Payrolls.FirstOrDefaultAsync(m => m.Id == id);
            if (payroll == null) return NotFound();

            return View(payroll);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payroll = await _context.Payrolls.FindAsync(id);
            if (payroll != null)
            {
                _context.Payrolls.Remove(payroll);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

		// GET: Payroll/MyPayroll
		// Employee-only: shows the logged-in employee's own payroll records
		[HttpGet]
		public async Task<IActionResult> MyPayroll()
		{
			// Only employees should view this page
			var role = HttpContext.Session.GetString("UserRole");
			if (!string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase))
				return RedirectToAction("Login", "Account", new { role = "Employee" });

			// Session UserId is Account.Id (int)
			var accountIdStr = HttpContext.Session.GetString("UserId");
			if (!int.TryParse(accountIdStr, out var accountId))
				return RedirectToAction("Login", "Account", new { role = "Employee" });

			// Get username from Account table
			var username = await _context.Account
				.Where(a => a.Id == accountId)
				.Select(a => a.Username)
				.FirstOrDefaultAsync();

			if (string.IsNullOrWhiteSpace(username))
				return View(new List<Payroll>());

			// Match how employee profiles are stored in Users table
			var syncAddress = $"AutoSynced:{username.Trim().ToLowerInvariant()}";

			// Get the real employee Guid from Users table
			var employeeUserId = await _context.Users
				.Where(u => u.Address == syncAddress)
				.Select(u => u.UserId)
				.FirstOrDefaultAsync();

			if (employeeUserId == Guid.Empty)
				return View(new List<Payroll>());

			// Pull only THIS employee's payroll rows
			var myPayrolls = await _context.Payrolls
				.Where(p => p.UserId == employeeUserId)
				.OrderByDescending(p => p.PayPeriodEnd)
				.ToListAsync();

			return View(myPayrolls);
		}

		// GET: Payroll/AdminCalculate
		[HttpGet]
		public IActionResult AdminCalculate()
		{
			if (!HasPayrollAccess())
				return RedirectToAction("Login", "Account", new { role = "HR" });

			var vm = new AdminPayrollCalcViewModel
			{
				StartDate = DateTime.Today.AddDays(-13),
				EndDate = DateTime.Today
			};

			return View(vm);
		}

		// POST: Payroll/AdminCalculate
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AdminCalculate(AdminPayrollCalcViewModel vm)
		{
			if (!HasPayrollAccess())
				return RedirectToAction("Login", "Account", new { role = "HR" });

			if (vm.EndDate.Date < vm.StartDate.Date)
				ModelState.AddModelError("", "End date must be on/after start date.");

			if (!ModelState.IsValid)
				return View(vm);

			var start = DateOnly.FromDateTime(vm.StartDate.Date);
			var end = DateOnly.FromDateTime(vm.EndDate.Date);

			var assignments = await (
				from sa in _context.ShiftAssignments
				join sh in _context.Shifts on sa.ShiftId equals sh.ShiftId
				where sa.ShiftDate >= start && sa.ShiftDate <= end
				select new
				{
					sa.UserId,
					ShiftStart = sh.StartTime,
					ShiftEnd = sh.EndTime
				}
			).ToListAsync();

			var users = await _context.Users.ToListAsync();

			decimal ShiftHours(TimeSpan startTime, TimeSpan endTime)
			{
				var dur = endTime - startTime;
				if (dur.TotalMinutes <= 0) dur = dur.Add(TimeSpan.FromHours(24));
				return (decimal)dur.TotalHours;
			}

			var rows = new List<AdminPayrollRow>();

			foreach (var u in users)
			{
				var wage = u.HourlyWage ?? 0m;
				if (wage <= 0m) continue;

				var userAssignments = assignments.Where(a => a.UserId == u.UserId).ToList();
				if (userAssignments.Count == 0) continue;

				var totalHours = userAssignments.Sum(a => ShiftHours(a.ShiftStart, a.ShiftEnd));
				var gross = totalHours * wage;
				// Simple fixed deduction rates (admin payroll calculation)
				decimal pensionRate = 0.05m;   // 5%
				decimal taxRate = 0.20m;       // 20%
				var pension = gross * pensionRate;
				var tax = gross * taxRate;
				var net = gross - pension - tax;

				rows.Add(new AdminPayrollRow
				{
					UserId = u.UserId,
					EmployeeName = $"{u.FirstName} {u.LastName}",
					HourlyWage = wage,
					ShiftCount = userAssignments.Count,
					TotalHours = Math.Round(totalHours, 2),
					GrossPay = Math.Round(gross, 2),
					PensionDeduction = Math.Round(pension, 2),
					TaxDeduction = Math.Round(tax, 2),
					NetPay = Math.Round(net, 2)
				});
			}

			vm.Rows = rows.OrderBy(r => r.EmployeeName).ToList();
			vm.TotalHoursAllEmployees = Math.Round(vm.Rows.Sum(r => r.TotalHours), 2);
			vm.TotalGrossAllEmployees = Math.Round(vm.Rows.Sum(r => r.GrossPay), 2);
			vm.TotalPensionAllEmployees = Math.Round(vm.Rows.Sum(r => r.PensionDeduction), 2);
			vm.TotalTaxAllEmployees = Math.Round(vm.Rows.Sum(r => r.TaxDeduction), 2);
			vm.TotalNetAllEmployees = Math.Round(vm.Rows.Sum(r => r.NetPay), 2);


			return View(vm);
		}


		private bool PayrollExists(int id)
        {
            return _context.Payrolls.Any(e => e.Id == id);
        }
    }
}