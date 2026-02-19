using HRMgmt.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HRMgmt.Controllers
{
    public class AccountController : Controller
    {
        private readonly OrgDbContext _context;

        public AccountController(OrgDbContext context)
        {
            _context = context;
        }

        // =========================
        // AUTH: LOGIN / SIGNUP / LOGOUT
        // =========================

        // GET: Account/Login?role=Employee
        [HttpGet]
        public IActionResult Login(string role)
        {
            ViewBag.Role = role;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Role = role;
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            var account = await _context.Account
                .FirstOrDefaultAsync(a => a.Username == username && a.Role == role);

            if (account == null || !BCrypt.Net.BCrypt.Verify(password, account.PasswordHash))
            {
                ViewBag.Role = role;
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            HttpContext.Session.SetString("UserRole", account.Role);
            HttpContext.Session.SetString("UserName", account.DisplayName ?? account.Username);
            HttpContext.Session.SetString("UserId", account.Id.ToString());

            if (string.Equals(account.Role, "Employee", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("MyShifts", "Shift");
            }

            if (string.Equals(account.Role, "HR", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.Equals(account.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Users");
        }

        // GET: Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        // =========================
        // CRUD: SCAFFOLDED ACTIONS
        // =========================

        // GET: Account
        public async Task<IActionResult> Index()
        {
            return View(await _context.Account.ToListAsync());
        }

        // GET: Account/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var account = await _context.Account.FirstOrDefaultAsync(m => m.Id == id);
            if (account == null) return NotFound();

            return View(account);
        }

        // GET: Account/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncEmployeeUsers()
        {
            var employeeAccounts = await _context.Account
                .Where(a => a.Role.ToLower() == "employee")
                .ToListAsync();

            var createdCount = 0;
            foreach (var account in employeeAccounts)
            {
                var result = await EnsureUserProfileForAccountAsync(
                    account.Username,
                    account.DisplayName ?? account.Username,
                    account.Role);

                if (!result.Success)
                {
                    TempData["Error"] = result.ErrorMessage ?? "Sync failed.";
                    return RedirectToAction(nameof(Create));
                }

                if (result.CreatedUser)
                {
                    createdCount++;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Synced employee profiles. Created {createdCount} user record(s).";
            return RedirectToAction(nameof(Create));
        }

        // POST: Account/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string password, string role, string displayName)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                ViewBag.Error = "Username, password, and role are required.";
                return View();
            }

            if (password.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters.";
                return View();
            }

            var exists = await _context.Account.AnyAsync(a => a.Username == username);
            if (exists)
            {
                ViewBag.Error = "Username already exists.";
                return View();
            }

            var syncResult = await EnsureUserProfileForAccountAsync(username, displayName, role);
            if (!syncResult.Success)
            {
                ViewBag.Error = syncResult.ErrorMessage;
                return View();
            }

            var account = new Account
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? username : displayName,
                CreatedAt = DateTime.UtcNow
            };

            _context.Account.Add(account);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Account '{username}' created successfully.";
            return RedirectToAction(nameof(Create));
        }

        // GET: Account/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var account = await _context.Account.FindAsync(id);
            if (account == null) return NotFound();

            return View(account);
        }

        // POST: Account/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,PasswordHash,Role,DisplayName,CreatedAt")] Account account)
        {
            if (id != account.Id) return NotFound();

            if (!ModelState.IsValid) return View(account);

            try
            {
                _context.Update(account);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AccountExists(account.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Account/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var account = await _context.Account.FirstOrDefaultAsync(m => m.Id == id);
            if (account == null) return NotFound();

            return View(account);
        }

        // POST: Account/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var account = await _context.Account.FindAsync(id);
            if (account != null)
            {
                _context.Account.Remove(account);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AccountExists(int id)
        {
            return _context.Account.Any(e => e.Id == id);
        }

        private async Task<(bool Success, bool CreatedUser, string? ErrorMessage)> EnsureUserProfileForAccountAsync(
            string username,
            string displayName,
            string role)
        {
            // Keep change small: only ensure Employee accounts are materialized in Users table,
            // because ShiftAssignment depends on Users.
            if (!string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase))
            {
                return (true, false, null);
            }

            var employeeRoleId = await _context.Roles
                .Where(r => r.RoleName.ToLower() == "employee")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (employeeRoleId == 0)
            {
                var roleEntity = new Role { RoleName = "Employee" };
                _context.Roles.Add(roleEntity);
                await _context.SaveChangesAsync();
                employeeRoleId = roleEntity.Id;
            }

            var (firstName, lastName) = BuildName(displayName, username);
            var syncAddress = BuildAutoSyncedAddress(username);

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Address == syncAddress &&
                    u.Role == employeeRoleId);

            if (existingUser != null)
            {
                return (true, false, null);
            }

            _context.Users.Add(new User
            {
                UserId = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Address = syncAddress,
                Role = employeeRoleId,
                HourlyWage = 0m
            });

            return (true, true, null);
        }

        private static (string FirstName, string LastName) BuildName(string displayName, string username)
        {
            var source = string.IsNullOrWhiteSpace(displayName) ? username : displayName;
            var cleaned = Regex.Replace(source, @"[^a-zA-Z\s'\-]", " ").Trim();
            var parts = cleaned
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            return parts.Count switch
            {
                0 => ("Employee", "User"),
                1 => (parts[0], "User"),
                _ => (parts[0], string.Join(" ", parts.Skip(1)))
            };
        }

        private static string BuildAutoSyncedAddress(string username)
        {
            return $"AutoSynced:{username.Trim().ToLowerInvariant()}";
        }
    }
}
