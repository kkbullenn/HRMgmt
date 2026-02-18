using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HRMgmt.Models;
using System.Text.RegularExpressions;

namespace HRMgmt.Controllers
{
    public class UsersController : Controller
    {
        private readonly OrgDbContext _context;

        public UsersController(OrgDbContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            await EnsureEmployeeUsersSyncedAsync();
            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        //GET:Users/Profile
        public async Task<IActionResult> Profile()
        {
            var accountIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(accountIdString) || !int.TryParse(accountIdString, out var accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Account.FindAsync(accountId);
            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var syncAddress = account.Username;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Address == account.Username);
            
            if (user == null)
            {
                return NotFound();
            }

            return View("Details", user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,FirstName,LastName,DateOfBirth,Address,Role,Photo,HourlyWage")] User user)
        {
            if (ModelState.IsValid)
            {
                user.UserId = Guid.NewGuid();
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("UserId,FirstName,LastName,DateOfBirth,Address,Role,Photo,HourlyWage")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(Guid id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        private async Task EnsureEmployeeUsersSyncedAsync()
        {
            var employeeRoleId = await _context.Roles
                .Where(r => r.RoleName.ToLower() == "employee")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (employeeRoleId == 0)
            {
                var role = new Role { RoleName = "Employee" };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
                employeeRoleId = role.Id;
            }

            var employeeAccounts = await _context.Account
                .Where(a => a.Role.ToLower() == "employee")
                .ToListAsync();

            var hasNewUsers = false;
            foreach (var account in employeeAccounts)
            {
                var (firstName, lastName) = BuildName(account.DisplayName, account.Username);
                var syncAddress = BuildAutoSyncedAddress(account.Username);

                var exists = await _context.Users.AnyAsync(u =>
                    u.Address == syncAddress &&
                    u.Role == employeeRoleId);

                if (exists)
                {
                    continue;
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
                hasNewUsers = true;
            }

            if (hasNewUsers)
            {
                await _context.SaveChangesAsync();
            }
        }

        private static (string FirstName, string LastName) BuildName(string? displayName, string username)
        {
            var source = string.IsNullOrWhiteSpace(displayName) ? username : displayName;
            var cleaned = Regex.Replace(source ?? string.Empty, @"[^a-zA-Z\s'\-]", " ").Trim();
            var parts = cleaned
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (parts.Count == 0)
            {
                return ("Employee", "User");
            }

            if (parts.Count == 1)
            {
                return (parts[0], "User");
            }

            return (parts[0], string.Join(" ", parts.Skip(1)));
        }

        private static string BuildAutoSyncedAddress(string username)
        {
            return $"AutoSynced:{(username ?? string.Empty).Trim().ToLowerInvariant()}";
        }
    }
}
