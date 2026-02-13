using Microsoft.AspNetCore.Mvc;

namespace HRMgmt.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account/Login?role=Employee
        public IActionResult Login(string role)
        {
            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Index", "Home");
            }

            // Store login info in session
            HttpContext.Session.SetString("UserRole", role);
            HttpContext.Session.SetString("UserName", role + "User");

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
