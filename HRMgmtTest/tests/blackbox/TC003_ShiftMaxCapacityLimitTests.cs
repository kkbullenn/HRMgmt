using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.tests.blackbox;

public class TC003_ShiftMaxCapacityLimitTests : BlackboxTestBase
{
    private ShiftAssignmentPage _shiftPage = null!;
    private string? _createdTemplate;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _shiftPage = new ShiftAssignmentPage(Driver);
        _createdTemplate = null;
    }

    [TearDown]
    public override void TearDown()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_createdTemplate))
            {
                _shiftPage.GoTo(BaseUrl);
                _shiftPage.SelectTemplateFromMenu(_createdTemplate);
                _shiftPage.ClickDeleteTemplate();
            }
        }
        catch
        {
            // Best effort cleanup only.
        }

        base.TearDown();
    }

    [Test]
    public void TC003_AssignOneShiftToAllEmployees_EnforceMax10Expected()
    {
        LoginAsAdminIfCredentialsExist();

        _shiftPage.GoTo(BaseUrl);
        var rowCount = _shiftPage.GetEmployeeCount();
        if (rowCount < 11)
        {
            EnsureMinimumEmployees(11);
            _shiftPage.GoTo(BaseUrl);
            rowCount = _shiftPage.GetEmployeeCount();
        }

        _shiftPage.ClickCreateNewTemplate();
        var templateName = BuildTemplateName("CapacityLimitTemplate");
        _createdTemplate = templateName;
        _shiftPage.SetTemplateName(templateName);

        Assert.That(rowCount, Is.GreaterThan(0), "No employees visible in schedule grid.");

        const int mondayColumn = 0;
        var firstShift = GetFirstShiftOptionLocal(0, mondayColumn);
        Assert.That(string.IsNullOrWhiteSpace(firstShift.value), Is.False,
            "No non-empty shift option found for assignment.");

        var targetRows = Math.Min(rowCount, 11);
        for (var row = 0; row < targetRows; row++)
        {
            _shiftPage.SetShiftCellValue(row, mondayColumn, firstShift.value);
        }

        _shiftPage.ClickSaveTemplate(waitForMenu: false);
        var alertText = AcceptAlertAndGetTextIfPresent(5) ?? string.Empty;
        var errorText = _shiftPage.GetErrorAlertText();
        var successText = TryGetSuccessText();

        if (rowCount < 11)
        {
            Assert.Inconclusive(
                $"TC003 boundary requires 11 employees. Current dataset has {rowCount} after setup attempt. " +
                "Auto-create path could not produce enough employees in this environment.");
        }

        var blockedAtLimit =
            alertText.Contains("max", StringComparison.OrdinalIgnoreCase) ||
            alertText.Contains("limit", StringComparison.OrdinalIgnoreCase) ||
            alertText.Contains("10", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("max", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("limit", StringComparison.OrdinalIgnoreCase) ||
            errorText.Contains("10", StringComparison.OrdinalIgnoreCase);

        Assert.That(blockedAtLimit, Is.True,
            "Expected a max-capacity validation at 11th employee assignment. " +
            $"Alert='{alertText}', Error='{errorText}', Success='{successText}'.");
    }

    private (string value, string label) GetFirstShiftOptionLocal(int rowIndex, int colIndex)
    {
        var select = new SelectElement(Wait.Until(d =>
            d.FindElement(By.CssSelector($".shift-dropdown[data-row='{rowIndex}'][data-col='{colIndex}']"))));

        var option = select.Options.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value")));
        if (option == null)
        {
            return (string.Empty, string.Empty);
        }

        return (option.GetAttribute("value") ?? string.Empty, option.Text.Trim());
    }

    private string TryGetSuccessText()
    {
        try
        {
            return _shiftPage.GetSuccessAlertText();
        }
        catch
        {
            return string.Empty;
        }
    }

    private void EnsureMinimumEmployees(int minimumCount)
    {
        var currentCount = _shiftPage.GetEmployeeCount();
        if (currentCount >= minimumCount)
        {
            return;
        }

        var toCreate = minimumCount - currentCount;
        for (var i = 0; i < toCreate; i++)
        {
            TryCreateEmployeeViaUi(i + 1);
        }
    }

    private void TryCreateEmployeeViaUi(int ordinal)
    {
        try
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Users/Create");
            Wait.Until(d => d.FindElement(By.Id("FirstName")));

            var (firstName, lastName) = BuildEmployeeName(ordinal);
            var alphaToken = BuildAlphaToken(8);
            Driver.FindElement(By.Id("FirstName")).SendKeys(firstName);
            Driver.FindElement(By.Id("LastName")).SendKeys(lastName);
            SetInputByJs("DateOfBirth", "1990-01-01");
            Driver.FindElement(By.Id("Address")).SendKeys($"CapacityTest Address {alphaToken}");

            var roleSelect = new SelectElement(Driver.FindElement(By.Id("RoleId")));
            var employeeOption = roleSelect.Options.FirstOrDefault(o =>
                o.Text.Contains("Employee", StringComparison.OrdinalIgnoreCase));
            if (employeeOption == null)
            {
                return;
            }

            roleSelect.SelectByText(employeeOption.Text);

            var wage = Driver.FindElement(By.Id("HourlyWage"));
            wage.Clear();
            wage.SendKeys("20");

            Driver.FindElement(By.Id("username")).SendKeys($"capacityuser{alphaToken}");
            Driver.FindElement(By.Id("password")).SendKeys("Temp123!");

            var submit = Driver.FindElement(By.CssSelector("button[type='submit']"));
            try
            {
                submit.Click();
            }
            catch (ElementClickInterceptedException)
            {
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", submit);
            }

            var shortWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(8));
            shortWait.Until(d =>
                !d.Url.Contains("/Users/Create", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            // Ignore setup creation failures; boundary check handles insufficient employees as inconclusive.
        }
    }

    private void SetInputByJs(string id, string value)
    {
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "const el=document.getElementById(arguments[0]); if(el){ el.value=arguments[1]; el.dispatchEvent(new Event('input',{bubbles:true})); el.dispatchEvent(new Event('change',{bubbles:true})); }",
            id, value);
    }

    private static (string FirstName, string LastName) BuildEmployeeName(int ordinal)
    {
        var firstNames = new[]
        {
            "Liam", "Noah", "Oliver", "Elijah", "James", "William", "Benjamin", "Lucas", "Henry", "Alexander",
            "Emma", "Olivia", "Ava", "Sophia", "Isabella", "Mia", "Charlotte", "Amelia", "Harper", "Evelyn"
        };

        var lastNames = new[]
        {
            "Anderson", "Bennett", "Carter", "Dawson", "Edwards", "Fisher", "Garcia", "Hayes", "Irwin", "Johnson",
            "Kim", "Lopez", "Miller", "Nelson", "Owens", "Parker", "Quinn", "Reed", "Sullivan", "Turner"
        };

        var idx = Math.Abs(ordinal) % firstNames.Length;
        return (firstNames[idx], lastNames[idx]);
    }

    private static string BuildAlphaToken(int length)
    {
        var letters = new string(Guid.NewGuid().ToString("N").Where(char.IsLetter).ToArray());
        if (letters.Length < length)
        {
            letters += "alphaunique";
        }

        return letters.Substring(0, length).ToLowerInvariant();
    }

    private static string BuildTemplateName(string prefix)
    {
        var suffix = BuildAlphaToken(10);
        return $"{prefix}_{suffix}";
    }
}
