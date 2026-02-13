using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMgmt.Models;

namespace HRMgmt.Controllers
{
    public class AccountController(OrgDbContext context) : Controller
    {
        private readonly OrgDbContext _context = context;

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
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Role = role;
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            var account = await _context.Accounts
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

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Signup
        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        // POST: Account/Signup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(string username, string password, string confirmPassword, string role, string displayName)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            {
                ViewBag.Error = "All fields are required.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            if (password.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters.";
                return View();
            }

            var exists = await _context.Accounts.AnyAsync(a => a.Username == username);
            if (exists)
            {
                ViewBag.Error = "Username already exists.";
                return View();
            }

            var account = new Account
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                DisplayName = string.IsNullOrEmpty(displayName) ? username : displayName,
                CreatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserRole", account.Role);
            HttpContext.Session.SetString("UserName", account.DisplayName ?? account.Username);
            HttpContext.Session.SetString("UserId", account.Id.ToString());

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}