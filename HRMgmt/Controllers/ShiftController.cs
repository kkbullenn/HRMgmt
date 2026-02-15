using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HRMgmt.Models;
using HRMgmt.ViewModels;

namespace HRMgmt
{
    public class ShiftController : Controller
    {
        private readonly OrgDbContext _context;
        private static readonly string[] DaysOfWeek = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        public ShiftController(OrgDbContext context)
        {
            _context = context;
        }

        // GET: Shift
        public async Task<IActionResult> Index()
        {
            return View(await _context.Shifts.ToListAsync());
        }

        // GET: Shift/ShiftAssignment
        public IActionResult ShiftAssignment()
        {
            var users = _context.Users.OrderBy(u => u.UserId).ToList();
            var model = new EmployeeShiftGridViewModel
            {
                Users = users,
                Shifts = _context.Shifts.OrderBy(s => s.Name).ToList(),
                Grid = Enumerable.Repeat(string.Empty, users.Count * 7).ToList()
            };
            ViewBag.TemplateNames = _context.SchedulingTemplates
                .Select(t => t.TemplateName)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
            return View("~/Views/ShiftAssignment/ShiftAssignment.cshtml", model);
        }

        // Backward-compatible route name from old page.
        public IActionResult AssignGrid()
        {
            return RedirectToAction(nameof(ShiftAssignment));
        }

        public IActionResult EmployeeShift()
        {
            return RedirectToAction(nameof(ShiftAssignment));
        }

        [HttpGet]
        public JsonResult GetTemplateData(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { templateName = "", weekType = 1, weekIndex = 0, grid = new List<string>() });
            }

            var latest = _context.SchedulingTemplates
                .Where(t => t.TemplateName == name)
                .OrderByDescending(t => t.Id)
                .FirstOrDefault();
            if (latest == null)
            {
                return Json(new { templateName = name, weekType = 1, weekIndex = 0, grid = new List<string>() });
            }

            var users = _context.Users.OrderBy(u => u.UserId).ToList();
            var rows = _context.SchedulingTemplates
                .Where(t => t.TemplateName == name && t.WeekType == latest.WeekType && t.WeekIndex == latest.WeekIndex)
                .ToList();

            var lookup = rows.ToDictionary(
                t => $"{t.UserId}_{t.DayOfWeek}",
                t => t.ShiftType,
                StringComparer.OrdinalIgnoreCase);

            var grid = new List<string>(users.Count * 7);
            foreach (var user in users)
            {
                foreach (var day in DaysOfWeek)
                {
                    lookup.TryGetValue($"{user.UserId}_{day}", out var shiftType);
                    grid.Add(shiftType ?? string.Empty);
                }
            }

            return Json(new
            {
                templateName = name,
                weekType = latest.WeekType,
                weekIndex = latest.WeekIndex,
                grid
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveShiftTemplate([FromForm] EmployeeShiftGridViewModel model, string templateName, int weekType = 1, int weekIndex = 0)
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                TempData["Error"] = "Template name is required.";
                return RedirectToAction(nameof(ShiftAssignment));
            }

            var userIds = model.Users?.Select(u => u.UserId).ToList() ?? new List<Guid>();
            var grid = model.Grid ?? new List<string>();
            var expected = userIds.Count * 7;
            if (grid.Count < expected)
            {
                grid.AddRange(Enumerable.Repeat(string.Empty, expected - grid.Count));
            }
            else if (grid.Count > expected && expected > 0)
            {
                grid = grid.Take(expected).ToList();
            }

            var oldRows = _context.SchedulingTemplates
                .Where(t => t.TemplateName == templateName && t.WeekType == weekType && t.WeekIndex == weekIndex)
                .ToList();
            if (oldRows.Count > 0)
            {
                _context.SchedulingTemplates.RemoveRange(oldRows);
            }

            for (var i = 0; i < userIds.Count; i++)
            {
                for (var j = 0; j < 7; j++)
                {
                    var idx = i * 7 + j;
                    if (idx >= grid.Count) continue;
                    var shiftType = (grid[idx] ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(shiftType)) continue;

                    _context.SchedulingTemplates.Add(new SchedulingTemplate
                    {
                        TemplateName = templateName.Trim(),
                        UserId = userIds[i],
                        WeekType = weekType,
                        WeekIndex = weekIndex,
                        DayOfWeek = DaysOfWeek[j],
                        ShiftType = shiftType
                    });
                }
            }

            _context.SaveChanges();

            return RedirectToAction(nameof(ShiftAssignment));
        }

