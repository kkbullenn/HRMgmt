using HRMgmt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HRMgmt.Controllers
{
    [Authorize] // Requires the user to be logged in to access anything here
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

            var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountIdString) || !int.TryParse(accountIdString, out var accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Account.FindAsync(accountId);
            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.Equals(account.Role, "Employee", StringComparison.OrdinalIgnoreCase))
            {
                // var syncAddress = BuildAutoSyncedAddress(account.Username);
                // var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Address == syncAddress);
                var (fName, lName) = BuildName(account.DisplayName, account.Username);
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => 
                    u.FirstName == fName && u.LastName == lName);
                
                if (currentUser == null)
                {
                    return NotFound();
                }

                return View("Details", currentUser);
            }

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
            
            // only Admin can see user roles
            var role = await _context.Roles.FindAsync(user.RoleId);
            ViewBag.RoleName = role?.RoleName ?? "N/A";

            return View(user);
        }

        //GET:Users/Profile
        public async Task<IActionResult> Profile()
        {
            var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountIdString) || !int.TryParse(accountIdString, out var accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Account.FindAsync(accountId);
            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // var syncAddress = BuildAutoSyncedAddress(account.Username);
            // var user = await _context.Users.FirstOrDefaultAsync(u => u.Address == syncAddress);
            var (fName, lName) = BuildName(account.DisplayName, account.Username);
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.FirstName == fName && u.LastName == lName);

            if (user == null)
            {
                return NotFound();
            }

            return View("Details", user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            var roles = _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleName)
                .ToList();

            ViewBag.Role = new SelectList(roles, "Id", "RoleName");
            ViewData["EnteredUsername"] = string.Empty;
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // FIXED: Added string username and string password to parameters
        public async Task<IActionResult> Create(
            [Bind("UserId,FirstName,LastName,DateOfBirth,Address,RoleId,Photo,HourlyWage")] User user, string username,
            string password, IFormFile? photoFile)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Username and password are required to create a new user account.");
            }


            if (ModelState.IsValid)
            {
                var roleEntity = await _context.Roles.FindAsync(user.RoleId);
                if (roleEntity == null)
                {
                    ModelState.AddModelError("RoleId", "Invalid role selection.");
                }
                else if (await _context.Account.AnyAsync(a => a.Username == username))
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                }
                else
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        if (photoFile != null && photoFile.Length > 0)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                await photoFile.CopyToAsync(memoryStream);
                                user.Photo = memoryStream.ToArray();
                            }
                        }
                        
                        var displayName = string.Join(" ", new[] { user.FirstName, user.LastName }
                            .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

                        // Moved above user creation to stop dupe creation
                        var account = new Account
                        {
                            Username = username,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                            Role = roleEntity.RoleName,
                            DisplayName = displayName,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Account.Add(account);
                        await _context.SaveChangesAsync();

                        user.UserId = Guid.NewGuid();
                        // Commented out to stop AutoSynced address issue
                        // if (string.Equals(roleEntity.RoleName, "Employee", StringComparison.OrdinalIgnoreCase))
                        // {
                        //     user.Address = BuildAutoSyncedAddress(username);
                        // }

                        _context.Add(user);

                        if (string.IsNullOrWhiteSpace(displayName))
                        {
                            displayName = username;
                        }

                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }

            // Repopulate dropdown if form fails
            var roles = await _context.Roles.AsNoTracking().OrderBy(r => r.RoleName).ToListAsync();
            ViewBag.Role = new SelectList(roles, "Id", "RoleName", user.RoleId);
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountIdString) || !int.TryParse(accountIdString, out var accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Account.FindAsync(accountId);
            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (string.Equals(account.Role, "Employee", StringComparison.OrdinalIgnoreCase))
            {
                // var syncAddress = BuildAutoSyncedAddress(account.Username);
                // var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Address == syncAddress);
                var (fName, lName) = BuildName(account.DisplayName, account.Username);
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => 
                    u.FirstName == fName && u.LastName == lName);

                if (currentUser == null || currentUser.UserId != id)
                {
                    return Forbid();
                }

                return View(user);
            }
            
            var roles = _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleName)
                .ToList();

            ViewBag.Role = new SelectList(roles, "Id", "RoleName");

            return View("AdminEdit", user);
        }

        // GET: Users/AdminEdit/5
        public async Task<IActionResult> adminEdit(Guid? id)
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
            
            var roles = _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleName)
                .ToList();

            ViewBag.Role = new SelectList(roles, "Id", "RoleName");

            return View("AdminEdit", user);
        }


        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id,
            [Bind("UserId,FirstName,LastName,DateOfBirth,Address,RoleId,Photo,HourlyWage")] User user, IFormFile? photoFile)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(accountIdString) || !int.TryParse(accountIdString, out var accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var account = await _context.Account.FindAsync(accountId);


            if (string.Equals(account?.Role, "Employee", StringComparison.OrdinalIgnoreCase))
            {
                // var syncAddress = BuildAutoSyncedAddress(account.Username);
                // var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Address == syncAddress);
                var (fName, lName) = BuildName(account.DisplayName, account.Username);
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => 
                    u.FirstName == fName && u.LastName == lName);

                if (currentUser == null || currentUser.UserId != id)
                {
                    return Forbid();
                }
            }

            if (account == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    existingUser.FirstName = user.FirstName;
                    existingUser.LastName = user.LastName;
                    existingUser.DateOfBirth = user.DateOfBirth;
                    existingUser.Address = user.Address;
                    existingUser.RoleId = user.RoleId;
                    existingUser.HourlyWage = user.HourlyWage;

                    if (photoFile != null && photoFile.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await photoFile.CopyToAsync(memoryStream);
                            existingUser.Photo = memoryStream.ToArray();
                        }
                    }

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
                //var syncAddress = BuildAutoSyncedAddress(account.Username); causing issue showing address

                var exists = await _context.Users.AnyAsync(u =>
                    u.FirstName == firstName &&
                    u.LastName == lastName &&
                    u.RoleId == employeeRoleId);

                if (exists)
                {
                    continue;
                }

                _context.Users.Add(new User
                {
                    UserId = Guid.NewGuid(),
                    FirstName = firstName,
                    LastName = lastName,
                    Address = "No address",
                    RoleId = employeeRoleId,
                    HourlyWage = 0m
                });
                hasNewUsers = true;
            }

            if (hasNewUsers)
            {
                await _context.SaveChangesAsync();
            }
        }


        // FIXED: Added null coalescing '?? string.Empty' to avoid null reference exceptions
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