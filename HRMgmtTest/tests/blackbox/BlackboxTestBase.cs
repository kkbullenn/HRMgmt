using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest;

public abstract class BlackboxTestBase
{
    protected IWebDriver Driver = null!;
    protected WebDriverWait Wait = null!;

    protected const string BaseUrl = "http://localhost:5175";

    [SetUp]
    public virtual void SetUp()
    {
        var options = new ChromeOptions();
        options.AddArgument("--window-size=1600,1000");

        Driver = new ChromeDriver(options);
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(15));
    }

    [TearDown]
    public virtual void TearDown()
    {
        Driver.Quit();
        Driver.Dispose();
    }

    protected void LoginAsAdminIfCredentialsExist()
    {
        var username = Environment.GetEnvironmentVariable("HRMGT_ADMIN_USER");
        var password = Environment.GetEnvironmentVariable("HRMGT_ADMIN_PASS");

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login?role=Admin");
        Wait.Until(d => d.FindElement(By.Id("username"))).SendKeys(username);
        Driver.FindElement(By.Id("password")).SendKeys(password);
        Driver.FindElement(By.CssSelector("button[type='submit']")).Click();

        Wait.Until(d => d.Url.Contains("/Users", StringComparison.OrdinalIgnoreCase));
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
