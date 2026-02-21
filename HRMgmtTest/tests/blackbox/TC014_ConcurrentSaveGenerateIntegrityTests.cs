using System.Text.Json;
using HRMgmtTest.pages;
using HRMgmtTest.utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.tests.blackbox;

public class TC014_ConcurrentSaveGenerateIntegrityTests : BlackboxTestBase
{
    private ShiftAssignmentPage _shiftPageA = null!;
    private IWebDriver? _driverB;
    private ShiftAssignmentPage? _shiftPageB;
    private LoginPage? _loginPageB;
    private string? _createdTemplate;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _shiftPageA = new ShiftAssignmentPage(Driver);
        _createdTemplate = null;
    }

    [TearDown]
    public override void TearDown()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_createdTemplate))
            {
                _shiftPageA.GoTo(BaseUrl);
                _shiftPageA.SelectTemplateFromMenu(_createdTemplate);
                _shiftPageA.ClickDeleteTemplate();
            }
        }
        catch
        {
            // Best effort cleanup only.
        }

        if (_driverB != null)
        {
            try
            {
                _driverB.Quit();
            }
            catch
            {
                // Ignore teardown errors.
            }
            finally
            {
                _driverB.Dispose();
            }
        }

        base.TearDown();
    }

    [Test]
    public void TC014_ConcurrentSaveGenerate_NoDuplicateOrCorruption()
    {
        LoginAsAdminIfCredentialsExist(); // Session A

        _driverB = ChromeDriverFactory.CreateChromeDriver();
        _loginPageB = new LoginPage(_driverB);
        _shiftPageB = new ShiftAssignmentPage(_driverB);
        _loginPageB.GoTo(BaseUrl);
        _loginPageB.Login("qa_test", "123456"); // Session B

        _shiftPageA.GoTo(BaseUrl);
        _shiftPageA.ClickCreateNewTemplate();

        var templateName = BuildTemplateName("ConcurrentIntegrityTemplate");
        _createdTemplate = templateName;
        _shiftPageA.SetTemplateName(templateName);

        var employeeId = Wait.Until(d => d.FindElement(By.CssSelector("input[name='Users[0].UserId']")))
            .GetAttribute("value") ?? string.Empty;
        Assert.That(string.IsNullOrWhiteSpace(employeeId), Is.False, "Missing employee id for row 0.");

        var shiftOptions = GetShiftLabelsFromA(0, 0);
        Assert.That(shiftOptions.Count, Is.GreaterThan(0), "No shift options available.");
        var shiftA = shiftOptions[0];
        var shiftB = shiftOptions.Count > 1 ? shiftOptions[1] : shiftOptions[0];

        SelectShiftByLabel(0, 0, shiftA);
        _shiftPageA.ClickSaveTemplate(waitForMenu: false);
        Assert.That(_shiftPageA.GetErrorAlertText(), Is.Empty, "Initial save by Admin A failed.");

        _shiftPageA.GoTo(BaseUrl);
        _shiftPageA.SelectTemplateFromMenu(templateName);
        SelectShiftByLabel(0, 0, shiftA);
        _shiftPageA.ClickSaveTemplate(waitForMenu: false);
        var errorA = _shiftPageA.GetErrorAlertText();
        Assert.That(errorA, Is.Empty, $"Admin A save failed: {errorA}");

        _shiftPageB!.GoTo(BaseUrl);
        _shiftPageB.SelectTemplateFromMenu(templateName);
        SelectShiftByLabelOnB(0, 0, shiftB);
        _shiftPageB.ClickSaveTemplate(waitForMenu: false);
        var errorB = _shiftPageB.GetErrorAlertText();
        Assert.That(errorB, Is.Empty, $"Admin B save failed: {errorB}");

        _shiftPageA.SetAssignmentStart("2026-02-16");
        _shiftPageA.SetAssignmentEnd("2026-02-16");
        _shiftPageA.ClickGenerateSchedule();
        var genA = _shiftPageA.WaitForBrowserAlertText();
        Assert.That(genA, Does.Not.Contain("error").IgnoreCase, $"Admin A generate failed: {genA}");

        _shiftPageB.SetAssignmentStart("2026-02-16");
        _shiftPageB.SetAssignmentEnd("2026-02-16");
        _shiftPageB.ClickGenerateSchedule();
        var genB = _shiftPageB.WaitForBrowserAlertText();
        Assert.That(genB, Does.Not.Contain("error").IgnoreCase, $"Admin B generate failed: {genB}");

        var events = FetchEmployeeEvents(employeeId);
        var sameDayCount = events.Count(e => e.Start.StartsWith("2026-02-16", StringComparison.OrdinalIgnoreCase));

        Assert.That(sameDayCount, Is.LessThanOrEqualTo(1),
            $"Expected no duplicate assignment for same employee/date. Found {sameDayCount}.");
    }

    private List<string> GetShiftLabelsFromA(int rowIndex, int colIndex)
    {
        var select = new SelectElement(Wait.Until(d =>
            d.FindElement(By.CssSelector($".shift-dropdown[data-row='{rowIndex}'][data-col='{colIndex}']"))));

        return select.Options
            .Where(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value")))
            .Select(o => o.Text.Trim())
            .ToList();
    }

    private void SelectShiftByLabelOnB(int rowIndex, int colIndex, string shiftLabel)
    {
        var waitB = new WebDriverWait(_driverB!, TimeSpan.FromSeconds(15));
        var select = new SelectElement(waitB.Until(d =>
            d.FindElement(By.CssSelector($".shift-dropdown[data-row='{rowIndex}'][data-col='{colIndex}']"))));
        select.SelectByText(shiftLabel);
    }

    private List<EmployeeShiftEvent> FetchEmployeeEvents(string employeeId)
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/Shift/GetEmployeeShifts?employeeId={employeeId}");
        var payload = Driver.FindElement(By.TagName("body")).Text;
        var events = JsonSerializer.Deserialize<List<EmployeeShiftEvent>>(payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return events ?? new List<EmployeeShiftEvent>();
    }

    private sealed class EmployeeShiftEvent
    {
        public string Start { get; set; } = string.Empty;
    }

    private static string BuildTemplateName(string prefix)
    {
        var token = new string(Guid.NewGuid().ToString("N").Where(char.IsLetter).Take(10).ToArray());
        if (string.IsNullOrWhiteSpace(token))
        {
            token = "alpha";
        }

        return $"{prefix}_{token}";
    }
}
