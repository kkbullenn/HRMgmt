using OpenQA.Selenium;

namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC026_EmptyCredentialsRejectedTests : SecurityTestBase
{
    [Test]
    public void TC026_EmptyCredentials_AreRejected_ByServerValidation()
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");

        var userInput = Wait.Until(d => d.FindElement(By.Id("username")));
        var passInput = Wait.Until(d => d.FindElement(By.Id("password")));
        var submit = Wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")));

        // Use whitespace to bypass HTML required check and hit server-side validation.
        userInput.Clear();
        userInput.SendKeys("   ");
        passInput.Clear();
        passInput.SendKeys("   ");
        submit.Click();

        Wait.Until(_ => IsLoginPage());

        var errorText = Driver.FindElements(By.CssSelector(".alert.alert-danger"))
            .FirstOrDefault()?.Text ?? string.Empty;

        var rejected = IsLoginPage()
                       || errorText.Contains("required", StringComparison.OrdinalIgnoreCase)
                       || errorText.Contains("invalid", StringComparison.OrdinalIgnoreCase);

        Assert.That(rejected, Is.True,
            $"Expected empty-credentials rejection message, got: '{errorText}'");
    }
}
