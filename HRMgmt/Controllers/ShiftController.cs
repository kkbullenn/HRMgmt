using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        private static readonly string[] DaysOfWeek = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday","Sunday" };

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
            return View("~/Views/ShiftAssignment/EmployeeShift.cshtml");
        }

        [HttpGet]
        public JsonResult GetTemplateData(string name, int? weekType = null, int? weekIndex = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { templateName = "", weekType = 1, weekIndex = 0, grid = new List<string>() });
            }

            var templateQuery = _context.SchedulingTemplates
                .Where(t => t.TemplateName == name);

            var latest = templateQuery
                .OrderByDescending(t => t.Id)
                .FirstOrDefault();
            if (latest == null)
            {
                return Json(new { templateName = name, weekType = 1, weekIndex = 0, grid = new List<string>() });
            }

            int resolvedWeekType;
            int resolvedWeekIndex;
            if (weekType.HasValue || weekIndex.HasValue)
            {
                resolvedWeekType = weekType ?? latest.WeekType;
                resolvedWeekIndex = weekIndex ?? latest.WeekIndex;
            }
            else
            {
                // Default open behavior: always prefer Week 1 first.
                if (templateQuery.Any(t => t.WeekType == 2 && t.WeekIndex == 0))
                {
                    resolvedWeekType = 2;
                    resolvedWeekIndex = 0;
                }
                else if (templateQuery.Any(t => t.WeekType == 1 && t.WeekIndex == 0))
                {
                    resolvedWeekType = 1;
                    resolvedWeekIndex = 0;
                }
                else
                {
                    resolvedWeekType = latest.WeekType;
                    resolvedWeekIndex = latest.WeekIndex;
                }
            }

            if (resolvedWeekType == 1)
            {
                resolvedWeekIndex = 0;
            }
            else
            {
                resolvedWeekIndex = resolvedWeekIndex <= 0 ? 0 : 1;
            }

            var users = _context.Users.OrderBy(u => u.UserId).ToList();
            var rows = templateQuery
                .Where(t => t.WeekType == resolvedWeekType && t.WeekIndex == resolvedWeekIndex)
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
                weekType = resolvedWeekType,
                weekIndex = resolvedWeekIndex,
                grid
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveShiftTemplate([FromForm] EmployeeShiftGridViewModel model, string templateName, int weekType = 1, int weekIndex = 0, string? allWeeksGridJson = null, string? originalTemplateName = null)
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                TempData["Error"] = "Template name is required.";
                return RedirectToAction(nameof(ShiftAssignment));
            }

            templateName = templateName.Trim();
            originalTemplateName = (originalTemplateName ?? string.Empty).Trim();

            var templateExists = _context.SchedulingTemplates.Any(t => t.TemplateName == templateName);
            var isCreateFlow = string.IsNullOrWhiteSpace(originalTemplateName);
            if (isCreateFlow && templateExists)
            {
                TempData["Error"] = $"Template name \"{templateName}\" already exists. Please choose a different name.";
                return RedirectToAction(nameof(ShiftAssignment));
            }

            var userIds = model.Users?.Select(u => u.UserId).ToList() ?? new List<Guid>();
            var expected = userIds.Count * 7;

            List<string> NormalizeGrid(List<string>? source)
            {
                var normalized = source ?? new List<string>();
                if (normalized.Count < expected)
                {
                    normalized.AddRange(Enumerable.Repeat(string.Empty, expected - normalized.Count));
                }
                else if (normalized.Count > expected && expected > 0)
                {
                    normalized = normalized.Take(expected).ToList();
                }
                return normalized;
            }

            var gridsToSave = new List<(int WeekType, int WeekIndex, List<string> Grid)>();

            if (!string.IsNullOrWhiteSpace(allWeeksGridJson))
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<List<WeekGridPayload>>(allWeeksGridJson) ?? new List<WeekGridPayload>();
                    foreach (var item in payload)
                    {
                        if (item == null) continue;
                        var payloadWeekType = item.weekType == 2 ? 2 : 1;
                        var payloadWeekIndex = item.weekIndex <= 0 ? 0 : 1;
                        if (payloadWeekType == 1) payloadWeekIndex = 0;
                        gridsToSave.Add((payloadWeekType, payloadWeekIndex, NormalizeGrid(item.grid)));
                    }
                }
                catch
                {
                    // Fallback to current posted grid below.
                }
            }

            if (gridsToSave.Count == 0)
            {
                var fallbackWeekType = weekType == 2 ? 2 : 1;
                var fallbackWeekIndex = fallbackWeekType == 1 ? 0 : (weekIndex <= 0 ? 0 : 1);
                gridsToSave.Add((fallbackWeekType, fallbackWeekIndex, NormalizeGrid(model.Grid)));
            }

            // Deduplicate by (WeekType, WeekIndex), keep the last submitted grid.
            gridsToSave = gridsToSave
                .GroupBy(g => new { g.WeekType, g.WeekIndex })
                .Select(g => g.Last())
                .ToList();

            var oldRows = _context.SchedulingTemplates
                .Where(t => t.TemplateName == templateName && t.WeekType == weekType)
                .ToList();
            if (oldRows.Count > 0)
            {
                _context.SchedulingTemplates.RemoveRange(oldRows);
                // Flush deletes first to avoid unique-key collisions when re-inserting same cells.
                _context.SaveChanges();
            }

            foreach (var block in gridsToSave)
            {
                for (var i = 0; i < userIds.Count; i++)
                {
                    for (var j = 0; j < 7; j++)
                    {
                        var idx = i * 7 + j;
                        if (idx >= block.Grid.Count) continue;
                        var shiftIdRaw = (block.Grid[idx] ?? string.Empty).Trim();
                        if (!Guid.TryParse(shiftIdRaw, out var shiftId)) continue;

                        _context.SchedulingTemplates.Add(new SchedulingTemplate
                        {
                            TemplateName = templateName,
                            UserId = userIds[i],
                            WeekType = block.WeekType,
                            WeekIndex = block.WeekIndex,
                            DayOfWeek = DaysOfWeek[j],
                            ShiftType = shiftId.ToString()
                        });
                    }
                }
            }

            _context.SaveChanges();
            TempData["Success"] = $"{templateName} saved successfully.";

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

            var globalStart = dto.startDate?.Date;
            var globalEnd = dto.endDate?.Date;
            DateOnly? globalStartOnly = globalStart != null ? DateOnly.FromDateTime(globalStart.Value) : (DateOnly?)null;
            DateOnly? globalEndOnly = globalEnd != null ? DateOnly.FromDateTime(globalEnd.Value) : (DateOnly?)null;
            var shiftMap = shifts.ToDictionary(s => s.ShiftId, s => s);
            var isBiweekly = dto.weekType == 2;
            var weeklyRows = templateRows.Where(r => r.WeekType == 1).ToList();
            var biweeklyRows = templateRows.Where(r => r.WeekType == 2).ToList();
            if (isBiweekly)
            {
                var hasWeek1 = biweeklyRows.Any(r => r.WeekIndex == 0);
                var hasWeek2 = biweeklyRows.Any(r => r.WeekIndex == 1);
                if (!hasWeek1 || !hasWeek2)
                {
                    return Json(new { success = false, message = "Biweekly generation requires both Week 1 and Week 2 templates." });
                }
            }
            else if (weeklyRows.Count == 0)
            {
                return Json(new { success = false, message = "Weekly generation requires a Weekly template (Week Type = Weekly)." });
            }
            // delete existing assignments that fall into any of the to-be-generated cells, to avoid conflicts and ensure idempotency
            var assignmentsToDelete = new List<ShiftAssignment>();
            foreach (var row in templateRows) {
                var shiftIdRaw = (row.ShiftType ?? string.Empty).Trim();
                if (!Guid.TryParse(shiftIdRaw, out var shiftId)) continue;
                if (!shiftMap.TryGetValue(shiftId, out var shift)) continue;
                var shiftStart = DateOnly.FromDateTime(shift.StartDate);
                var shiftEnd = DateOnly.FromDateTime(shift.EndDate);
                var rangeStart = globalStartOnly ?? shiftStart;
                var rangeEnd = globalEndOnly ?? shiftEnd;
                if (rangeStart < shiftStart) rangeStart = shiftStart;
                if (rangeEnd > shiftEnd) rangeEnd = shiftEnd;
                if (rangeEnd < rangeStart) continue;
                var toDel = _context.ShiftAssignments.Where(sa =>
                    sa.UserId == row.UserId &&
                    sa.ShiftId == shift.ShiftId &&
                    sa.ShiftDate >= rangeStart &&
                    sa.ShiftDate <= rangeEnd
                ).ToList();
                assignmentsToDelete.AddRange(toDel);
            }
            if (assignmentsToDelete.Count > 0) {
                _context.ShiftAssignments.RemoveRange(assignmentsToDelete);
                _context.SaveChanges();
            }
            var added = 0;
            var existingUserDateKeys = new HashSet<string>(
                _context.ShiftAssignments.Select(sa => $"{sa.UserId}_{sa.ShiftDate:yyyy-MM-dd}").ToList(),
                StringComparer.OrdinalIgnoreCase);
            foreach (var row in templateRows)
            {
                var shiftIdRaw = (row.ShiftType ?? string.Empty).Trim();
                if (!Guid.TryParse(shiftIdRaw, out var shiftId)) continue;
                if (!shiftMap.TryGetValue(shiftId, out var shift)) continue;
                var shiftStart = DateOnly.FromDateTime(shift.StartDate);
                var shiftEnd = DateOnly.FromDateTime(shift.EndDate);
                var rangeStart = globalStartOnly ?? shiftStart;
                var rangeEnd = globalEndOnly ?? shiftEnd;
                if (rangeStart < shiftStart) rangeStart = shiftStart;
                if (rangeEnd > shiftEnd) rangeEnd = shiftEnd;
                if (rangeEnd < rangeStart) continue;
                for (var date = rangeStart; date <= rangeEnd; date = date.AddDays(1))
                {
                    var dayName = date.DayOfWeek.ToString();
                    bool match = false;
                    if (isBiweekly && row.WeekType == 2)
                    {
                        var weekIndex = ((date.DayNumber - rangeStart.DayNumber) / 7) % 2;
                        match = (row.WeekIndex == weekIndex) && row.DayOfWeek.Equals(dayName, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (!isBiweekly && row.WeekType == 1)
                    {
                        match = row.DayOfWeek.Equals(dayName, StringComparison.OrdinalIgnoreCase);
                    }
                    if (!match) continue;
                    var userDateKey = $"{row.UserId}_{date:yyyy-MM-dd}";
                    if (existingUserDateKeys.Contains(userDateKey)) continue;
                    var exists = _context.ShiftAssignments.Any(sa =>
                        sa.UserId == row.UserId &&
                        sa.ShiftId == shift.ShiftId &&
                        sa.ShiftDate == date);
                    if (exists) continue;
                    _context.ShiftAssignments.Add(new ShiftAssignment
                    {
                        UserId = row.UserId,
                        ShiftId = shift.ShiftId,
                        ShiftDate = date
                    });
                    existingUserDateKeys.Add(userDateKey);
                    added++;
                }
            }

            _context.SaveChanges();
            DateOnly? minDate = null;
            DateOnly? maxDate = null;
            foreach (var row in templateRows)
            {
                var shiftIdRaw = (row.ShiftType ?? string.Empty).Trim();
                if (!Guid.TryParse(shiftIdRaw, out var shiftId)) continue;
                if (!shiftMap.TryGetValue(shiftId, out var shift)) continue;
                var shiftStart = DateOnly.FromDateTime(shift.StartDate);
                var shiftEnd = DateOnly.FromDateTime(shift.EndDate);
                var rangeStart = globalStartOnly ?? shiftStart;
                var rangeEnd = globalEndOnly ?? shiftEnd;
                if (rangeStart < shiftStart) rangeStart = shiftStart;
                if (rangeEnd > shiftEnd) rangeEnd = shiftEnd;
                if (rangeEnd < rangeStart) continue;
                if (minDate == null || rangeStart < minDate) minDate = rangeStart;
                if (maxDate == null || rangeEnd > maxDate) maxDate = rangeEnd;
            }
            _context.TemplateGenerationLogs.Add(new TemplateGenerationLog
            {
                TemplateName = dto.templateName.Trim(),
                WeekType = dto.weekType == 2 ? 2 : 1,
                StartDate = minDate ?? DateOnly.FromDateTime(DateTime.Today),
                EndDate = maxDate ?? DateOnly.FromDateTime(DateTime.Today),
                GeneratedAt = DateTime.UtcNow,
                GeneratedCount = added
            });
            _context.SaveChanges();
            return Json(new
            {
                success = true,
                count = added,
                message = $"Template applied using range {(minDate?.ToString("yyyy-MM-dd") ?? "-")} to {(maxDate?.ToString("yyyy-MM-dd") ?? "-")}."
            });
        }

        [HttpPost]
        public JsonResult DeleteTemplate([FromBody] DeleteTemplateDto dto)
        {
            var templateName = dto?.templateName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(templateName))
            {
                return Json(new { success = false, message = "Template name is required." });
            }

            var rows = _context.SchedulingTemplates
                .Where(t => t.TemplateName == templateName)
                .ToList();
            if (rows.Count == 0)
            {
                return Json(new { success = false, message = "Template is not found." });
            }

            _context.SchedulingTemplates.RemoveRange(rows);
            _context.SaveChanges();
            return Json(new { success = true, message = $"Template \"{templateName}\" deleted.", count = rows.Count });
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
            var model = new Shift
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today,
                RecurrenceType = RecurrenceType.Daily,
                RecurrenceDays = string.Empty
            };
            return View(model);
        }

        // ================= EMPLOYEE SHIFT APIs =================

        // Get all employees (for frontend dropdown)
        [HttpGet]
        public JsonResult GetEmployees()
        {
            var users = _context.Users.Select(u => new
            {
                id = u.UserId,
                name = u.FirstName + " " + u.LastName
            }).ToList();
            return Json(users);
        }

        // Get shifts for a specific employee (for calendar events)
        [HttpGet]
        public JsonResult GetEmployeeShifts(Guid employeeId)
        {
            var assignments = _context.ShiftAssignments
                .Where(sa => sa.UserId == employeeId)
                .Join(_context.Shifts, sa => sa.ShiftId, s => s.ShiftId, (sa, s) => new
                {
                    id = sa.ShiftId,
                    title = s.Name + " " + s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm"),
                    start = sa.ShiftDate.ToString("yyyy-MM-dd"),
                    end = sa.ShiftDate.ToString("yyyy-MM-dd")
                }).ToList();

            return Json(assignments);
        }

        // FullCalendar: get all shifts
        [HttpGet]
        public JsonResult GetShifts()
        {
            var shifts = _context.Shifts.Select(s => new
            {
                id = s.ShiftId,
                title = s.Name,
                start = s.StartTime.ToString(@"hh\:mm"),
                end = s.EndTime.ToString(@"hh\:mm")
            }).ToList();

            return Json(shifts);
        }

        // Assign a shift to an employee
        [HttpPost]
        public JsonResult AssignShift([FromBody] AssignShiftDto dto)
        {
            try
            {
                var exists = _context.ShiftAssignments.Any(sa =>
                    sa.UserId == dto.UserId &&
                    sa.ShiftDate == DateOnly.FromDateTime(dto.Date));

                if (exists)
                {
                    return Json(new { success = false, message = "This user already has a shift on that date." });
                }

                var assignment = new ShiftAssignment
                {
                    ShiftId = dto.ShiftId,
                    UserId = dto.UserId,
                    ShiftDate = DateOnly.FromDateTime(dto.Date)
                };

                _context.ShiftAssignments.Add(assignment);

                try
                {
                    _context.SaveChanges();
                }
                catch (Exception dbEx)
                {
                    var values = new Dictionary<string, object>
                    {
                        {"dto.UserId", dto.UserId},
                        {"dto.ShiftId", dto.ShiftId},
                        {"dto.Date", dto.Date},
                        {"assignment.ShiftDate", assignment.ShiftDate},
                        {"assignment.UserId", assignment.UserId},
                        {"assignment.ShiftId", assignment.ShiftId}
                    };
                    return Json(new {
                        success = false,
                        message = "DB error: " + dbEx.Message,
                        inner = dbEx.InnerException?.Message,
                        values
                    });
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class AssignShiftDto
        {
            public Guid UserId { get; set; }
            public Guid ShiftId { get; set; }
            public DateTime Date { get; set; }
        }

        // Delete shift assignment
        [HttpPost]
        public JsonResult DeleteEmployeeShift([FromBody] DeleteEmployeeShiftDto dto)
        {
            var assignment = _context.ShiftAssignments
                .FirstOrDefault(sa =>
                    sa.UserId == dto.UserId &&
                    sa.ShiftId == dto.ShiftId &&
                    sa.ShiftDate == DateOnly.FromDateTime(dto.Date));

            if (assignment == null)
                return Json(new { success = false, message = "Shift assignment not found." });

            _context.ShiftAssignments.Remove(assignment);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        public class DeleteEmployeeShiftDto
        {
            public Guid UserId { get; set; }
            public Guid ShiftId { get; set; }
            public DateTime Date { get; set; }
        }

        // POST: Shift/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ShiftId,Name,RequiredCount,StartTime,EndTime,StartDate,EndDate,RecurrenceType,RecurrenceDays")] Shift shift)
        {
            // UI currently hides recurrence inputs; keep safe defaults for validation/persistence.
            if (!Enum.IsDefined(typeof(RecurrenceType), shift.RecurrenceType))
            {
                shift.RecurrenceType = RecurrenceType.Daily;
            }
            shift.RecurrenceDays ??= string.Empty;
            ModelState.Remove(nameof(Shift.RecurrenceType));
            ModelState.Remove(nameof(Shift.RecurrenceDays));

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

            if (!Enum.IsDefined(typeof(RecurrenceType), shift.RecurrenceType))
            {
                shift.RecurrenceType = RecurrenceType.Daily;
            }
            shift.RecurrenceDays ??= string.Empty;
            ModelState.Remove(nameof(Shift.RecurrenceType));
            ModelState.Remove(nameof(Shift.RecurrenceDays));

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
            public int weekType { get; set; } = 1;
        }

        public sealed class DeleteTemplateDto
        {
            public string templateName { get; set; } = string.Empty;
        }

        private sealed class WeekGridPayload
        {
            public int weekType { get; set; }
            public int weekIndex { get; set; }
            public List<string> grid { get; set; } = new();
        }
    }
}
