namespace HRMgmtTest;

[TestFixture]
public class TC022_HrRoleAccessTests : SecurityTestBase
{
    [Test]
    public void TC022_HR_CanAccess_HrViews_AndCannotAccess_AdminOnlyViews()
    {
        LoginAsHr();

        var shouldAllow = new[]
        {
            "/Home/Index",
            "/Shift/ShiftAssignment",
            "/Shift/AssignGrid",
            "/Shift/Index",
            "/Users",
            "/Users/Create",
            "/Payroll/Index",
            "/Payroll/Create"
        };

        foreach (var route in shouldAllow)
        {
            Assert.That(CheckRouteAccess(route), Is.EqualTo(AccessResult.Allowed), $"HR should access {route}");
        }

        var shouldDeny = new[]
        {
            "/Role/Index",
            "/ShiftAssignment/Index",
            "/Account/Index",
            "/Account/Create"
        };

        foreach (var route in shouldDeny)
        {
            Assert.That(CheckRouteAccess(route), Is.EqualTo(AccessResult.AccessDenied), $"HR should be denied {route}");
        }
    }
}
