namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC020_ManagerRoleAccessTests : SecurityTestBase
{
    private const string ExistingUserId = "11111111-1111-1111-1111-000000000001";
    private const string ExistingShiftId = "22222222-2222-2222-2222-000000000001";

    [Test]
    public void TC020_Manager_RoleMatrixAccess()
    {
        LoginAsManager();

        Assert.That(CheckRouteAccess("/Home/Index"), Is.EqualTo(AccessResult.Allowed), "Manager should access Home");

        // Users: Manager = R/U (no C/D)
        // Shifts: Manager = C/R/U/D
        // Payroll: Manager = Read-only
        var shouldAllow = new[]
        {
            "/Users",
            $"/Users/adminEdit/{ExistingUserId}",
            "/Shift/Index",
            "/Shift/Create",
            $"/Shift/Edit/{ExistingShiftId}",
            $"/Shift/Delete/{ExistingShiftId}",
            "/Shift/ShiftAssignment",
            "/Payroll/Index",
        };

        foreach (var route in shouldAllow)
        {
            AssertAllowedOrRouteAliasRedirect(route, "Manager should be allowed by role matrix");
        }

        var shouldDeny = new[]
        {
            "/Users/Create",
            $"/Users/Delete/{ExistingUserId}",
            "/Payroll/Create",
            "/Payroll/AdminCalculate",
            "/Account/Create"
        };

        foreach (var route in shouldDeny)
        {
            AssertDeniedOrRedirected(route, "Manager should be denied/restricted");
        }
    }
}

