using HRMgmtTest.pages;
using HRMgmtTest.utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Text.Json;

namespace HRMgmtTest.tests.blackbox;

/// <summary>
/// TC-008 - Prevent Duplicate Assignment on Re-Generation
/// Owner: Sam
/// Purpose: Verify generating schedule multiple times does not create duplicate assignments.
/// </summary>
public class TC008_PreventDuplicateAssignment
{
    private IWebDriver _driver = null!;
    private ShiftAssignmentPage _shiftAssignmentPage = null!;
    private EmployeeShiftPage _employeeShiftPage = null!;
    private LoginPage _loginPage = null!;

    private const string TemplateName = "WK_TC008";

    [SetUp]
    public void Setup()
    {
        _driver = ChromeDriverFactory.CreateChromeDriver();
        _shiftAssignmentPage = new ShiftAssignmentPage(_driver);
        _loginPage = new LoginPage(_driver);
        _employeeShiftPage = new EmployeeShiftPage(_driver);
    }

    [Test]
    public void TC008_PreventDuplicateAssignment_Test()
    {
        _loginPage.GoTo();
        _loginPage.Login("qa_test", "123456");

        _shiftAssignmentPage.GoTo();
        _shiftAssignmentPage.SetTemplateName(TemplateName);

        const int rowIndex = 0;
        const int mondayColumn = 0;
        var employeeId = GetEmployeeIdByRow(rowIndex);
        var shiftValue = GetFirstNonEmptyShiftValue(rowIndex, mondayColumn);

        _shiftAssignmentPage.SetShiftCellValue(rowIndex, mondayColumn, shiftValue);

        _shiftAssignmentPage.ClickSaveTemplate();
        var successMessage = _shiftAssignmentPage.GetSuccessAlertText();
        Assert.That(successMessage, Does.Contain(TemplateName + " saved successfully.").IgnoreCase,
            "Expected success message after saving template");

        var targetDate = "2026-02-16";
        var beforeCount = CountEventsOnDate(employeeId, targetDate);

        _shiftAssignmentPage.GoTo();
        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
        _shiftAssignmentPage.SetAssignmentStart("2026-02-16");
        _shiftAssignmentPage.SetAssignmentEnd("2026-02-23");
        _shiftAssignmentPage.ClickGenerateSchedule();

        successMessage = _shiftAssignmentPage.WaitForBrowserAlertText();
        Assert.That(successMessage, Does.Not.Contain("error").IgnoreCase,
            "Generate should not produce an error alert");

        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
        Assert.That(_shiftAssignmentPage.GetShiftCellValue(rowIndex, mondayColumn), Is.EqualTo(shiftValue),
            "Selected shift should persist in template grid");

        var afterFirstGenerateCount = CountEventsOnDate(employeeId, targetDate);
        Assert.That(afterFirstGenerateCount, Is.GreaterThanOrEqualTo(beforeCount),
            "First generation should not reduce existing assignments");

        _shiftAssignmentPage.GoTo();
        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
        _shiftAssignmentPage.SetAssignmentStart("2026-02-16");
        _shiftAssignmentPage.SetAssignmentEnd("2026-02-23");
        _shiftAssignmentPage.ClickGenerateSchedule();

        successMessage = _shiftAssignmentPage.WaitForBrowserAlertText();
        Assert.That(successMessage, Does.Not.Contain("error").IgnoreCase,
            "Second generate should not produce an error alert");

        var afterSecondGenerateCount = CountEventsOnDate(employeeId, targetDate);
        Assert.That(afterSecondGenerateCount, Is.EqualTo(afterFirstGenerateCount),
            "Second generation should not create duplicate assignments on the same date");

        var errorMessage = _shiftAssignmentPage.GetErrorAlertText();
        Assert.That(errorMessage, Is.Empty, "No error messages should be displayed");
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            _shiftAssignmentPage.GoTo();
            _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
            _shiftAssignmentPage.ClickDeleteTemplate();
        }
        catch
        {
            // ignore cleanup failures
        }

        if (_driver != null)
        {
            _driver.Quit();
            _driver.Dispose();
        }
    }

    private string GetEmployeeIdByRow(int rowIndex)
    {
        return _driver.FindElement(By.CssSelector($"input[name='Users[{rowIndex}].UserId']"))
                   .GetAttribute("value")
               ?? string.Empty;
    }

    private string GetFirstNonEmptyShiftValue(int rowIndex, int colIndex)
    {
        var select = new SelectElement(_driver.FindElement(
            By.CssSelector($".shift-dropdown[data-row='{rowIndex}'][data-col='{colIndex}']")));
        var opt = select.Options.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value")));
        Assert.That(opt, Is.Not.Null, $"No shift options found for row={rowIndex}, col={colIndex}");
        return opt!.GetAttribute("value") ?? string.Empty;
    }

    private int CountEventsOnDate(string employeeId, string targetDate)
    {
        _driver.Navigate().GoToUrl($"http://localhost:5175/Shift/GetEmployeeShifts?employeeId={employeeId}");
        var payload = _driver.FindElement(By.TagName("body")).Text;
        var events = JsonSerializer.Deserialize<List<EmployeeShiftEvent>>(payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<EmployeeShiftEvent>();

        return events.Count(e =>
            DateOnly.TryParse(e.Start, out var d) &&
            d.ToString("yyyy-MM-dd") == targetDate);
    }

    private sealed class EmployeeShiftEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Start { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
    }
}
