namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC029_SessionTimeoutInactivityTests : SecurityTestBase
{
    [Test]
    public void TC029_AuthSession_IsInvalidAfterCookieRemoval()
    {
        LoginAsAdmin();
        Assert.That(CheckRouteAccess("/Users"), Is.EqualTo(AccessResult.Allowed),
            "Precondition failed: expected /Users access while logged in.");

        // Simulate expired/inactive auth by removing auth cookie.
        Driver.Manage().Cookies.DeleteCookieNamed(".AspNetCore.Cookies");

        Assert.That(CheckRouteAccess("/Users"), Is.EqualTo(AccessResult.RedirectedToLogin),
            "Expected redirect to login after auth cookie is no longer valid.");
    }
}
