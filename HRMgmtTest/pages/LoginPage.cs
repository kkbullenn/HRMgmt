using OpenQA.Selenium;

namespace HRMgmtTest.pages;

public class LoginPage : BasePage
{
    private const string PagePath = "/Account/Login";

    private IWebElement UsernameInput => _wait.Until(d => d.FindElement(By.Id("username")));
    private IWebElement PasswordInput => _wait.Until(d => d.FindElement(By.Id("password")));
    private IWebElement LoginButton => _wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")));

    public LoginPage(IWebDriver driver) : base(driver)
    {
    }

    public void GoTo(string baseUrl, string role = null)
    {
        var trimmedBase = (baseUrl ?? string.Empty).TrimEnd('/');
        var url = $"{trimmedBase}{PagePath}";
        if (!string.IsNullOrEmpty(role))
        {
            url += $"?role={role}";
        }
        _driver.Navigate().GoToUrl(url);
    }

    public void Login(string username, string password)
    {
        UsernameInput.Clear();
        UsernameInput.SendKeys(username);
        PasswordInput.Clear();
        PasswordInput.SendKeys(password);
        ClickElement(LoginButton);
        _wait.Until(d => !d.Url.EndsWith("Login", StringComparison.OrdinalIgnoreCase) && !d.Url.Contains("Account/Login", StringComparison.OrdinalIgnoreCase));
        _wait.Until(d => d.FindElements(By.LinkText("Log out")).Count > 0);
    }
}
