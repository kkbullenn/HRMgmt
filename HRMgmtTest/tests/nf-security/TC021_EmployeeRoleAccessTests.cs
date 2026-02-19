namespace HRMgmtTest;

[TestFixture]
public class TC021_EmployeeRoleAccessTests : SecurityTestBase
{
    [Test]
    public void TC021_Employee_CannotAccess_AdminHrViews()
    {
        LoginAsEmployee();

        var shouldAllow = new[]
        {
            "/Home/Index",
            "/Shift/MyShifts",
            "/Users",
            "/Users/Profile"
        };

        foreach (var route in shouldAllow)
        {
            Assert.That(CheckRouteAccess(route), Is.EqualTo(AccessResult.Allowed), $"Employee should access {route}");
        }

        var shouldDeny = new[]
        {
            "/Shift/ShiftAssignment",
            "/Shift/AssignGrid",
            "/Users/Create",
            "/Payroll/Index",
            "/Payroll/Create",
            "/Payroll/AdminCalculate",
            "/Role/Index",
            "/ShiftAssignment/Index",
            "/Account/Index",
            "/Account/Create"
        };

        foreach (var route in shouldDeny)
        {
            Assert.That(CheckRouteAccess(route), Is.EqualTo(AccessResult.AccessDenied), $"Employee should be denied {route}");
        }
    }
}
