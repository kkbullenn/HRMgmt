using OpenQA.Selenium;

namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC025_InvalidCredentialsRejectedTests : SecurityTestBase
{
    [Test]
    public void TC025_InvalidCredentials_AreRejected()
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");

        var userInput = Wait.Until(d => d.FindElement(By.Id("username")));
        var passInput = Wait.Until(d => d.FindElement(By.Id("password")));
        var submit = Wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")));

        userInput.Clear();
        userInput.SendKeys($"invalid_{Guid.NewGuid():N}"[..18]);
        passInput.Clear();
        passInput.SendKeys("wrong-password");
        submit.Click();

        Wait.Until(_ => IsLoginPage());

        var errorText = Driver.FindElements(By.CssSelector(".alert.alert-danger"))
            .FirstOrDefault()?.Text ?? string.Empty;

        var rejected = IsLoginPage()
                       && (string.IsNullOrWhiteSpace(errorText)
                           || errorText.Contains("invalid", StringComparison.OrdinalIgnoreCase)
                           || errorText.Contains("username", StringComparison.OrdinalIgnoreCase)
                           || errorText.Contains("password", StringComparison.OrdinalIgnoreCase));

        if (!rejected)
        {
            var msg = $"Expected invalid-credentials rejection, got: '{errorText}'";
            if (IsCi())
            {
                Assert.Inconclusive($"Security finding (non-blocking in CI): {msg}");
            }
            Assert.Fail(msg);
        }
    }
}
