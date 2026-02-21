using BenchmarkDotNet.Attributes;
using HRMgmt.Controllers;
using HRMgmt.Models;
using HRMgmt.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HRMgmt.Performance
{
    [MemoryDiagnoser]
    public class PayrollBenchmark
    {
        private OrgDbContext _context;
        private PayrollController _controller;
        private AdminPayrollCalcViewModel _vm;

        [Params(10, 50)] 
        public int N; // Number of users

        [GlobalSetup]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<OrgDbContext>()
                .UseInMemoryDatabase(databaseName: "PayrollPerfTest_" + Guid.NewGuid())
                .Options;

            _context = new OrgDbContext(options);
            SeedData(N);
            
            // Instantiate Real Controller
            _controller = new PayrollController(_context);

            // Mock ControllerContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "AdminUser"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "mock"))
                }
            };

            _vm = new AdminPayrollCalcViewModel
            {
                StartDate = DateTime.Today.AddDays(-30),
                EndDate = DateTime.Today
            };
        }

        private void SeedData(int userCount)
        {
            var users = new List<User>();
            var shifts = new List<Shift>();
            var rnd = new Random(42);

            // Create Shifts
            for (int i = 0; i < 5; i++)
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

            // Create Users
            for (int i = 0; i < userCount; i++)
            {
                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    FirstName = $"User{i}",
                    LastName = "Test",
                    Address = "Test Address",
                    HourlyWage = 20m + i,
                    RoleNavigation = role
                };
                users.Add(user);
                _context.Users.Add(user);
            }

            // Create Assignments
            var startDate = DateTime.Today.AddDays(-30);
            for (int d = 0; d < 30; d++)
            {
                var date = DateOnly.FromDateTime(startDate.AddDays(d));
                foreach (var user in users)
                {
                    if (rnd.NextDouble() > 0.2)
                    {
                        var shift = shifts[rnd.Next(shifts.Count)];
                        var sa = new ShiftAssignment
                        {
                            UserId = user.UserId,
                            ShiftId = shift.ShiftId,
                            ShiftDate = date
                        };
                        _context.ShiftAssignments.Add(sa);
                    }
                }
            }
            
            // Seed some existing Payroll records for Index/Edit/Delete
            for(int i=0; i<10; i++)
            {
                _context.Payrolls.Add(new Payroll
                {
                    UserId = users[i % users.Count].UserId,
                    PayPeriodStart = startDate,
                    PayPeriodEnd = startDate.AddDays(14),
                    GrossPay = 1000 + i,
                    Status = "Paid",
                    CreatedDate = DateTime.UtcNow
                });
            }

            _context.SaveChanges();
        }

        [Benchmark]
        public async Task AdminCalculate()
        {
            // Call the ACTUAL controller method (Calculation logic)
            await _controller.AdminCalculate(_vm);
        }

        [Benchmark]
        public async Task Index()
        {
            await _controller.Index();
        }

        [Benchmark]
        public async Task CreatePayroll()
        {
            var p = new Payroll
            {
                UserId = _context.Users.First().UserId,
                PayPeriodStart = DateTime.Now,
                PayPeriodEnd = DateTime.Now.AddDays(7),
                RegularHours = 40,
                GrossPay = 800,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };
            await _controller.Create(p);
        }

        [Benchmark]
        public async Task EditPayroll()
        {
            var p = await _context.Payrolls.FirstAsync();
            p.GrossPay += 1;
            await _controller.Edit(p.Id, p);
        }

        [Benchmark]
        public async Task DeletePayroll()
        {
            // Add one to delete so we don't run out
            var p = new Payroll 
            { 
                 UserId = _context.Users.First().UserId,
                 PayPeriodStart=DateTime.Now, 
                 GrossPay=500, 
                 Status="Draft" 
            };
            _context.Payrolls.Add(p);
            _context.SaveChanges();

            await _controller.DeleteConfirmed(p.Id);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
