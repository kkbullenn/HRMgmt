using HRMgmtTest.pages;
using HRMgmtTest.utils;

namespace HRMgmtTest.tests.blackbox;

/// <summary>
/// TC-005 – Assign Different Shifts to Different Employees
/// Owner: Sam
/// Purpose: Verify system handles assigning different shifts to different employees simultaneously.
/// </summary>
public class TC005_AssignDifferentShifts
{
    private ShiftAssignmentPage _shiftAssignmentPage;
    private EmployeeShiftPage _employeeShiftPage;
    private LoginPage _loginPage;
    private const string TemplateName = "WK_TC004"; // Note: Test case uses WK_TC004 as template name

    private const string EmployeeId5 = "11111111-1111-1111-1111-000000000005"; // E005
    private const string EmployeeId6 = "11111111-1111-1111-1111-000000000006"; // E006
    private const string EmployeeId7 = "11111111-1111-1111-1111-000000000007"; // E007
    private const string ShiftD4 = "22222222-2222-2222-2222-000000000004"; // D4 Afternoon (12:00-17:00)
    private const string ShiftD5 = "22222222-2222-2222-2222-000000000005"; // D5 Mid-Morning (10:00-13:30)
    private const string ShiftD6 = "22222222-2222-2222-2222-000000000006"; // D6 Mid-Afternoon (13:30-19:00)

    [SetUp]
    public void Setup()
    {
        var driver = ChromeDriverFactory.CreateChromeDriver();
        _loginPage = new LoginPage(driver);
        _shiftAssignmentPage = new ShiftAssignmentPage(driver);
        _employeeShiftPage = new EmployeeShiftPage(driver);
    }

    [Test]
    public void TC005_AssignDifferentShifts_Test()
    {
        // Step 1: Login as Admin (if authentication is implemented)
        _loginPage.GoTo();
        _loginPage.Login("qa_test", "123456");

        // Step 2: Open Schedule Page
        _shiftAssignmentPage.GoTo();

        // Step 3: Enter template name: WK_TC004
        _shiftAssignmentPage.SetTemplateName(TemplateName);

        // Step 4: Assign different shifts to different employees on same day
        _shiftAssignmentPage.SetShiftCellValue(3, 0, ShiftD4); // D4 → E005
        _shiftAssignmentPage.SetShiftCellValue(4, 0, ShiftD5); // D5 → E006
        _shiftAssignmentPage.SetShiftCellValue(5, 0, ShiftD6); // D6 → E007

        // Step 5: Click Save
        _shiftAssignmentPage.ClickSaveTemplate();

        // Verify save succeeded
        var successMessage = _shiftAssignmentPage.GetSuccessAlertText();
        Assert.That(successMessage, Does.Contain(TemplateName + " saved successfully.").IgnoreCase,
            "Expected success message after saving template");

        // Step 6: Click Generate Schedule
        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
        _shiftAssignmentPage.SetAssignmentStart("2026-02-17"); // Monday of a test week
        _shiftAssignmentPage.SetAssignmentEnd("2026-02-23"); // Sunday of a test week
        _shiftAssignmentPage.ClickGenerateSchedule();

        // Verify generation succeeded
        successMessage = _shiftAssignmentPage.WaitForBrowserAlertText();
        Assert.That(successMessage, Does.Contain("generated").IgnoreCase,
            "Expected success message after generating schedule");

        // Step 7: Reload template to verify assignments
        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);

        Assert.That(_shiftAssignmentPage.GetShiftCellValue(3, 0), Is.EqualTo(ShiftD4),
            "E005 should have shift D4");
        Assert.That(_shiftAssignmentPage.GetShiftCellValue(4, 0), Is.EqualTo(ShiftD5),
            "E006 should have shift D5");
        Assert.That(_shiftAssignmentPage.GetShiftCellValue(5, 0), Is.EqualTo(ShiftD6),
            "E007 should have shift D6");

        // Step 7: Verify each employee shows correct shift in employee calendar
        string targetDate = "2026-02-23"; // Monday, Feb 23, 2026
        
        // Check E005's calendar - should have D4
        _employeeShiftPage.GoTo();
        _employeeShiftPage.SelectEmployee(EmployeeId5);
        Assert.That(_employeeShiftPage.HasShiftOnDate(targetDate), Is.True,
            "E005 should have a shift on the target date");
        var e005Shifts = _employeeShiftPage.GetShiftNamesOnDate(targetDate);
        Assert.That(e005Shifts, Has.Count.GreaterThanOrEqualTo(1),
            "E005 should have at least one shift");
        Assert.That(string.Join(" ", e005Shifts).ToLower(), Does.Contain("12:00").Or.Contain("d4").Or.Contain("afternoon"),
            $"E005 should have shift D4 (12:00-17:00), got: {string.Join(", ", e005Shifts)}");
        
        // Check E006's calendar - should have D5
        _employeeShiftPage.SelectEmployee(EmployeeId6);
        Assert.That(_employeeShiftPage.HasShiftOnDate(targetDate), Is.True,
            "E006 should have a shift on the target date");
        var e006Shifts = _employeeShiftPage.GetShiftNamesOnDate(targetDate);
        Assert.That(e006Shifts, Has.Count.GreaterThanOrEqualTo(1),
            "E006 should have at least one shift");
        Assert.That(string.Join(" ", e006Shifts).ToLower(), Does.Contain("10:00").Or.Contain("d5").Or.Contain("mid-morning"),
            $"E006 should have shift D5 (10:00-13:30), got: {string.Join(", ", e006Shifts)}");
        
        // Check E007's calendar - should have D6
        _employeeShiftPage.SelectEmployee(EmployeeId7);
        Assert.That(_employeeShiftPage.HasShiftOnDate(targetDate), Is.True,
            "E007 should have a shift on the target date");
        var e007Shifts = _employeeShiftPage.GetShiftNamesOnDate(targetDate);
        Assert.That(e007Shifts, Has.Count.GreaterThanOrEqualTo(1),
            "E007 should have at least one shift");
        Assert.That(string.Join(" ", e007Shifts).ToLower(), Does.Contain("13:30").Or.Contain("d6").Or.Contain("mid-afternoon"),
            $"E007 should have shift D6 (13:30-19:00), got: {string.Join(", ", e007Shifts)}");

        // Verify no cross-assignment errors
        var errorMessage = _shiftAssignmentPage.GetErrorAlertText();
        Assert.That(errorMessage, Is.Empty, "No error messages should be displayed");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up: Delete the test template
        try
        {
            _shiftAssignmentPage.GoTo();
            _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
            _shiftAssignmentPage.ClickDeleteTemplate();
        }
        catch (Exception)
        {
            // Template might not exist, ignore
        }

        // Close browser
        _shiftAssignmentPage.CloseBrowser();
    }
}