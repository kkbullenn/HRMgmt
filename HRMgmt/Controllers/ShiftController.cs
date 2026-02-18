using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            var role = HttpContext.Session.GetString("UserRole");
            if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(MyShifts));
                }

                return RedirectToAction("Index", "Home");
            }

            return View(await _context.Shifts.ToListAsync());
        }

        // GET: Shift/MyShifts
        // Employee-only view: list shifts for the current logged-in employee.
        public async Task<IActionResult> MyShifts()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (!string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Index));
            }

            var userId = await ResolveCurrentEmployeeUserIdAsync();
            if (userId == null)
            {
                ViewBag.Error = "Unable to map this account to an employee record. Please ask HR to verify account/profile mapping.";
                return View(new List<EmployeeShiftListItemViewModel>());
            }

            var employee = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (employee == null)
            {
                ViewBag.Error = "Employee profile not found.";
                return View(new List<EmployeeShiftListItemViewModel>());
            }

            var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
            var relatedUserIds = await _context.Users
                .AsNoTracking()
                .Where(u => u.FirstName == employee.FirstName && u.LastName == employee.LastName)
                .Select(u => u.UserId)
                .ToListAsync();

            if (!relatedUserIds.Contains(userId.Value))
            {
                relatedUserIds.Add(userId.Value);
            }

            var shifts = await (
                from sa in _context.ShiftAssignments.AsNoTracking()
                join s in _context.Shifts.AsNoTracking() on sa.ShiftId equals s.ShiftId
                where relatedUserIds.Contains(sa.UserId)
                orderby sa.ShiftDate, s.StartTime
                select new EmployeeShiftListItemViewModel
                {
                    Name = employeeName,
                    ShiftName = s.Name,
                    ShiftDate = sa.ShiftDate,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                }).ToListAsync();

            if (shifts.Count == 0)
            {
                var templates = await _context.SchedulingTemplates
                    .AsNoTracking()
                    .Where(t => relatedUserIds.Contains(t.UserId))
                    .OrderBy(t => t.TemplateName)
                    .ThenBy(t => t.WeekType)
                    .ThenBy(t => t.WeekIndex)
                    .ThenBy(t => t.DayOfWeek)
                    .ToListAsync();

                var shiftIds = templates
                    .Select(t => t.ShiftType)
                    .Distinct()
                    .ToList();

                var shiftLookup = await _context.Shifts
                    .AsNoTracking()
                    .Where(s => shiftIds.Contains(s.ShiftId.ToString()))
                    .ToDictionaryAsync(s => s.ShiftId.ToString(), s => s);

                var templateRows = new List<EmployeeShiftListItemViewModel>();
                foreach (var template in templates)
                {
                    if (!shiftLookup.TryGetValue(template.ShiftType, out var shift))
                    {
                        continue;
                    }

                    if (!TryParseDayOfWeek(template.DayOfWeek, out var dayOfWeek))
                    {
                        continue;
                    }

                    templateRows.Add(new EmployeeShiftListItemViewModel
                    {
                        Name = template.TemplateName,
                        ShiftName = shift.Name,
                        ShiftDate = ResolveTemplateDate(dayOfWeek, template.WeekType, template.WeekIndex),
                        StartTime = shift.StartTime,
                        EndTime = shift.EndTime
                    });
                }

                shifts = templateRows
                    .OrderBy(x => x.ShiftDate)
                    .ThenBy(x => x.StartTime)
                    .ToList();
            }

            return View(shifts);
        }

        // GET: Shift/ShiftAssignment
        public IActionResult ShiftAssignment()
        {
            var users = GetScheduleUsers();
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

        private void EnsureEmployeeUsersSynced()
        {
            var employeeRoleId = _context.Roles
                .Where(r => r.RoleName.ToLower() == "employee")
                .Select(r => r.Id)
                .FirstOrDefault();

            if (employeeRoleId == 0)
            {
                var role = new Role { RoleName = "Employee" };
                _context.Roles.Add(role);
                _context.SaveChanges();
                employeeRoleId = role.Id;
            }

            var employeeAccounts = _context.Account
                .Where(a => a.Role.ToLower() == "employee")
                .ToList();

            var hasNewUsers = false;
            foreach (var account in employeeAccounts)
            {
                var (firstName, lastName) = BuildName(account.DisplayName, account.Username);
                var syncAddress = BuildAutoSyncedAddress(account.Username);

                var exists = _context.Users.Any(u =>
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
                _context.SaveChanges();
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

        private static bool TryParseDayOfWeek(string dayName, out DayOfWeek dayOfWeek)
        {
            if (Enum.TryParse(dayName, true, out dayOfWeek))
            {
                return true;
            }

            dayOfWeek = DayOfWeek.Monday;
            return false;
        }

        private static DateOnly ResolveTemplateDate(DayOfWeek dayOfWeek, int weekType, int weekIndex)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var current = today.DayOfWeek;
            var delta = ((int)dayOfWeek - (int)current + 7) % 7;
            var resolved = today.AddDays(delta);

            if (weekType == 2)
            {
                resolved = resolved.AddDays((weekIndex <= 0 ? 0 : 1) * 7);
            }

            return resolved;
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

            var users = GetScheduleUsers();
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

        private List<User> GetScheduleUsers()
        {
            EnsureEmployeeUsersSynced();

            var employeeRoleIds = _context.Roles
                .Where(r => r.RoleName.ToLower() == "employee")
                .Select(r => r.Id)
                .ToList();

            if (employeeRoleIds.Count == 0)
            {
                return new List<User>();
            }

            var users = _context.Users
                .AsNoTracking()
                .Where(u => employeeRoleIds.Contains(u.Role))
                .ToList();

            var deduped = users
                .OrderByDescending(u => !string.IsNullOrWhiteSpace(u.Address) && u.Address.StartsWith("AutoSynced:", StringComparison.OrdinalIgnoreCase))
                .ThenBy(u => u.UserId)
                .GroupBy(u => $"{u.FirstName} {u.LastName}".Trim().ToLowerInvariant())
                .Select(g => g.First())
                .OrderBy(u => u.UserId)
                .ToList();

            return deduped;
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

        private async Task<Guid?> ResolveCurrentEmployeeUserIdAsync()
        {
            var sessionUserId = HttpContext.Session.GetString("UserId");

            if (Guid.TryParse(sessionUserId, out var guidUserId))
            {
                var existsByGuid = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.UserId == guidUserId);

                if (existsByGuid)
                {
                    return guidUserId;
                }
            }

            var sessionDisplayName = (HttpContext.Session.GetString("UserName") ?? string.Empty).Trim();

            static string Normalize(string? value) =>
                (value ?? string.Empty).Trim().ToLowerInvariant();

            string? username = null;

            if (int.TryParse(sessionUserId, out var accountId))
            {
                username = await _context.Account
                    .AsNoTracking()
                    .Where(a => a.Id == accountId)
                    .Select(a => a.Username)
                    .FirstOrDefaultAsync();

                var autosyncedAddress = BuildAutoSyncedAddress(username ?? string.Empty);
                var byAutoSyncedAddress = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Address == autosyncedAddress)
                    .Select(u => (Guid?)u.UserId)
                    .FirstOrDefaultAsync();

                if (byAutoSyncedAddress != null)
                {
                    return byAutoSyncedAddress;
                }
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                var normalizedUsername = Normalize(username);
                var byUsername = await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        u.FirstName.ToLower() == normalizedUsername ||
                        (u.FirstName + u.LastName).ToLower() == normalizedUsername ||
                        (u.FirstName + "." + u.LastName).ToLower() == normalizedUsername ||
                        (u.FirstName + "_" + u.LastName).ToLower() == normalizedUsername)
                    .Select(u => (Guid?)u.UserId)
                    .FirstOrDefaultAsync();

                if (byUsername != null)
                {
                    return byUsername;
                }
            }

            var normalizedDisplayName = Normalize(sessionDisplayName);
            if (!string.IsNullOrWhiteSpace(normalizedDisplayName))
            {
                var byDisplayName = await _context.Users
                    .AsNoTracking()
                    .Where(u => (u.FirstName + " " + u.LastName).ToLower() == normalizedDisplayName)
                    .Select(u => (Guid?)u.UserId)
                    .FirstOrDefaultAsync();

                if (byDisplayName != null)
                {
                    return byDisplayName;
                }
            }

            return null;
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
