using HRMgmtTest.pages;
using HRMgmtTest.utils;

namespace HRMgmtTest.tests.blackbox;

/// <summary>
/// TC-002 – Assign Same Shift to Multiple Employees
/// Owner: Sam
/// Purpose: Verify that one shift can be assigned to multiple employees and generated correctly.
/// </summary>
public class TC002_AssignSameShiftToMultipleEmployees
{
    private ShiftAssignmentPage _shiftAssignmentPage;
    private LoginPage _loginPage;
    private const string TemplateName = "WK_TC002";

    // Employees: E001, E002, E003
    // Shift: D2 (09:00–17:00)
    private const string ShiftD2 = "22222222-2222-2222-2222-000000000002"; // D2 Day (09:00-17:00)

    [SetUp]
    public void Setup()
    {
        var driver = ChromeDriverFactory.CreateChromeDriver();
        _loginPage = new LoginPage(driver);
        _shiftAssignmentPage = new ShiftAssignmentPage(driver);
    }

    [Test]
    public void TC002_AssignSameShiftToMultipleEmployees_Test()
    {
        // Step 1: Login as Admin
        _loginPage.GoTo();
        _loginPage.Login("qa_test", "123456");

        // Step 2: Open Schedule Page
        _shiftAssignmentPage.GoTo();

        // Step 3: Enter template name: WK_TC002
        _shiftAssignmentPage.SetTemplateName(TemplateName);

        // Step 4: Assign shift D2 to E001, E002, E003 on Tuesday (column index 1)
        _shiftAssignmentPage.SetShiftCellValue(0, 1, ShiftD2); // E001 on Tuesday
        _shiftAssignmentPage.SetShiftCellValue(1, 1, ShiftD2); // E002 on Tuesday
        _shiftAssignmentPage.SetShiftCellValue(2, 1, ShiftD2); // E003 on Tuesday

        // Step 5: Click Save
        _shiftAssignmentPage.ClickSaveTemplate();

        // Verify save succeeded
        var successMessage = _shiftAssignmentPage.GetSuccessAlertText();
        Assert.That(successMessage, Does.Contain(TemplateName + " saved successfully.").IgnoreCase,
            "Expected success message after saving template");

        // Step 6: Click Generate Schedule
        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
        _shiftAssignmentPage.SetAssignmentStart("2026-02-17");
        _shiftAssignmentPage.SetAssignmentEnd("2026-02-19");
        _shiftAssignmentPage.ClickGenerateSchedule();

        // Verify generation succeeded
        successMessage = _shiftAssignmentPage.WaitForBrowserAlertText();
        Assert.That(successMessage, Does.Contain("generated").IgnoreCase,
            "Expected success message after generating schedule");

        // Step 7: Reload template to verify assignments
        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);

        // Verify assignments in template
        Assert.That(_shiftAssignmentPage.GetShiftCellValue(0, 1), Is.EqualTo(ShiftD2),
            "E001 should have shift D2 on Tuesday");
        Assert.That(_shiftAssignmentPage.GetShiftCellValue(1, 1), Is.EqualTo(ShiftD2),
            "E002 should have shift D2 on Tuesday");
        Assert.That(_shiftAssignmentPage.GetShiftCellValue(2, 1), Is.EqualTo(ShiftD2),
            "E003 should have shift D2 on Tuesday");

        // Verify no errors occurred
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