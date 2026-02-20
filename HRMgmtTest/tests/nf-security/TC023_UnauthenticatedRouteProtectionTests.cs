namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC023_UnauthenticatedRouteProtectionTests : SecurityTestBase
{
    private const string ExistingUserId = "11111111-1111-1111-1111-000000000001";
    private const string ExistingShiftId = "22222222-2222-2222-2222-000000000001";

    [Test]
    public void TC023_UnauthenticatedUser_IsRedirectedToLogin_OnProtectedRoutes()
    {
        LogoutIfLoggedIn();

        var protectedRoutes = new[]
        {
            "/Shift/ShiftAssignment",
            "/Shift/AssignGrid",
            "/Shift/Index",
            "/Shift/MyShifts",
            "/Shift/Create",
            $"/Shift/Edit/{ExistingShiftId}",
            $"/Shift/Delete/{ExistingShiftId}",
            "/Users",
            "/Users/Create",
            $"/Users/adminEdit/{ExistingUserId}",
            $"/Users/Delete/{ExistingUserId}",
            "/Payroll/Index",
            "/Payroll/Create",
            "/Payroll/AdminCalculate",
            "/Payroll/MyPayroll",
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


