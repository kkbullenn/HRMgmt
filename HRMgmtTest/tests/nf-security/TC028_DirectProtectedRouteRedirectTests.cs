namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC028_DirectProtectedRouteRedirectTests : SecurityTestBase
{
    [Test]
    public void TC028_DirectAccess_WithoutLogin_RedirectsToLogin()
    {
        LogoutIfLoggedIn();

        var protectedRoutes = new[]
        {
            "/Shift/ShiftAssignment",
            "/Users",
            "/Payroll/Index",
            "/Account/Index",
            "/Role/Index" // expected gate; likely FAIL in current codebase
        };

        foreach (var route in protectedRoutes)
        {
            Assert.That(CheckRouteAccess(route), Is.EqualTo(AccessResult.RedirectedToLogin),
                $"Expected direct unauthenticated access to redirect for route: {route}");
        }
    }
}
