namespace HRMgmtTest;

[TestFixture]
public class TC020_ManagerRoleAccessTests : SecurityTestBase
{
    [Test]
    public void TC020_Manager_RoleAccess_IsRestrictedForAdminHrOnlyRoutes()
    {
        LoginAsManager();

        Assert.That(CheckRouteAccess("/Home/Index"), Is.EqualTo(AccessResult.Allowed), "Manager should access Home");

        var shouldDeny = new[]
        {
            "/Shift/ShiftAssignment",
            "/Shift/AssignGrid",
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

        foreach (var route in shouldDeny)
        {
            Assert.That(CheckRouteAccess(route), Is.EqualTo(AccessResult.AccessDenied), $"Manager should be denied {route}");
        }
    }
}
