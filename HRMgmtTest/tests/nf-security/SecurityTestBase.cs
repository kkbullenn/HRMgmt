using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.tests.nfsecurity;

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
        options.AddArgument("--no-first-run");
        options.AddArgument("--no-default-browser-check");
        options.AddArgument("--password-store=basic");
        options.AddArgument("--disable-sync");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-default-apps");
        options.AddArgument("--disable-notifications");
        options.AddArgument("--disable-save-password-bubble");
        options.AddArgument("--disable-features=PasswordLeakDetection,PasswordCheck,PasswordManager,PasswordManagerOnboarding,PasswordManagerDesktopSync,PasswordUi,SafeBrowsingEnhancedProtection");
        options.AddUserProfilePreference("credentials_enable_service", false);
        options.AddUserProfilePreference("profile.password_manager_enabled", false);
        options.AddUserProfilePreference("profile.password_manager_leak_detection", false);
        options.AddUserProfilePreference("safebrowsing.enabled", false);
        options.AddUserProfilePreference("profile.default_content_settings.notifications", 2);

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
        var (user, pass) = ResolveCredentials("ADMIN", "qa_test", "123456");
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

        var normalizedRequested = full.TrimEnd('/').ToLowerInvariant();
        var normalizedCurrent = current.TrimEnd('/').ToLowerInvariant();
        if (!normalizedCurrent.StartsWith(normalizedRequested, StringComparison.Ordinal))
        {
            return AccessResult.RedirectedElsewhere;
        }

        return AccessResult.Allowed;
    }

    protected void AssertDeniedOrRedirected(string route, string reason)
    {
        var result = CheckRouteAccess(route);
        Assert.That(result == AccessResult.AccessDenied || result == AccessResult.RedirectedElsewhere,
            Is.True, $"{reason} | route={route} | actual={result}");
    }

    protected void AssertAllowedOrRouteAliasRedirect(string route, string reason)
    {
        var result = CheckRouteAccess(route);
        Assert.That(result == AccessResult.Allowed || result == AccessResult.RedirectedElsewhere,
            Is.True, $"{reason} | route={route} | actual={result}");
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
    AccessDenied,
    RedirectedElsewhere
}

