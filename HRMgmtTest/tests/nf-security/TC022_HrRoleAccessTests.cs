namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC022_HrRoleAccessTests : SecurityTestBase
{
    private const string ExistingUserId = "11111111-1111-1111-1111-000000000001";
    private const string ExistingShiftId = "22222222-2222-2222-2222-000000000001";

    [Test]
    public void TC022_HR_RoleMatrixAccess()
    {
        LoginAsHr();

        // Users: HR = R/U (no C/D)
        // Shifts: HR = Read-only
        // Payroll: HR = Read-only
        var shouldAllow = new[]
        {
            "/Home/Index",
            "/Users",
            $"/Users/adminEdit/{ExistingUserId}",
            "/Shift/Index",
            "/Payroll/Index",
        };

        foreach (var route in shouldAllow)
        {
            AssertAllowedOrRouteAliasRedirect(route, "HR should be allowed by role matrix");
        }

        var shouldDeny = new[]
        {
            "/Users/Create",
            $"/Users/Delete/{ExistingUserId}",
            "/Shift/ShiftAssignment",
            "/Shift/AssignGrid",
            "/Shift/Create",
            $"/Shift/Edit/{ExistingShiftId}",
            $"/Shift/Delete/{ExistingShiftId}",
            "/Payroll/Create",
            "/Payroll/AdminCalculate",
            "/Account/Create"
        };

        foreach (var route in shouldDeny)
        {
            AssertDeniedOrRedirected(route, "HR should be denied/restricted");
        }
    }
}

