namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC024_ValidLoginPerRoleTests : SecurityTestBase
{
    [Test]
    public void TC024_ValidLogin_Works_ForAllSeededRoles()
    {
        var creds = new[]
        {
            ResolveCredentials("ADMIN", "qa_test", "123456"),
            ResolveCredentials("HR", "JerryHR", "jerry123"),
            ResolveCredentials("MANAGER", "JerryManager", "jerry123"),
            ResolveCredentials("EMPLOYEE", "JerryEmployee", "jerry123")
        };

        foreach (var (username, password) in creds)
        {
            LogoutIfLoggedIn();
            Login(username, password);

            Assert.That(IsLoginPage(), Is.False, $"Expected successful login for user '{username}'.");
            Assert.That(CheckRouteAccess("/Home/Index"), Is.EqualTo(AccessResult.Allowed),
                $"Expected Home access after login for '{username}'.");
        }
    }
}
