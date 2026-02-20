namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC021_EmployeeRoleAccessTests : SecurityTestBase
{
    private const string ExistingUserId = "11111111-1111-1111-1111-000000000001";
    private const string ExistingShiftId = "22222222-2222-2222-2222-000000000001";

    [Test]
    public void TC021_Employee_RoleMatrixAccess()
    {
        LoginAsEmployee();

        // Users: Employee = Read-own only
        // Shifts: Employee = Read-only
        // Payroll: Employee = Read-own only
        var shouldAllow = new[]
        {
            "/Home/Index",
            "/Users",
            "/Users/Profile",
            "/Shift/Index",
            "/Shift/MyShifts",
            "/Payroll/MyPayroll"
        };

        foreach (var route in shouldAllow)
        {
            AssertAllowedOrRouteAliasRedirect(route, "Employee should be allowed by role matrix");
        }

        var shouldDeny = new[]
        {
            "/Shift/ShiftAssignment",
            "/Shift/AssignGrid",
            "/Shift/Create",
            $"/Shift/Edit/{ExistingShiftId}",
            $"/Shift/Delete/{ExistingShiftId}",
            "/Users/Create",
            $"/Users/adminEdit/{ExistingUserId}",
            $"/Users/Delete/{ExistingUserId}",
            "/Payroll/Index",
            "/Payroll/Create",
            "/Payroll/AdminCalculate",
            "/Account/Create"
        };

        foreach (var route in shouldDeny)
        {
            AssertDeniedOrRedirected(route, "Employee should be denied/restricted");
        }
    }
}

