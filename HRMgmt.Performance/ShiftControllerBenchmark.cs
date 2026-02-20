using BenchmarkDotNet.Attributes;
using HRMgmt.Controllers;
using HRMgmt.Models;
using HRMgmt.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HRMgmt.Performance
{
    [MemoryDiagnoser]
    public class ShiftControllerBenchmark
    {
        private OrgDbContext _context;
        private ShiftController _controller;
        private Guid _employeeUserId;
        private int _accountId;

        [Params(10, 50)]
        public int N; // Employee count

        [GlobalSetup]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<OrgDbContext>()
                .UseInMemoryDatabase(databaseName: "ShiftCtrlPerf_" + Guid.NewGuid()) 
                .Options;

            _context = new OrgDbContext(options);
            _controller = new ShiftController(_context);

            SeedData();

            // Mock Context for an Employee
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, _accountId.ToString()),
                        new Claim(ClaimTypes.Name, "employeeUser"),
                        new Claim(ClaimTypes.Role, "Employee")
                    }, "mock"))
                }
            };
        }

        private void SeedData()
        {
            var role = new Role { Id = 1, RoleName = "Employee" };
            _context.Roles.Add(role);

            // Create target employee account
            var account = new Account
            {
                Id = 1,
                Username = "employeeUser",
                Role = "Employee",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };
            _context.Account.Add(account);
            _accountId = account.Id;

            // Create target employee user
            var syncAddr = "AutoSynced:employeeuser";
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = "Employee",
                LastName = "User",
                Address = syncAddr,
                RoleNavigation = role
            };
            _context.Users.Add(user);
            _employeeUserId = user.UserId;

            // Create Shifts
            var shifts = new List<Shift>();
            for(int i=0; i<5; i++)
            {
                var s = new Shift 
                { 
                    ShiftId = Guid.NewGuid(), 
                    Name = $"Shift {i}", 
                    StartTime = new TimeSpan(9,0,0), 
                    EndTime = new TimeSpan(17,0,0),
                    RecurrenceDays = "Mon,Tue,Wed,Thu,Fri" 
                };
                shifts.Add(s);
                _context.Shifts.Add(s);
            }

            // Create assignments for this user
            var start = DateTime.Today.AddDays(-30);
            for(int i=0; i<30; i++)
            {
                _context.ShiftAssignments.Add(new ShiftAssignment
                {
                    UserId = _employeeUserId,
                    ShiftId = shifts[i%5].ShiftId,
                    ShiftDate = DateOnly.FromDateTime(start.AddDays(i))
                });
            }

            // Seed other users to simulate load
            for(int i=0; i<N; i++)
            {
                var otherId = Guid.NewGuid();
                _context.Users.Add(new User { UserId = otherId, FirstName=$"Other{i}", LastName="User", Address="456 Other St", RoleNavigation=role});
            }
            
            // Seed Template
            _context.SchedulingTemplates.Add(new SchedulingTemplate
            {
                TemplateName = "BenchmarkTemplate",
                UserId = _employeeUserId,
                WeekType = 1,
                WeekIndex = 0,
                DayOfWeek = "Monday",
                ShiftType = shifts[0].ShiftId.ToString()
            });

            _context.SaveChanges();
        }

        [Benchmark]
        public async Task Index()
        {
            // The Index method for shifts
             await _controller.Index();
        }

        [Benchmark]
        public async Task MyShifts()
        {
            await _controller.MyShifts(); // Returns View(List<EmployeeShiftListItemViewModel>)
        }

        [Benchmark]
        public async Task CreateShift()
        {
            var s = new Shift 
            {
                ShiftId = Guid.NewGuid(),
                Name = "New Benchmark Shift",
                StartTime = new TimeSpan(8,0,0),
                EndTime = new TimeSpan(16,0,0),
                RecurrenceDays = "Mon,Tue"
            };
            await _controller.Create(s);
        }

        [Benchmark]
        public async Task EditShift()
        {
            var s = await _context.Shifts.FirstAsync();
            s.Name = "Updated Name";
            await _controller.Edit(s.ShiftId, s);
        }

        [Benchmark]
        public async Task DeleteShift()
        {
            var s = new Shift { ShiftId=Guid.NewGuid(), Name="Delete Me" };
            _context.Shifts.Add(s);
            await _context.SaveChangesAsync();

            await _controller.DeleteConfirmed(s.ShiftId);
        }

        [Benchmark]
        public void GetTemplateData()
        {
            _controller.GetTemplateData("BenchmarkTemplate");
        }

        [Benchmark]
        public void AutoAssignByTemplate()
        {
             // Benchmark the auto-assign logic (complex)
             // Need a date range
            _controller.AutoAssignByTemplate(new ShiftController.AutoAssignByTemplateDto 
            { 
                 templateName = "BenchmarkTemplate", 
                 startDate = DateTime.Today, 
                 endDate = DateTime.Today.AddDays(7) 
            });
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
