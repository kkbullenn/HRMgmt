namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC019_AdminRoleAccessTests : SecurityTestBase
{
    private const string ExistingUserId = "11111111-1111-1111-1111-000000000001";
    private const string ExistingShiftId = "22222222-2222-2222-2222-000000000001";

    [Test]
    public void TC019_Admin_RoleMatrixAccess()
    {
        LoginAsAdmin();

        // Users: Admin = C/R/U/D
        // Shifts: Admin = C/R/U/D
        // Payroll: Admin = Read-only
        var shouldAllow = new[]
        {
            "/Users",
            "/Users/Create",
            $"/Users/adminEdit/{ExistingUserId}",
            $"/Users/Delete/{ExistingUserId}",
            "/Shift/Index",
            "/Shift/Create",
            $"/Shift/Edit/{ExistingShiftId}",
            $"/Shift/Delete/{ExistingShiftId}",
            "/Shift/ShiftAssignment",
            "/Payroll/Index"
        };

        foreach (var route in shouldAllow)
        {
            Assert.That(CheckRouteAccess(route), Is.EqualTo(AccessResult.Allowed), $"Admin should access {route}");
        }

        var shouldDeny = new[]
        {
            "/Payroll/Create",
            "/Payroll/AdminCalculate"
        };

        foreach (var route in shouldDeny)
        {
            AssertDeniedOrRedirected(route, "Admin should be read-only for payroll by matrix");
        }
    }
}

