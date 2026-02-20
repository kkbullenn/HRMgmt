namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC027_LogoutInvalidatesSessionTests : SecurityTestBase
{
    [Test]
    public void TC027_Logout_RevokesAccessToProtectedRoutes()
    {
        LoginAsAdmin();
        Assert.That(CheckRouteAccess("/Shift/ShiftAssignment"), Is.EqualTo(AccessResult.Allowed),
            "Precondition failed: expected protected route access while logged in.");

        LogoutIfLoggedIn();

        Assert.That(CheckRouteAccess("/Shift/ShiftAssignment"), Is.EqualTo(AccessResult.RedirectedToLogin),
            "Expected protected route to redirect to login after logout.");
    }
}
