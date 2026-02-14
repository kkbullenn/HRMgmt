using HRMgmt.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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

            // IMPORTANT: assumes OrgDbContext has DbSet<Account> Accounts
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

            // You don't have HomeController in your project right now
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

        // POST: Account/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Username,PasswordHash,Role,DisplayName,CreatedAt")] Account account)
        {
            if (!ModelState.IsValid) return View(account);

            _context.Add(account);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
    }
}