        [HttpPost]
        public JsonResult AutoAssignByTemplate([FromBody] AutoAssignByTemplateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.templateName))
            {
                return Json(new { success = false, message = "Template name is required." });
            }

            var templateRows = _context.SchedulingTemplates
                .Where(t => t.TemplateName == dto.templateName)
                .ToList();
            if (templateRows.Count == 0)
            {
                return Json(new { success = false, message = "Template not found." });
            }

            var shifts = _context.Shifts.ToList();
            if (shifts.Count == 0)
            {
                return Json(new { success = false, message = "No shifts found in the system." });
            }

            // If range is not provided, fallback to the min/max range from Shift table.
            var effectiveStart = dto.startDate?.Date ?? shifts.Min(s => s.StartDate.Date);
            var effectiveEnd = dto.endDate?.Date ?? shifts.Max(s => s.EndDate.Date);
            if (effectiveEnd < effectiveStart)
            {
                return Json(new { success = false, message = "End date must be on or after start date." });
            }

            var shiftsInRange = shifts
                .Where(s => s.StartDate.Date <= effectiveEnd && s.EndDate.Date >= effectiveStart)
                .ToList();
            if (shiftsInRange.Count == 0)
            {
                return Json(new { success = false, message = "No shifts match the selected assignment range." });
            }

            var added = 0;
            for (var date = effectiveStart; date <= effectiveEnd; date = date.AddDays(1))
            {
                var dayName = date.DayOfWeek.ToString();
                var biWeekIndex = ((date - effectiveStart).Days / 7) % 2;
                var weekType = biWeekIndex == 0 ? 1 : 2;
                var weekIndex = biWeekIndex;
                var shiftDate = DateOnly.FromDateTime(date);

                var rowsForDay = templateRows.Where(r =>
                    r.DayOfWeek.Equals(dayName, StringComparison.OrdinalIgnoreCase) &&
                    r.WeekType == weekType &&
                    r.WeekIndex == weekIndex);

                foreach (var row in rowsForDay)
                {
                    var shiftType = (row.ShiftType ?? string.Empty).Trim().ToLowerInvariant();
                    if (string.IsNullOrEmpty(shiftType))
                    {
                        continue;
                    }

                    var shift = shiftsInRange
                        .FirstOrDefault(s => s.Name.Trim().Equals(shiftType, StringComparison.OrdinalIgnoreCase));
                    if (shift == null) continue;

                    var exists = _context.ShiftAssignments.Any(sa =>
                        sa.UserId == row.UserId &&
                        sa.ShiftId == shift.ShiftId &&
                        sa.ShiftDate == shiftDate);
                    if (exists) continue;

                    _context.ShiftAssignments.Add(new ShiftAssignment
                    {
                        UserId = row.UserId,
                        ShiftId = shift.ShiftId,
                        ShiftDate = shiftDate
                    });
                    added++;
                }
            }

            _context.SaveChanges();
            return Json(new
            {
                success = true,
                count = added,
                message = $"Template applied using range {effectiveStart:yyyy-MM-dd} to {effectiveEnd:yyyy-MM-dd}."
            });
        }

        // GET: Shift/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shift = await _context.Shifts
                .FirstOrDefaultAsync(m => m.ShiftId == id);
            if (shift == null)
            {
                return NotFound();
            }

            return View(shift);
        }

        // GET: Shift/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Shift/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ShiftId,Name,RequiredCount,StartTime,EndTime,StartDate,EndDate,RecurrenceType,RecurrenceDays")] Shift shift)
        {
            if (ModelState.IsValid)
            {
                shift.ShiftId = Guid.NewGuid();
                _context.Add(shift);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(shift);
        }

        // GET: Shift/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null)
            {
                return NotFound();
            }
            return View(shift);
        }

        // POST: Shift/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("ShiftId,Name,RequiredCount,StartTime,EndTime,StartDate,EndDate,RecurrenceType,RecurrenceDays")] Shift shift)
        {
            if (id != shift.ShiftId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shift);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShiftExists(shift.ShiftId))
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
            return View(shift);
        }

        // GET: Shift/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shift = await _context.Shifts
                .FirstOrDefaultAsync(m => m.ShiftId == id);
            if (shift == null)
            {
                return NotFound();
            }

            return View(shift);
        }

        // POST: Shift/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift != null)
            {
                _context.Shifts.Remove(shift);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShiftExists(Guid id)
        {
            return _context.Shifts.Any(e => e.ShiftId == id);
        }

        public sealed class AutoAssignByTemplateDto
        {
            public DateTime? startDate { get; set; }
            public DateTime? endDate { get; set; }
            public string templateName { get; set; } = string.Empty;
        }
    }
}
