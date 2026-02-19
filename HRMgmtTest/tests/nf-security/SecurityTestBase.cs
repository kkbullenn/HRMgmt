using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest;

public abstract class SecurityTestBase
{
    protected IWebDriver Driver = null!;
    protected WebDriverWait Wait = null!;

    protected string BaseUrl =>
        (Environment.GetEnvironmentVariable("HRMGT_BASE_URL") ?? "http://localhost:5175").TrimEnd('/');

    [SetUp]
    public void SetUp()
    {
        EnsureAppReachableOrIgnore();

        var options = new ChromeOptions();
        if (string.Equals(Environment.GetEnvironmentVariable("HRMGT_HEADLESS"), "1", StringComparison.Ordinal))
        {
            options.AddArgument("--headless=new");
        }
        options.AddArgument("--window-size=1600,1000");

        Driver = new ChromeDriver(options);
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
    }

    [TearDown]
    public void TearDown()
    {
        if (Driver != null)
        {
            Driver.Quit();
            Driver.Dispose();
        }
    }

    protected void LoginAsAdmin()
    {
        var (user, pass) = ResolveCredentials("ADMIN", "tommy", "123456");
        Login(user, pass);
    }

    protected void LoginAsEmployee()
    {
        var (user, pass) = ResolveCredentials("EMPLOYEE", "JerryEmployee", "jerry123");
        Login(user, pass);
    }

    protected void LoginAsHr()
    {
        var (user, pass) = ResolveCredentials("HR", "JerryHR", "jerry123");
        Login(user, pass);
    }

    protected void LoginAsManager()
    {
        var (user, pass) = ResolveCredentials("MANAGER", "JerryManager", "jerry123");
        Login(user, pass);
    }

    protected void Login(string username, string password)
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");

        var userInput = Wait.Until(d => d.FindElement(By.Id("username")));
        var passInput = Wait.Until(d => d.FindElement(By.Id("password")));
        var submit = Wait.Until(d => d.FindElement(By.CssSelector("button[type='submit']")));

        userInput.Clear();
        userInput.SendKeys(username);
        passInput.Clear();
        passInput.SendKeys(password);
        submit.Click();

        Wait.Until(_ => !IsLoginPage());
    }

    protected (string Username, string Password) ResolveCredentials(string roleKey, string defaultUsername, string defaultPassword)
    {
        var username = Environment.GetEnvironmentVariable($"HRMGT_{roleKey}_USER");
        var password = Environment.GetEnvironmentVariable($"HRMGT_{roleKey}_PASS");

        username = string.IsNullOrWhiteSpace(username) ? defaultUsername : username;
        password = string.IsNullOrWhiteSpace(password) ? defaultPassword : password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Assert.Ignore($"Missing credentials for {roleKey}.");
        }

        return (username!, password!);
    }

    protected void LogoutIfLoggedIn()
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Logout");
    }

    protected AccessResult CheckRouteAccess(string route)
    {
        var full = route.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? route
            : $"{BaseUrl}{(route.StartsWith('/') ? route : "/" + route)}";

        Driver.Navigate().GoToUrl(full);
        Thread.Sleep(300);

        var current = Driver.Url;
        if (current.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            return AccessResult.RedirectedToLogin;
        }

        if (current.Contains("/Home/Error", StringComparison.OrdinalIgnoreCase))
        {
            return AccessResult.AccessDenied;
        }

        return AccessResult.Allowed;
    }

    protected bool IsLoginPage()
    {
        return Driver.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase)
               || Driver.FindElements(By.Id("username")).Count > 0;
    }

    private void EnsureAppReachableOrIgnore()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = client.GetAsync($"{BaseUrl}/Account/Login").GetAwaiter().GetResult();
            _ = response.StatusCode;
        }
        catch
        {
            Assert.Ignore($"App is not reachable at {BaseUrl}. Start HRMgmt first or set HRMGT_BASE_URL.");
        }
    }
}

public enum AccessResult
{
    Allowed,
    RedirectedToLogin,
    AccessDenied
}
