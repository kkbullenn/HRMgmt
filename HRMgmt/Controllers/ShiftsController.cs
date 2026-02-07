using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRMgmt.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using HRMgmt.ViewModels;
using HRMgmt;

namespace HRMgmt.Controllers
{
    public class ShiftsController : Controller
    {
        private readonly OrgDbContext _context;
        public ShiftsController(OrgDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public JsonResult AutoAssignByTemplate([FromBody] AutoAssignByTemplateDto dto)
        {
            try
            {
                Console.WriteLine($"[DEBUG] AutoAssignByTemplate called");
                if (dto == null)
                {
                    Console.WriteLine("[ERROR] dto is null");
                    return Json(new { success = false, message = "Input dto is null" });
                }
                Console.WriteLine($"[DEBUG] startDate={dto.startDate}, endDate={dto.endDate}, templateName={dto.templateName}");
                var shifts = _context?.Shifts?.ToList();
                if (shifts == null)
                {
                    Console.WriteLine("[ERROR] _context.Shifts is null");
                    return Json(new { success = false, message = "Shifts not found" });
                }
                DateTime startDate, endDate;
                if (!DateTime.TryParse(dto.startDate, out startDate))
                {
                    startDate = shifts.Count > 0 ? shifts.Min(s => s.StartDate).Date : DateTime.Today;
                }
                if (!DateTime.TryParse(dto.endDate, out endDate))
                {
                    endDate = shifts.Count > 0 ? shifts.Max(s => s.EndDate).Date : DateTime.Today;
                }
                if (endDate < startDate)
                {
                    Console.WriteLine($"[DEBUG] End date before start date");
                    return Json(new { success = false, message = "End date must be after start date." });
                }
                var days = (endDate - startDate).Days + 1;
                var employees = _context?.Employees?.ToList();
                if (employees == null)
                {
                    Console.WriteLine("[ERROR] _context.Employees is null");
                    return Json(new { success = false, message = "Employees not found" });
                }
                Console.WriteLine($"[DEBUG] templateName={dto.templateName}");
                var shiftTemplates = _context?.ShiftTemplates?.Where(t => t.TemplateName == dto.templateName).ToList();
                if (shiftTemplates == null)
                {
                    Console.WriteLine("[ERROR] _context.ShiftTemplates is null");
                    return Json(new { success = false, message = "ShiftTemplates not found" });
                }
                Console.WriteLine($"[DEBUG] shifts.Count={shifts.Count}, employees.Count={employees.Count}, shiftTemplates.Count={shiftTemplates.Count}");
                foreach (var t in shiftTemplates) {
                    Console.WriteLine($"[DEBUG] ShiftTemplate: Id={t.Id}, TemplateName={t.TemplateName}, EmployeeId={t.EmployeeId}, DayOfWeek={t.DayOfWeek}, ShiftType={t.ShiftType}, WeekType={t.WeekType}, WeekIndex={t.WeekIndex}");
                }
                var daysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
                var empIdToIndex = employees.Select((e, idx) => new { e.ID, idx }).ToDictionary(x => x.ID, x => x.idx);
                var toAdd = new List<EmployeeShift>();
                for (var d = 0; d < days; d++)
                {
                    var date = startDate.AddDays(d);
                    var weekType = ((date - startDate).Days / 7) % 2 == 0 ? 1 : 2; // 1=odd, 2=even
                    var weekIndex = ((date - startDate).Days / 7) % 2 == 0 ? 0 : 1;
                    var dayOfWeek = date.DayOfWeek.ToString();
                    Console.WriteLine($"[DEBUG] date={date:yyyy-MM-dd}, weekType={weekType}, weekIndex={weekIndex}, dayOfWeek={dayOfWeek}");
                    foreach (var shift in shifts)
                    {
                        bool needThisDay = false;
                        if (shift.RecurrenceType == Models.RecurrenceType.Daily)
                            needThisDay = date >= shift.StartDate && date <= shift.EndDate && ((date - shift.StartDate).Days % shift.Interval == 0);
                        else if (shift.RecurrenceType == Models.RecurrenceType.Weekly)
                            needThisDay = date >= shift.StartDate && date <= shift.EndDate && shift.RecurrenceDays.Split(',').Contains(dayOfWeek);
                        else if (shift.RecurrenceType == Models.RecurrenceType.BiWeekly)
                            needThisDay = date >= shift.StartDate && date <= shift.EndDate && shift.RecurrenceDays.Split(',').Contains(dayOfWeek) && (((date - shift.StartDate).Days / 7) % shift.Interval == 0);
                        else if (shift.RecurrenceType == Models.RecurrenceType.Monthly)
                            needThisDay = date >= shift.StartDate && date <= shift.EndDate && date.Day == shift.StartDate.Day;
                        if (!needThisDay) continue;
                        var assigned = shiftTemplates.Where(t => t.DayOfWeek == dayOfWeek && t.ShiftType.ToLower() == shift.Name.ToLower() && t.TemplateName == dto.templateName).ToList();
                        Console.WriteLine($"[DEBUG] {date:yyyy-MM-dd} shift={shift.Name} assigned.Count={assigned.Count} (查找条件: DayOfWeek={dayOfWeek}, ShiftType={shift.Name.ToLower()}, TemplateName={dto.templateName})");
                        foreach (var a in assigned) {
                            Console.WriteLine($"[DEBUG]  Assigned: EmployeeId={a.EmployeeId}, DayOfWeek={a.DayOfWeek}, ShiftType={a.ShiftType}");
                        }
                        int assignedCount = 0;
                        foreach (var t in assigned)
                        {
                            if (assignedCount >= shift.RequiredCount) break;
                            var key = $"{t.EmployeeId}_{shift.ID}_{date:yyyy-MM-dd}";
                            if (toAdd.Any(es => $"{es.EmployeeId}_{es.ShiftId}_{es.Date:yyyy-MM-dd}" == key))
                                continue;
                            if (_context.EmployeeShifts.Any(es => es.EmployeeId == t.EmployeeId && es.ShiftId == shift.ID && es.Date == date))
                                continue;
                            toAdd.Add(new EmployeeShift
                            {
                                EmployeeId = t.EmployeeId,
                                ShiftId = shift.ID,
                                Date = date
                            });
                            assignedCount++;
                        }
                    }
                }
                var existingKeys = new HashSet<string>(_context.EmployeeShifts.Select(es => $"{es.EmployeeId}_{es.ShiftId}_{es.Date:yyyy-MM-dd}"));
                var newShifts = toAdd.Where(es => !existingKeys.Contains($"{es.EmployeeId}_{es.ShiftId}_{es.Date:yyyy-MM-dd}")).ToList();
                Console.WriteLine($"[DEBUG] toAdd.Count={toAdd.Count}, newShifts.Count={newShifts.Count}");
                _context.EmployeeShifts.AddRange(newShifts);
                var affected = _context.SaveChanges();
                Console.WriteLine($"[DEBUG] SaveChanges affected rows: {affected}");
                return Json(new { success = true, count = newShifts.Count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] AutoAssignByTemplate Exception: {ex.Message}\n{ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class AutoAssignByTemplateDto
        {
              public string startDate { get; set; }
              public string endDate { get; set; }
            public string templateName { get; set; }
        }

        [HttpGet]
        public JsonResult GetTemplateData(string name)
        {
            var first = _context.ShiftTemplates.Where(t => t.TemplateName == name).OrderByDescending(t => t.Id).FirstOrDefault();
            if (first == null)
                return Json(new { templateName = name, weekType = 1, weekIndex = 0, grid = new List<string>() });
            var employees = _context.Employees.OrderBy(e => e.ID).ToList();
            var grid = new List<string>();
            for (int i = 0; i < employees.Count; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    var empId = employees[i].ID;
                    var dayOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }[j];
                    var cell = _context.ShiftTemplates.FirstOrDefault(t => t.TemplateName == name && t.EmployeeId == empId && t.DayOfWeek == dayOfWeek && t.WeekType == first.WeekType && t.WeekIndex == first.WeekIndex);
                    grid.Add(cell?.ShiftType ?? "");
                }
            }
            return Json(new { templateName = name, weekType = first.WeekType, weekIndex = first.WeekIndex, grid });
        }
        [HttpPost]
        public IActionResult SaveShiftTemplate([FromForm] EmployeeShiftGridViewModel model, string templateName, int weekType = 1, int weekIndex = 0)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SaveShiftTemplate called, templateName={templateName}, weekType={weekType}, weekIndex={weekIndex}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Employees.Count={model.Employees?.Count}, Grid.Count={model.Grid?.Count}");
            var old = _context.ShiftTemplates.Where(t => t.TemplateName == templateName && t.WeekType == weekType && t.WeekIndex == weekIndex);
            _context.ShiftTemplates.RemoveRange(old);
            int insertCount = 0;
            var daysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            for (int i = 0; i < (model.Employees?.Count ?? 0); i++)
            {
                var empId = model.Employees[i].ID;
                for (int j = 0; j < 7; j++)
                {
                    int gridIndex = i * 7 + j;
                    string shiftType = "";
                    if (model.Grid != null && model.Grid.Count > gridIndex && model.Grid[gridIndex] != null)
                    {
                        shiftType = model.Grid[gridIndex];
                    }
                    if (!string.IsNullOrEmpty(shiftType))
                    {
                        var dayOfWeek = daysOfWeek[j];
                        var entity = new ShiftTemplate
                        {
                            TemplateName = templateName,
                            EmployeeId = empId,
                            WeekIndex = weekIndex,
                            DayOfWeek = dayOfWeek,
                            ShiftType = shiftType
                        };
                        _context.ShiftTemplates.Add(entity);
                        insertCount++;
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Add ShiftTemplate: empId={empId}, dayOfWeek={dayOfWeek}, shiftType={shiftType}");
                    }
                }
            }
            var affected = _context.SaveChanges();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SaveChanges affected rows: {affected}, insertCount={insertCount}");
            return RedirectToAction("AssignGrid");
        }
        public IActionResult AssignGrid()
        {
            var employees = _context.Employees.ToList();
            var shifts = _context.Shifts.ToList();
            var daysOfWeek = 7;
            var grid = new List<string>();
            for (int i = 0; i < employees.Count; i++)
            {
                for (int j = 0; j < daysOfWeek; j++)
                {
                    grid.Add(""); 
                }
            }
            var vm = new EmployeeShiftGridViewModel
            {
                Employees = employees,
                Grid = grid,
                Shifts = shifts
            };
            return View(vm);
        }

        public async Task<IActionResult> AllEmployeeShifts()
        {
            var records = await _context.EmployeeShifts
                .Include(es => es.Employee)
                .Include(es => es.Shift)
                .OrderBy(es => es.Date)
                .ThenBy(es => es.ShiftId)
                .ThenBy(es => es.EmployeeId)
                .ToListAsync();
            return View(records);
        }

        [HttpPost]
        public JsonResult AutoGenerateSchedule([FromBody] AutoGenerateScheduleDto dto)
        {
            var startDate = dto.startDate;
            var days = dto.days;
            var employees = _context.Employees.OrderBy(e => e.ID).ToList();
            var shifts = _context.Shifts.OrderBy(s => s.StartTime).ToList();
            var employeeCount = employees.Count;
            var shiftCount = shifts.Count;
            Console.WriteLine($"[DEBUG] 员工数: {employeeCount}, 班次数: {shiftCount}");
            foreach (var shift in shifts)
            {
                Console.WriteLine($"[DEBUG] 班次: {shift.Name}, RequiredCount: {shift.RequiredCount}");
            }
            var queue = new Queue<Employee>(employees);
            var shiftsToAdd = new List<EmployeeShift>();

            var lastDay = _context.EmployeeShifts.OrderByDescending(es => es.Date).FirstOrDefault();
            Dictionary<Guid, string> lastShiftType = new Dictionary<Guid, string>();
            if (lastDay != null)
            {
                var lastDate = lastDay.Date;
                var lastShifts = _context.EmployeeShifts.Where(es => es.Date == lastDate).ToList();
                foreach (var es in lastShifts)
                {
                    var shift = shifts.FirstOrDefault(s => s.ID == es.ShiftId);
                    if (shift != null)
                        lastShiftType[es.EmployeeId] = shift.Name;
                }
            }

            var existingKeys = new HashSet<string>(
                _context.EmployeeShifts.Select(es => $"{es.EmployeeId}_{es.ShiftId}_{es.Date:yyyy-MM-dd}")
            );
            var assignedSet = new HashSet<string>(existingKeys);
            for (int day = 0; day < days; day++)
            {
                var today = startDate.Date.AddDays(day);
                var queueList = queue.ToList();
                queueList = queueList.OrderBy(e =>
                    lastShiftType.ContainsKey(e.ID) && lastShiftType[e.ID] == "Night" ? 1 : 0
                ).ToList();
                queue = new Queue<Employee>(queueList);

                foreach (var shift in shifts)
                {
                    for (int i = 0; i < shift.RequiredCount; i++)
                    {
                        if (queue.Count == 0) queue = new Queue<Employee>(employees);
                        if (queue.Count == 0)
                        {
                            break;
                        }
                        var emp = queue.Dequeue();
                        string key = $"{emp.ID}_{shift.ID}_{today:yyyy-MM-dd}";
                        if (assignedSet.Contains(key))
                        {
                            queue.Enqueue(emp);
                            continue;
                        }
                        shiftsToAdd.Add(new EmployeeShift
                        {
                            EmployeeId = emp.ID,
                            ShiftId = shift.ID,
                            Date = today
                        });
                        assignedSet.Add(key);
                        lastShiftType[emp.ID] = shift.Name;
                        queue.Enqueue(emp);
                    }
                }
            }
            var distinctShifts = shiftsToAdd
                .GroupBy(es => new { es.EmployeeId, es.ShiftId, es.Date })
                .Select(g => g.First())
                .ToList();
            _context.ChangeTracker.Clear();
            _context.EmployeeShifts.AddRange(distinctShifts);
            _context.SaveChanges();
            return Json(new { success = true, count = distinctShifts.Count });
        }

        public class AutoGenerateScheduleDto
        {
            public DateTime startDate { get; set; }
            public int days { get; set; }
        }


        public class BiWeeklyScheduleDto
        {
            public Guid EmployeeId { get; set; }
            public Guid TemplateId { get; set; }
            public DateTime StartDate { get; set; }
            public int Weeks { get; set; }
        }
        // Delete EmployeeShift
        [HttpPost]
        public JsonResult DeleteEmployeeShift([FromBody] DeleteEmployeeShiftDto dto)
        {
            var es = _context.EmployeeShifts
                .FirstOrDefault(e => e.EmployeeId == dto.EmployeeId && e.ShiftId == dto.ShiftId && e.Date == dto.Date);
            if (es == null)
                return Json(new { success = false, message = "Not found" });
            _context.EmployeeShifts.Remove(es);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        public class DeleteEmployeeShiftDto
        {
            public Guid EmployeeId { get; set; }
            public Guid ShiftId { get; set; }
            public DateTime Date { get; set; }
        }
        [HttpGet]
        public JsonResult GetEmployees()
        {
            var employees = _context.Employees.Select(e => new
            {
                id = e.ID,
                name = e.Name
            }).ToList();
            return Json(employees);
        }

        [HttpGet]
        public JsonResult GetEmployeeShifts(Guid employeeId)
        {
            var shifts = _context.EmployeeShifts
                .Where(es => es.EmployeeId == employeeId)
                .Include(es => es.Shift)
                .Select(es => new
                {
                    id = es.ShiftId,
                    title = es.Shift.Name + " " + es.Shift.StartTime.ToString(@"hh\:mm") + "-" + es.Shift.EndTime.ToString(@"hh\:mm") + " " + es.Shift.Location,
                    start = es.Date.ToString("yyyy-MM-dd"),
                    end = es.Date.ToString("yyyy-MM-dd")
                }).ToList();
            return Json(shifts);
        }
        // GET: Shifts/EmployeeShift
        public IActionResult EmployeeShift()
        {
            return View();
        }




        [HttpGet]
        public JsonResult GetShifts()
        {
            var shifts = _context.Shifts.Select(s => new
            {
                id = s.ID,
                title = s.Name,
                start = s.StartTime.ToString(@"hh\:mm"),
                end = s.EndTime.ToString(@"hh\:mm"),
                location = s.Location
            }).ToList();
            return Json(shifts);
        }

        [HttpPost]
        public JsonResult UpdateShiftTime([FromBody] ShiftTimeUpdateDto dto)
        {
            var shift = _context.Shifts.Find(dto.id);
            if (shift == null) return Json(new { success = false, message = "未找到班次" });
            shift.StartTime = TimeSpan.Parse(dto.start);
            shift.EndTime = TimeSpan.Parse(dto.end ?? dto.start);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult AssignShift([FromBody] AssignShiftDto dto)
        {
            try
            {
                var exists = _context.EmployeeShifts.Any(es => es.EmployeeId == dto.EmployeeId && es.Date == dto.Date);
                if (exists)
                {
                    return Json(new { success = false, message = "The employee already has a shift for this day." });
                }
                var es = new EmployeeShift
                {
                    EmployeeId = dto.EmployeeId,
                    ShiftId = dto.ShiftId,
                    Date = dto.Date
                };
                _context.EmployeeShifts.Add(es);
                int result = _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class AssignShiftDto
        {
            public Guid EmployeeId { get; set; }
            public Guid ShiftId { get; set; }
            public DateTime Date { get; set; }
        }
        [HttpPost]
        public JsonResult CreateFromCalendar([FromBody] ShiftCreateDto dto)
        {
            try
            {
                var shift = new Shift
                {
                    ID = Guid.NewGuid(),
                    Name = dto.name,
                    StartTime = TimeSpan.Parse(dto.start),
                    EndTime = TimeSpan.Parse(dto.end ?? dto.start),
                    Location = ""
                };
                _context.Shifts.Add(shift);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class ShiftTimeUpdateDto
        {
            public Guid id { get; set; }
            public string start { get; set; } // "08:00"
            public string? end { get; set; }  // "16:00"
        }
        public class ShiftCreateDto
        {
            public string name { get; set; }
            public string start { get; set; } // "08:00"
            public string? end { get; set; }  // "16:00"
        }

        // GET: Shifts
        public async Task<IActionResult> Index()
        {
            var shifts = await _context.Shifts.Include(s => s.EmployeeShifts).ToListAsync();
            return View(shifts);
        }

        // GET: Shifts/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();
            var shift = await _context.Shifts
                .Include(s => s.EmployeeShifts)
                .ThenInclude(es => es.Employee)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (shift == null) return NotFound();
            return View(shift);
        }

        // GET: Shifts/Create
        public IActionResult Create()
        {
            var model = new Shift
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            };
            return View(model);
        }

        // POST: Shifts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,StartTime,EndTime,Location,RequiredCount,StartDate,EndDate,RecurrenceType,RecurrenceDays,Interval")] Shift shift)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    shift.ID = Guid.NewGuid();
                    _context.Add(shift);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    ViewBag.DebugError = "ModelState Invalid: " + errors;
                }
            }
            catch (Exception ex)
            {
                ViewBag.DebugError = "Exception: " + ex.Message + (ex.InnerException != null ? (" | Inner: " + ex.InnerException.Message) : "");
            }
            return View(shift);
        }

        // GET: Shifts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var shift = await _context.Shifts.FindAsync(id);
            if (shift == null) return NotFound();
            return View(shift);
        }

        // POST: Shifts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("ID,Name,StartTime,EndTime,Location,RequiredCount,StartDate,EndDate,RecurrenceType,RecurrenceDays,Interval")] Shift shift)
        {
            if (id != shift.ID) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shift);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Shifts.Any(e => e.ID == shift.ID))
                        return NotFound();
                    else
                        throw;
                }
            }
            return View(shift);
        }

        // GET: Shifts/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            var shift = await _context.Shifts.FirstOrDefaultAsync(m => m.ID == id);
            if (shift == null) return NotFound();
            return View(shift);
        }

        // POST: Shifts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var shift = await _context.Shifts.FindAsync(id);
            if (shift != null)
            {
                _context.Shifts.Remove(shift);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
