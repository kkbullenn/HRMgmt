using BenchmarkDotNet.Attributes;
using HRMgmt.Controllers;
using HRMgmt.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HRMgmt.Performance
{
    [MemoryDiagnoser]
    public class AccountControllerBenchmark
    {
        private OrgDbContext _context;
        private AccountController _controller;
        private string _password = "password123";
        private string _hashedPassword;
        private Mock<IAuthenticationService> _authServiceMock;
        private Mock<ISession> _sessionMock; // Mock ISession

        [GlobalSetup]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<OrgDbContext>()
                .UseInMemoryDatabase(databaseName: "AccountPerf_" + Guid.NewGuid()) 
                .Options;

            _context = new OrgDbContext(options);
            _controller = new AccountController(_context);

            _hashedPassword = BCrypt.Net.BCrypt.HashPassword(_password);

            // Seed Roles
            var adminRole = new Role { Id = 1, RoleName = "Admin" };
            var empRole = new Role { Id = 2, RoleName = "Employee" };
            _context.Roles.Add(adminRole);
            _context.Roles.Add(empRole);
            
            // Seed 10 existing accounts for Index/Edit/Delete
            for(int i=0; i<10; i++)
            {
                _context.Account.Add(new Account
                {
                    Username = $"existing_{i}",
                    PasswordHash = _hashedPassword,
                    Role = "Employee",
                    DisplayName = "Existing User"
                });
            }
            _context.SaveChanges();

            // Mock Auth Service for Login/Logout
            _authServiceMock = new Mock<IAuthenticationService>();
            _authServiceMock
                .Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);
            _authServiceMock
                .Setup(x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // Mock Session
            _sessionMock = new Mock<ISession>();
            // Setup SetString to do nothing or store in dictionary if we really cared, but for benchmark ignoring is fine or basic setup
            // The controller calls SetString.
            // Setup: void Set(string key, byte[] value);
            // Extension methods like SetString call Set.
            _sessionMock.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()));
            _sessionMock.Setup(s => s.Remove(It.IsAny<string>()));
            _sessionMock.Setup(s => s.Clear());


            // Mock UrlHelper and TempData Factories
            var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
            var tempDataFactoryMock = new Mock<ITempDataDictionaryFactory>();

            // Setup UrlHelperFactory to return a mock IUrlHelper
            // (Minimal implementation if needed, otherwise rely on null check or simple mock)
            urlHelperFactoryMock.Setup(x => x.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);

             // Setup TempDataDictionaryFactory to return a mock ITempDataDictionary
            tempDataFactoryMock.Setup(x => x.GetTempData(It.IsAny<HttpContext>())).Returns(new Mock<ITempDataDictionary>().Object);


            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(_authServiceMock.Object);
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IUrlHelperFactory)))
                .Returns(urlHelperFactoryMock.Object);
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(ITempDataDictionaryFactory)))
                .Returns(tempDataFactoryMock.Object);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = serviceProviderMock.Object,
                    Session = _sessionMock.Object, // Assign the mocked session
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, "AdminUser"),
                        new Claim(ClaimTypes.Role, "Admin")
                    }, "mock"))
                }
            };
        }

        [Benchmark]
        public async Task Login()
        {
            // Login with one of the seeded users
            await _controller.Login("existing_0", _password);
        }

        [Benchmark]
        public async Task Index()
        {
            await _controller.Index();
        }

        [Benchmark]
        public async Task CreateAccount()
        {
            await _controller.Create($"new_{Guid.NewGuid()}", _password, "Employee", "New User");
        }

        [Benchmark]
        public async Task EditAccount()
        {
            _context.ChangeTracker.Clear();
            // Edit the first one repeatedly (in memory db operations are fast enough/isolated per iter usually if we reset, 
            // but here we just update same record which is fine for measuring update/save cost)
            var acc = await _context.Account.AsNoTracking().FirstAsync();
            var updatedAcc = new Account
            {
                Id = acc.Id,
                Username = acc.Username,
                PasswordHash = acc.PasswordHash,
                Role = acc.Role,
                DisplayName = "Updated Name " + Guid.NewGuid(),
                CreatedAt = acc.CreatedAt
            };
            await _controller.Edit(acc.Id, updatedAcc);
        }

        [Benchmark]
        public async Task DeleteAccount()
        {
            // We need to create one to delete so we don't run out
            var acc = new Account { Username=$"del_{Guid.NewGuid()}", PasswordHash=_hashedPassword, Role="Employee" };
            _context.Account.Add(acc);
            await _context.SaveChangesAsync();

            await _controller.DeleteConfirmed(acc.Id);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
