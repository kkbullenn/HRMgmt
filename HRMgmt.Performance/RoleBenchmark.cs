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
    public class RoleBenchmark
    {
        private OrgDbContext _context;
        private RoleController _controller;

        [GlobalSetup]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<OrgDbContext>()
                .UseInMemoryDatabase(databaseName: "RolePerf_" + Guid.NewGuid())
                .Options;

            _context = new OrgDbContext(options);
            _controller = new RoleController(_context);

            // Seed Roles
            for (int i = 0; i < 50; i++)
            {
                _context.Roles.Add(new Role { RoleName = $"Role {i}" });
            }
            _context.SaveChanges();

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

        [Benchmark]
        public async Task Index()
        {
            await _controller.Index();
        }

        [Benchmark]
        public async Task CreateRole()
        {
            var role = new Role { RoleName = "New Benchmark Role " + Guid.NewGuid() };
            await _controller.Create(role);
        }

        [Benchmark]
        public async Task EditRole()
        {
            var role = await _context.Roles.FirstAsync();
            role.RoleName = "Updated " + Guid.NewGuid();
            await _controller.Edit(role.Id, role);
        }

        [Benchmark]
        public async Task DeleteRole()
        {
            // Create a disposable role
            var role = new Role { RoleName = "DeleteMe " + Guid.NewGuid() };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            await _controller.DeleteConfirmed(role.Id);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
