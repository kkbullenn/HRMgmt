using HRMgmtTest.pages;
using HRMgmtTest.utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.tests.blackbox;

/// <summary>
/// TC-005 - Assign Different Shifts to Different Employees
/// Owner: Sam
/// Purpose: Verify system handles assigning different shifts to different employees simultaneously.
/// </summary>
public class TC005_AssignDifferentShifts
{
    private IWebDriver _driver = null!;
    private ShiftAssignmentPage _shiftAssignmentPage = null!;
    private EmployeeShiftPage _employeeShiftPage = null!;
    private LoginPage _loginPage = null!;
    private const string TemplateName = "WK_TC005";

    [SetUp]
    public void Setup()
    {
        _driver = ChromeDriverFactory.CreateChromeDriver();
        _loginPage = new LoginPage(_driver);
        _shiftAssignmentPage = new ShiftAssignmentPage(_driver);
        _employeeShiftPage = new EmployeeShiftPage(_driver);
    }

    [Test]
    public void TC005_AssignDifferentShifts_Test()
    {
        _loginPage.GoTo();
        _loginPage.Login("qa_test", "123456");

        _shiftAssignmentPage.GoTo();
        _shiftAssignmentPage.SetTemplateName(TemplateName);

        const int col = 0;
        var row0 = 0;
        var row1 = 1;
        var row2 = 2;

        var shift0 = GetFirstNonEmptyShiftValue(row0, col);
        var shift1 = GetDifferentShiftValue(row1, col, shift0);
        var shift2 = GetDifferentShiftValue(row2, col, shift0, shift1);

        _shiftAssignmentPage.SetShiftCellValue(row0, col, shift0);
        _shiftAssignmentPage.SetShiftCellValue(row1, col, shift1);
        _shiftAssignmentPage.SetShiftCellValue(row2, col, shift2);

        _shiftAssignmentPage.ClickSaveTemplate();
        var successMessage = _shiftAssignmentPage.GetSuccessAlertText();
        Assert.That(successMessage, Does.Contain(TemplateName + " saved successfully.").IgnoreCase,
            "Expected success message after saving template");

        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
        _shiftAssignmentPage.SetAssignmentStart("2026-02-16");
        _shiftAssignmentPage.SetAssignmentEnd("2026-02-23");
        _shiftAssignmentPage.ClickGenerateSchedule();

        successMessage = _shiftAssignmentPage.WaitForBrowserAlertText();
        Assert.That(successMessage, Does.Not.Contain("error").IgnoreCase,
            "Generate should not produce an error alert");

        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
        Assert.That(_shiftAssignmentPage.GetShiftCellValue(row0, col), Is.EqualTo(shift0));
        Assert.That(_shiftAssignmentPage.GetShiftCellValue(row1, col), Is.EqualTo(shift1));
        Assert.That(_shiftAssignmentPage.GetShiftCellValue(row2, col), Is.EqualTo(shift2));

        var emp0 = GetEmployeeIdByRow(row0);
        var emp1 = GetEmployeeIdByRow(row1);
        var emp2 = GetEmployeeIdByRow(row2);
        var targetDate = "2026-02-16";

        _employeeShiftPage.GoTo();
        _employeeShiftPage.SelectEmployee(emp0);
        Assert.That(_employeeShiftPage.HasShiftOnDate(targetDate), Is.True,
            "Row 0 employee should have a shift on target date");

        _employeeShiftPage.SelectEmployee(emp1);
        Assert.That(_employeeShiftPage.HasShiftOnDate(targetDate), Is.True,
            "Row 1 employee should have a shift on target date");

        _employeeShiftPage.SelectEmployee(emp2);
        Assert.That(_employeeShiftPage.HasShiftOnDate(targetDate), Is.True,
            "Row 2 employee should have a shift on target date");

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

    private string GetDifferentShiftValue(int rowIndex, int colIndex, params string[] excluded)
    {
        var excludedSet = new HashSet<string>(excluded.Where(e => !string.IsNullOrWhiteSpace(e)),
            StringComparer.OrdinalIgnoreCase);
        var select = new SelectElement(_driver.FindElement(
            By.CssSelector($".shift-dropdown[data-row='{rowIndex}'][data-col='{colIndex}']")));
        var opt = select.Options.FirstOrDefault(o =>
            !string.IsNullOrWhiteSpace(o.GetAttribute("value")) &&
            !excludedSet.Contains(o.GetAttribute("value") ?? string.Empty));

        if (opt == null)
        {
            return GetFirstNonEmptyShiftValue(rowIndex, colIndex);
        }

        return opt.GetAttribute("value") ?? string.Empty;
    }
}
