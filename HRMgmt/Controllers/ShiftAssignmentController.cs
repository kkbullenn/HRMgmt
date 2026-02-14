using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HRMgmt.Models;

namespace HRMgmt.Controllers
{
    public class ShiftAssignmentController : Controller
    {
        private readonly OrgDbContext _context;

        public ShiftAssignmentController(OrgDbContext context)
        {
            _context = context;
        }

        // GET: ShiftAssignment
        public async Task<IActionResult> Index()
        {
            return View(await _context.ShiftAssignments.ToListAsync());
        }

        // GET: ShiftAssignment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shiftAssignment = await _context.ShiftAssignments
                .FirstOrDefaultAsync(m => m.Id == id);
            if (shiftAssignment == null)
            {
                return NotFound();
            }

            return View(shiftAssignment);
        }

        // GET: ShiftAssignment/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ShiftAssignment/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ShiftId,UserId")] ShiftAssignment shiftAssignment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(shiftAssignment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(shiftAssignment);
        }

        // GET: ShiftAssignment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shiftAssignment = await _context.ShiftAssignments.FindAsync(id);
            if (shiftAssignment == null)
            {
                return NotFound();
            }
            return View(shiftAssignment);
        }

        // POST: ShiftAssignment/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ShiftId,UserId")] ShiftAssignment shiftAssignment)
        {
            if (id != shiftAssignment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shiftAssignment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShiftAssignmentExists(shiftAssignment.Id))
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
            return View(shiftAssignment);
        }

        // GET: ShiftAssignment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shiftAssignment = await _context.ShiftAssignments
                .FirstOrDefaultAsync(m => m.Id == id);
            if (shiftAssignment == null)
            {
                return NotFound();
            }

            return View(shiftAssignment);
        }

        // POST: ShiftAssignment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shiftAssignment = await _context.ShiftAssignments.FindAsync(id);
            if (shiftAssignment != null)
            {
                _context.ShiftAssignments.Remove(shiftAssignment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShiftAssignmentExists(int id)
        {
            return _context.ShiftAssignments.Any(e => e.Id == id);
        }
    }
}
