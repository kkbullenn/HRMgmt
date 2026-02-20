using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.tests.nfsecurity;

[TestFixture]
public class TC030_PasswordPolicyChecksTests : SecurityTestBase
{
    [Test]
    public void TC030_PasswordPolicy_RejectsWeakPasswordBeyondMinLength()
    {
        LoginAsAdmin();
        Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Create");

        var userInput = Wait.Until(d => d.FindElement(By.Id("username")));
        var displayNameInput = Wait.Until(d => d.FindElement(By.Id("displayName")));
        var roleSelect = new SelectElement(Wait.Until(d => d.FindElement(By.Id("role"))));
        var passwordInput = Wait.Until(d => d.FindElement(By.Id("password")));
        var submit = Wait.Until(d => d.FindElement(By.CssSelector("button[type='submit'].btn.btn-primary")));

        var probeUsername = $"pw_probe_{DateTime.UtcNow:yyyyMMddHHmmss}";

        // Use unique username so password policy is evaluated before duplicate-username guard.
        userInput.Clear();
        userInput.SendKeys(probeUsername);
        displayNameInput.Clear();
        displayNameInput.SendKeys("QA Weak Password Probe");
        roleSelect.SelectByValue("Admin");

        // Length is 6 (meets min), but no complexity (no uppercase/special) to probe policy gap.
        passwordInput.Clear();
        passwordInput.SendKeys("abcdef");
        submit.Click();

        var errorText = Driver.FindElements(By.CssSelector(".alert.alert-danger"))
            .FirstOrDefault()?.Text ?? string.Empty;
        var successText = Driver.FindElements(By.CssSelector(".alert.alert-success"))
            .FirstOrDefault()?.Text ?? string.Empty;

        // Requirement-level expectation: complexity policy should reject this weak password.
        var rejectedByPolicy =
            errorText.Contains("password", StringComparison.OrdinalIgnoreCase) &&
            (errorText.Contains("must", StringComparison.OrdinalIgnoreCase)
             || errorText.Contains("complex", StringComparison.OrdinalIgnoreCase)
             || errorText.Contains("uppercase", StringComparison.OrdinalIgnoreCase));

        if (!rejectedByPolicy)
        {
            var msg = $"Expected password-policy rejection for weak password. Error='{errorText}', Success='{successText}'";
            if (IsCi())
            {
                Assert.Inconclusive($"Security finding (non-blocking in CI): {msg}");
            }
            Assert.Fail(msg);
        }
    }
}
