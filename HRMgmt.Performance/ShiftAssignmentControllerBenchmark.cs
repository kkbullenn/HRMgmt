using BenchmarkDotNet.Attributes;
using HRMgmt.Controllers;
using HRMgmt.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HRMgmt.Performance
{
    [MemoryDiagnoser]
    public class ShiftAssignmentControllerBenchmark
    {
        private OrgDbContext _context;
        private ShiftAssignmentController _controller;

        [GlobalSetup]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<OrgDbContext>()
                .UseInMemoryDatabase(databaseName: "ShiftAssignPerf_" + Guid.NewGuid())
                .Options;

            _context = new OrgDbContext(options);
            _controller = new ShiftAssignmentController(_context);

            SeedData();

            // Mock Context
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
        }

        private void SeedData()
        {
            // Create Role
            var role = new Role { Id = 1, RoleName = "Employee" };
            _context.Roles.Add(role);

            // Create 5 Shifts
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

            // Create 10 Users
            var users = new List<User>();
            for(int i=0; i<10; i++)
            {
                var u = new User 
                { 
                    UserId = Guid.NewGuid(), 
                    FirstName = $"User{i}", 
                    LastName = "Test", 
                    Address = "123 Main St",
                    RoleNavigation = role
                };
                users.Add(u);
                _context.Users.Add(u);
            }

            // Create Assignments
            for(int i=0; i<50; i++)
            {
                _context.ShiftAssignments.Add(new ShiftAssignment
                {
                     ShiftId = shifts[i % 5].ShiftId,
                     UserId = users[i % 10].UserId,
                     ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(i))
                });
            }

            _context.SaveChanges();
        }

        [Benchmark]
        public async Task Index()
        {
            await _controller.Index();
        }

        [Benchmark]
        public async Task CreateAssignment()
        {
            var users = await _context.Users.ToListAsync();
            var shifts = await _context.Shifts.ToListAsync();
            
            var sa = new ShiftAssignment
            {
                UserId = users[0].UserId,
                ShiftId = shifts[0].ShiftId,
                ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddDays(100))
            };
            await _controller.Create(sa);
        }

        [Benchmark]
        public async Task EditAssignment()
        {
            var sa = await _context.ShiftAssignments.FirstAsync();
            sa.ShiftDate = sa.ShiftDate.AddDays(1);
            await _controller.Edit(sa.Id, sa);
        }

        [Benchmark]
        public async Task DeleteAssignment()
        {
            // Add a temp one
            var sa = new ShiftAssignment
            {
                UserId = _context.Users.First().UserId,
                ShiftId = _context.Shifts.First().ShiftId,
                ShiftDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1))
            };
            _context.ShiftAssignments.Add(sa);
            await _context.SaveChangesAsync();

            await _controller.DeleteConfirmed(sa.Id);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
