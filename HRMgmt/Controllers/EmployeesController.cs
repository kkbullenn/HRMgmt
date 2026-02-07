using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HRMgmt;
using HRMgmt.Models;

namespace HRMgmt.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly OrgDbContext _context;

        public EmployeesController(OrgDbContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            return View(await _context.Employees.ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.ID == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Salary,ID,Name,Address,DateOfBirth,Role")] Employee employee, IFormFile Photo)
        {
            Console.WriteLine($"[DEBUG] Create POST called at {DateTime.Now}");
            if (Photo != null && Photo.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await Photo.CopyToAsync(ms);
                    employee.Photo = ms.ToArray();
                    Console.WriteLine($"[DEBUG] Photo uploaded, length = {employee.Photo?.Length}");
                }
            }

            ModelState.Remove(nameof(Employee.Photo));
            bool hasError = false;
            foreach (var kv in ModelState)
            {
                foreach (var err in kv.Value.Errors)
                {
                    Console.WriteLine($"[DEBUG] ModelState error: {kv.Key}: {err.ErrorMessage}");
                    hasError = true;
                }
            }
            if (ModelState.IsValid)
            {
                Console.WriteLine("[DEBUG] ModelState is valid, saving employee...");
                employee.ID = Guid.NewGuid();
                _context.Add(employee);
                await _context.SaveChangesAsync();
                Console.WriteLine("[DEBUG] Employee saved, redirecting to Index.");
                return RedirectToAction(nameof(Index));
            }
            else
            {
                Console.WriteLine("[DEBUG] ModelState is invalid, returning to Create view.");
            }
            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Salary,ID,Name,Address,DateOfBirth,Role")] Employee employee, IFormFile Photo, string ExistingPhoto)
        {
            if (id != employee.ID)
            {
                return NotFound();
            }

            if (Photo != null && Photo.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await Photo.CopyToAsync(ms);
                    employee.Photo = ms.ToArray();
                    Console.WriteLine($"Photo.Length = {employee.Photo?.Length}");

                }
            }
            else if (!string.IsNullOrEmpty(ExistingPhoto))
            {
                Console.WriteLine("Using ExistingPhoto");
                employee.Photo = Convert.FromBase64String(ExistingPhoto);
                Console.WriteLine(employee.Photo?.Length);
            }

            ModelState.Remove(nameof(Employee.Photo));
            ModelState.Remove(nameof(ExistingPhoto));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.ID))
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
            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.ID == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(Guid id)
        {
            return _context.Employees.Any(e => e.ID == id);
        }
    }
}
