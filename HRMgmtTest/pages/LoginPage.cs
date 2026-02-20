using OpenQA.Selenium;

namespace HRMgmtTest.pages;

public class LoginPage : BasePage
{
    private const string PagePath = "/Account/Login";

    public LoginPage(IWebDriver driver) : base(driver)
    {
    }

    public void GoTo(string? baseUrl = null)
    {
        var trimmedBase = (baseUrl ?? BaseUrl).TrimEnd('/');
        _driver.Navigate().GoToUrl($"{trimmedBase}{PagePath}");
        WaitForPage();
    }

    public void WaitForPage()
    {
        _wait.Until(d =>
        {
            var username = d.FindElement(By.Id("username"));
            var password = d.FindElement(By.Id("password"));
            return username.Displayed && password.Displayed;
        });
    }

    public void Login(string username, string password)
    {
        _wait.Until(d =>
        {
            var usernameInput = d.FindElement(By.Id("username"));
            var passwordInput = d.FindElement(By.Id("password"));
            var loginButton = d.FindElement(By.CssSelector("button[type='submit']"));
            return usernameInput.Displayed && usernameInput.Enabled
                                           && passwordInput.Displayed && passwordInput.Enabled
                                           && loginButton.Displayed && loginButton.Enabled;
        });

        var usernameInput = _driver.FindElement(By.Id("username"));
        var passwordInput = _driver.FindElement(By.Id("password"));
        var loginButton = _driver.FindElement(By.CssSelector("button[type='submit']"));

        usernameInput.Clear();
        usernameInput.SendKeys(username);
        passwordInput.Clear();
        passwordInput.SendKeys(password);

        ClickElement(loginButton);

        // Wait for successful login redirect by checking for "Welcome" text
        _wait.Until(d => d.PageSource.Contains("Welcome"));
    }

    private void DismissPopups()
    {
        try
        {
            // Try to click OK button on any visible dialog
            var okButton = _driver.FindElement(By.XPath("//button[contains(text(), 'OK') or contains(text(), 'ok')]"));
            okButton.Click();
            System.Threading.Thread.Sleep(300);
        }
        catch
        {
            // Button not found
        }

        try
        {
            // Try Escape key
            var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
            actions.SendKeys(Keys.Escape).Perform();
            System.Threading.Thread.Sleep(300);
        }
        catch
        {
            // Any error, continue
        }
    }
}