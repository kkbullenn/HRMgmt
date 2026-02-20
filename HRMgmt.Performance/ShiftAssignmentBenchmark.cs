using BenchmarkDotNet.Attributes;
using HRMgmt.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HRMgmt.Performance
{
    [MemoryDiagnoser]
    public class ShiftAssignmentBenchmark
    {
        private OrgDbContext _context;
        private string _templateName = "BenchmarkTemplate";
        private DateTime _startDate;
        private DateTime _endDate;

        [Params(10, 50)]
        public int N; // Number of users

        [GlobalSetup]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<OrgDbContext>()
                .UseInMemoryDatabase(databaseName: "ShiftAssignPerf_" + Guid.NewGuid())
                .Options;

            _context = new OrgDbContext(options);
            SeedData(N);

            _startDate = DateTime.Today;
            _endDate = DateTime.Today.AddDays(30);
        }

        private void SeedData(int userCount)
        {
            // Create Shifts
            var shifts = new List<Shift>();
            for (int i = 0; i < 3; i++)
            {
                var shift = new Shift
                {
                    ShiftId = Guid.NewGuid(),
                    Name = $"Shift {i}",
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    StartDate = DateTime.Today.AddYears(-1),
                    EndDate = DateTime.Today.AddYears(1),
                    RecurrenceDays = ""
                };
                shifts.Add(shift);
                _context.Shifts.Add(shift);
            }

            // Create Role
            var role = new Role { Id = 1, RoleName = "Employee" };
            _context.Roles.Add(role);

            // Create Users & Templates
            for (int i = 0; i < userCount; i++)
            {
                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    FirstName = $"User{i}",
                    LastName = "Test",
                    Address = "Test Address",
                    HourlyWage = 20m,
                    RoleNavigation = role
                };
                _context.Users.Add(user);

                // Create a weekly template for M-F
                var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
                foreach (var day in days)
                {
                    _context.SchedulingTemplates.Add(new SchedulingTemplate
                    {
                        TemplateName = _templateName,
                        UserId = user.UserId,
                        WeekType = 1,
                        WeekIndex = 0,
                        DayOfWeek = day,
                        ShiftType = shifts[0].ShiftId.ToString()
                    });
                }
            }

            _context.SaveChanges();
        }

        [Benchmark]
        public void AutoAssign()
        {
            // Logic extracted from ShiftController.AutoAssignByTemplate
            
            var templateRows = _context.SchedulingTemplates.Where(t => t.TemplateName == _templateName).ToList();
            var shifts = _context.Shifts.ToList();
            var shiftMap = shifts.ToDictionary(s => s.ShiftId, s => s);

            var globalStartOnly = DateOnly.FromDateTime(_startDate);
            var globalEndOnly = DateOnly.FromDateTime(_endDate);

            var added = 0;
            var existingUserDateKeys = new HashSet<string>(); 
            
            foreach (var row in templateRows)
            {
                var shiftIdRaw = (row.ShiftType ?? string.Empty).Trim();
                if (!Guid.TryParse(shiftIdRaw, out var shiftId) || !shiftMap.TryGetValue(shiftId, out var shift)) continue;

                var shiftStart = DateOnly.FromDateTime(shift.StartDate);
                var shiftEnd = DateOnly.FromDateTime(shift.EndDate);
                var rangeStart = globalStartOnly; // Simplified intersection for benchmark
                var rangeEnd = globalEndOnly;
                
                if (rangeStart < shiftStart) rangeStart = shiftStart;
                if (rangeEnd > shiftEnd) rangeEnd = shiftEnd;
                if (rangeEnd < rangeStart) continue;

                for (var date = rangeStart; date <= rangeEnd; date = date.AddDays(1))
                {
                    var dayName = date.DayOfWeek.ToString();
                    bool match = false;
                    // Weekly only for this benchmark
                    if (row.WeekType == 1)
                    {
                        match = row.DayOfWeek.Equals(dayName, StringComparison.OrdinalIgnoreCase);
                    }
                    if (!match) continue;

                    var userDateKey = $"{row.UserId}_{date:yyyy-MM-dd}";
                    if (existingUserDateKeys.Contains(userDateKey)) continue;
                    
                    var exists = _context.ShiftAssignments.Any(sa => sa.UserId == row.UserId && sa.ShiftId == shift.ShiftId && sa.ShiftDate == date);
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
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
