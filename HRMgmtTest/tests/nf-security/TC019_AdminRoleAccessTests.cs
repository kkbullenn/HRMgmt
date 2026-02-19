namespace HRMgmtTest;

[TestFixture]
public class TC019_AdminRoleAccessTests : SecurityTestBase
{
    [Test]
    public void TC019_Admin_CanAccess_AdminAndHrViews()
    {
        LoginAsAdmin();

        var shouldAllow = new[]
        {
            "/Shift/ShiftAssignment",
            "/Shift/AssignGrid",
            "/Shift/Index",
            "/Shift/EmployeeShift",
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

        foreach (var route in shouldAllow)
        {
            Assert.That(CheckRouteAccess(route), Is.EqualTo(AccessResult.Allowed), $"Admin should access {route}");
        }
    }
}
