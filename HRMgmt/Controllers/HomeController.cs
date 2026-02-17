using System.Diagnostics;
using HRMgmt.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HRMgmt.Controllers
{
    public class HomeController(OrgDbContext context) : Controller
    {
        private readonly OrgDbContext _context = context;

        public IActionResult Index()
        {
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var message = "An error occurred while processing your request.";
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (feature?.Error != null)
            {
                var ex = feature.Error;
                if (ex.Message.Contains("doesn't exist") || ex.GetType().Name.Contains("MySql") || ex.GetType().Name.Contains("Db"))
                {
                    message = "The database is currently unavailable or not fully set up. Please try again later or contact your administrator.";
                }
                else if (HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
                {
                    message = ex.Message;
                }
            }

            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Message = message
            });
        }
    }
}
