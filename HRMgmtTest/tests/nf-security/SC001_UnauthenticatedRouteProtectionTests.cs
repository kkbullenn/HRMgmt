namespace HRMgmtTest;

[TestFixture]
public class SC001_UnauthenticatedRouteProtectionTests : SecurityTestBase
{
    [Test]
    public void SC001_UnauthenticatedUser_IsRedirectedToLogin_OnProtectedRoutes()
    {
        LogoutIfLoggedIn();

        var protectedRoutes = new[]
        {
            "/Shift/ShiftAssignment",
            "/Shift/AssignGrid",
            "/Shift/Index",
            "/Shift/MyShifts",
            "/Users",
            "/Users/Create",
            "/Payroll/Index",
            "/Payroll/Create",
            "/Payroll/AdminCalculate",
            "/Role/Index",
            "/ShiftAssignment/Index",
            "/Account/Index",
            "/Account/Create"
        };

        foreach (var route in protectedRoutes)
        {
            Assert.That(CheckRouteAccess(route), Is.EqualTo(AccessResult.RedirectedToLogin), $"Unauthenticated access should redirect for {route}");
        }
    }
}
