using BenchmarkDotNet.Attributes;
using HRMgmt.Controllers;
using HRMgmt.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HRMgmt.Performance
{
    [MemoryDiagnoser]
    public class UsersBenchmark
    {
        private OrgDbContext _context;
        private UsersController _controller;

        [Params(10, 50)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<OrgDbContext>()
                .UseInMemoryDatabase(databaseName: "UsersPerf_" + Guid.NewGuid()) 
                .Options;

            _context = new OrgDbContext(options);
            _controller = new UsersController(_context);

            SeedData();

            // Mock Context
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "1"), // Admin ID
                        new Claim(ClaimTypes.Name, "admin"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "mock"))
                }
            };
        }

        private void SeedData()
        {
            // Create Role
            var role = new Role { Id = 1, RoleName = "Admin" };
            _context.Roles.Add(role);
            var empRole = new Role { Id = 2, RoleName = "Employee" };
            _context.Roles.Add(empRole);

            // Create Admin Account
            var account = new Account
            {
                Id = 1,
                Username = "admin",
                Role = "Admin",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };
            _context.Account.Add(account);
            
            // Create existing users
            for(int i=0; i<N; i++)
            {
                _context.Users.Add(new User 
                { 
                    UserId = Guid.NewGuid(), 
                    FirstName = $"User{i}", 
                    LastName = "Test", 
                    Address = "Test Address",
                    RoleNavigation = empRole 
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
        public async Task Details()
        {
            var u = await _context.Users.FirstAsync();
            await _controller.Details(u.UserId);
        }

        [Benchmark]
        public async Task CreateUser()
        {
            var user = new User
            {
                FirstName = "New",
                LastName = "User",
                Address = "New Address",
                RoleId = 2,
                HourlyWage = 15
            };
            // Mock IFormFile as null
            await _controller.Create(user, $"newuser_{Guid.NewGuid()}", "password123", null);
        }

        [Benchmark]
        public async Task EditUser()
        {
            var u = await _context.Users.FirstAsync();
            u.HourlyWage += 1;
            await _controller.Edit(u.UserId, u, null);
        }

        [Benchmark]
        public async Task DeleteUser()
        {
            // Create one to delete
            var u = new User { UserId=Guid.NewGuid(), FirstName="Del", LastName="Me", Address="Del Address", RoleId=2 };
            _context.Users.Add(u);
            _context.SaveChanges();

            await _controller.DeleteConfirmed(u.UserId);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
