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

		// GET: Payroll/AdminCalculate (Admin/HR)
		[HttpGet]
		[Authorize(Roles = "Admin,HR")]
		public IActionResult AdminCalculate()
		{
			return View(new AdminPayrollCalcViewModel
			{
				StartDate = DateTime.Today.AddDays(-13),
				EndDate = DateTime.Today,
				Rows = new List<AdminPayrollRow>()
			});
		}

		// POST: Payroll/AdminCalculate (Admin/HR)
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin,HR")]
		public async Task<IActionResult> AdminCalculate(AdminPayrollCalcViewModel vm)
		{
			if (vm.EndDate.Date < vm.StartDate.Date)
				ModelState.AddModelError("", "End date must be on/after start date.");

			if (!ModelState.IsValid)
				return View(vm);

			var resultVm = await BuildPayrollCalculationAsync(vm.StartDate, vm.EndDate);
			return View(resultVm);
		}
		// GET: Payroll/MyPayroll
		// Employee-only: shows the logged-in employee's payroll records
		[HttpGet]
		public async Task<IActionResult> MyPayroll()
		{
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

		// Maps Session Account.Id -> Account.Username -> Users.Address AutoSynced -> Users.UserId(Guid)
		private async Task<Guid> GetLoggedInEmployeeUserGuidAsync()
		{
			var accountIdStr = HttpContext.Session.GetString("UserId");
			if (!int.TryParse(accountIdStr, out var accountId))
				return Guid.Empty;

			var username = await _context.Account
				.Where(a => a.Id == accountId)
				.Select(a => a.Username)
				.FirstOrDefaultAsync();

			if (string.IsNullOrWhiteSpace(username))
				return Guid.Empty;

			var syncAddress = $"AutoSynced:{username.Trim().ToLowerInvariant()}";

			var employeeUserId = await _context.Users
				.Where(u => u.Address == syncAddress)
				.Select(u => u.UserId)
				.FirstOrDefaultAsync();

			return employeeUserId;
		}

		// GET: Payroll/MyPayrollCalculate (Employee only)
		[HttpGet]
		public async Task<IActionResult> MyPayrollCalculate()
		{
			var role = HttpContext.Session.GetString("UserRole");
			if (!string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase))
				return RedirectToAction("Login", "Account", new { role = "Employee" });

			var vm = new AdminPayrollCalcViewModel
			{
				StartDate = DateTime.Today.AddDays(-13),
				EndDate = DateTime.Today,
				Rows = new List<AdminPayrollRow>()
			};

			return View(vm);
		}

		// POST: Payroll/MyPayrollCalculate (Employee only)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> MyPayrollCalculate(AdminPayrollCalcViewModel vm)
		{
			var role = HttpContext.Session.GetString("UserRole");
			if (!string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase))
				return RedirectToAction("Login", "Account", new { role = "Employee" });

			if (vm.EndDate.Date < vm.StartDate.Date)
				ModelState.AddModelError("", "End date must be on/after start date.");

			if (!ModelState.IsValid)
				return View(vm);

			var employeeUserId = await GetLoggedInEmployeeUserGuidAsync();
			if (employeeUserId == Guid.Empty)
				return View(vm);

			var resultVm = await BuildPayrollCalculationAsync(vm.StartDate, vm.EndDate, employeeUserId);
			return View(resultVm);
		}

		// Shared payroll calculation logic used by both Admin and Employee calculators
		// If userFilterId is null => calculates for all users (Admin/HR)
		// If userFilterId has value => calculates only for that user (Employee)
		private async Task<AdminPayrollCalcViewModel> BuildPayrollCalculationAsync(
			DateTime startDate,
			DateTime endDate,
			Guid? userFilterId = null)
		{
			var vm = new AdminPayrollCalcViewModel
			{
				StartDate = startDate.Date,
				EndDate = endDate.Date
			};

			var start = DateOnly.FromDateTime(vm.StartDate);
			var end = DateOnly.FromDateTime(vm.EndDate);

			var assignmentsQuery =
				from sa in _context.ShiftAssignments
				join sh in _context.Shifts on sa.ShiftId equals sh.ShiftId
				where sa.ShiftDate >= start && sa.ShiftDate <= end
				select new
				{
					sa.UserId,
					ShiftStart = sh.StartTime,
					ShiftEnd = sh.EndTime
				};

			if (userFilterId.HasValue)
				assignmentsQuery = assignmentsQuery.Where(a => a.UserId == userFilterId.Value);

			var assignments = await assignmentsQuery.ToListAsync();

			var usersQuery = _context.Users.AsQueryable();
			if (userFilterId.HasValue)
				usersQuery = usersQuery.Where(u => u.UserId == userFilterId.Value);

			var users = await usersQuery.ToListAsync();

			decimal ShiftHours(TimeSpan startTime, TimeSpan endTime)
			{
				var dur = endTime - startTime;
				if (dur.TotalMinutes <= 0) dur = dur.Add(TimeSpan.FromHours(24)); // overnight shift
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

				// deduction logic
				decimal pensionRate = 0.05m;
				decimal taxRate = 0.20m;
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

			return vm;
		}

		private bool PayrollExists(int id)
        {
            return _context.Payrolls.Any(e => e.Id == id);
        }
    }
}