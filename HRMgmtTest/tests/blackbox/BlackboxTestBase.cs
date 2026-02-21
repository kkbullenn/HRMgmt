using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.tests.blackbox;

public abstract class BlackboxTestBase
{
    protected IWebDriver Driver = null!;
    protected WebDriverWait Wait = null!;

    protected const string BaseUrl = "http://localhost:5175";

    [SetUp]
    public virtual void SetUp()
    {
        var options = new ChromeOptions();
        if (string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            options.AddArgument("--headless=new");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--remote-debugging-port=9222");
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
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
    }

    [TearDown]
    public virtual void TearDown()
    {
        if (Driver == null)
        {
            return;
        }

        try
        {
            Driver.Quit();
        }
        catch
        {
            // Ignore shutdown errors in teardown.
        }
        finally
        {
            Driver.Dispose();
        }
    }

    protected void LoginAsAdminIfCredentialsExist()
    {
        var username = Environment.GetEnvironmentVariable("HRMGT_ADMIN_USER");
        var password = Environment.GetEnvironmentVariable("HRMGT_ADMIN_PASS");

        // Use project sample defaults when env vars are not provided.
        username = string.IsNullOrWhiteSpace(username) ? "qa_test" : username;
        password = string.IsNullOrWhiteSpace(password) ? "123456" : password;

        Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");
        Wait.Until(d => d.FindElement(By.Id("username"))).SendKeys(username);
        Driver.FindElement(By.Id("password")).SendKeys(password);
        Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        // Current app redirects to Home/Index after successful login.
        Wait.Until(d => !d.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase));
    }

    protected void AcceptAlertIfPresent(int timeoutSeconds = 5)
    {
        try
        {
            var shortWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
            shortWait.Until(d =>
            {
                try
                {
                    d.SwitchTo().Alert().Accept();
                    return true;
                }
                catch (NoAlertPresentException)
                {
                    return false;
                }
            });
        }
        catch (WebDriverTimeoutException)
        {
            // no alert to accept
        }
    }

    protected string? AcceptAlertAndGetTextIfPresent(int timeoutSeconds = 5)
    {
        try
        {
            var shortWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(timeoutSeconds));
            return shortWait.Until(d =>
            {
                try
                {
                    var alert = d.SwitchTo().Alert();
                    var text = alert.Text;
                    alert.Accept();
                    return text;
                }
                catch (NoAlertPresentException)
                {
                    return null;
                }
            });
        }
        catch (WebDriverTimeoutException)
        {
            return null;
        }
    }

    protected string FindRowIndexByEmployeeToken(string employeeToken)
    {
        if (string.IsNullOrWhiteSpace(employeeToken))
        {
            var firstRow = Wait.Until(d => d.FindElement(By.CssSelector("tr[data-row='0']")));
            return firstRow.GetAttribute("data-row") ?? "0";
        }

        var rows = Driver.FindElements(By.CssSelector("tr[data-row]"));
        var matched = rows.FirstOrDefault(r =>
            r.Text.Contains(employeeToken, StringComparison.OrdinalIgnoreCase));

        if (matched != null)
        {
            return matched.GetAttribute("data-row") ?? "0";
        }

        var fallback = Wait.Until(d => d.FindElement(By.CssSelector("tr[data-row='0']")));
        return fallback.GetAttribute("data-row") ?? "0";
    }

    protected (string RowIndex, string EmployeeName) GetFirstEmployeeFromGrid()
    {
        var firstRow = Wait.Until(d => d.FindElement(By.CssSelector("tr[data-row='0']")));
        var employeeName = firstRow.FindElement(By.CssSelector(".emp-name")).Text.Trim();
        var rowIndex = firstRow.GetAttribute("data-row") ?? "0";
        return (rowIndex, employeeName);
    }

    protected (string ShiftValue, string ShiftLabel, string ShiftToken) GetFirstShiftOption(int rowIndex, int colIndex)
    {
        var shiftSelect = new SelectElement(Wait.Until(d =>
            d.FindElement(By.CssSelector($".shift-dropdown[data-row='{rowIndex}'][data-col='{colIndex}']"))));

        var option = shiftSelect.Options.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value")));
        Assert.That(option, Is.Not.Null, "No shift options are available in the selected grid cell.");

        var label = option!.Text.Trim();
        var token = label.Split('(')[0].Trim();
        return (option.GetAttribute("value") ?? string.Empty, label, token);
    }

    protected void SelectShiftByLabel(int rowIndex, int colIndex, string shiftLabel)
    {
        var shiftSelect = new SelectElement(Wait.Until(d =>
            d.FindElement(By.CssSelector($".shift-dropdown[data-row='{rowIndex}'][data-col='{colIndex}']"))));
        shiftSelect.SelectByText(shiftLabel);
    }

    protected void SelectCalendarEmployee(string employeeToken)
    {
        var employeeSelect = Wait.Until(d => d.FindElement(By.Id("employeeSelect")));
        Wait.Until(d =>
        {
            try
            {
                var selectProbe = new SelectElement(d.FindElement(By.Id("employeeSelect")));
                return selectProbe.Options.Count > 0;
            }
            catch
            {
                return false;
            }
        });
        var select = new SelectElement(employeeSelect);

        if (!string.IsNullOrWhiteSpace(employeeToken))
        {
            var option = select.Options.FirstOrDefault(o =>
                o.Text.Contains(employeeToken, StringComparison.OrdinalIgnoreCase));
            if (option != null)
            {
                select.SelectByText(option.Text);
                return;
            }
        }

        Assert.That(select.Options.Count, Is.GreaterThan(0), "No employees available in calendar dropdown.");
        select.SelectByIndex(0);
    }
}
