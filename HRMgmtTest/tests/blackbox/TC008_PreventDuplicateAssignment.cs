using HRMgmtTest.pages;
using HRMgmtTest.utils;

namespace HRMgmtTest.tests.blackbox;

/// <summary>
/// TC-008 – Prevent Duplicate Assignment on Re-Generation
/// Owner: Sam
/// Purpose: Verify generating schedule multiple times does not create duplicate assignments.
/// </summary>
public class TC008_PreventDuplicateAssignment
{
    private ShiftAssignmentPage _shiftAssignmentPage;
    private EmployeeShiftPage _employeeShiftPage;
    private LoginPage _loginPage;

    private const string TemplateName = "WK_TC008";

    // Test data - Matches SQL insert statements in testData/test_employees_shifts.sql
    // Employee: E001
    // Shift: D1 (08:00–16:00)
    private const string EmployeeId1 = "11111111-1111-1111-1111-000000000001"; // E001
    private const string ShiftD1 = "22222222-2222-2222-2222-000000000001"; // D1 Morning (08:00-16:00)

    [SetUp]
    public void Setup()
    {
        var driver = ChromeDriverFactory.CreateChromeDriver();
        _shiftAssignmentPage = new ShiftAssignmentPage(driver);
        _loginPage = new LoginPage(driver);
        _employeeShiftPage = new EmployeeShiftPage(driver);
    }

    [Test]
    public void TC008_PreventDuplicateAssignment_Test()
    {
        // Step 1: Login as Admin
        _loginPage.GoTo();
        _loginPage.Login("qa_test", "123456");

        // Step 2: Open Schedule Page
        _shiftAssignmentPage.GoTo();

        // Step 3: Create template assigning D1 → E001
        _shiftAssignmentPage.SetTemplateName(TemplateName);

        // Assign shift D1 to E001 on Monday (column index 0)
        // Assuming row index for E001 is 0
        _shiftAssignmentPage.SetShiftCellValue(0, 0, ShiftD1); // E001 on Monday

        // Step 4: Click Save
        _shiftAssignmentPage.ClickSaveTemplate();

        // Verify save succeeded
        var successMessage = _shiftAssignmentPage.GetSuccessAlertText();
        Assert.That(successMessage, Does.Contain(TemplateName + " saved successfully.").IgnoreCase,
            "Expected success message after saving template");

        // Step 5: Click Generate Schedule (first time)
        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
        _shiftAssignmentPage.SetAssignmentStart("2026-02-17"); // Monday of a test week
        _shiftAssignmentPage.SetAssignmentEnd("2026-02-23"); // Sunday of a test week
        _shiftAssignmentPage.ClickGenerateSchedule();

        // Verify generation succeeded
        successMessage = _shiftAssignmentPage.WaitForBrowserAlertText();
        Assert.That(successMessage, Does.Contain("generated").IgnoreCase,
            "Expected success message after generating schedule");
     
        // Verify assignments in template
        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
        Assert.That(_shiftAssignmentPage.GetShiftCellValue(0, 0), Is.EqualTo(ShiftD1),
            "E001 should have shift D1 on Monday");

        // Verify assignment exists in employee calendar
        string targetDate = "2026-02-23";
        _employeeShiftPage.GoTo();
        _employeeShiftPage.SelectEmployee(EmployeeId1);

        Assert.That(_employeeShiftPage.HasShiftOnDate(targetDate), Is.True,
            "E001 should have shift D1 after first generation");

        var shiftsAfterFirstGen = _employeeShiftPage.CountShiftsOnDate(targetDate);
        Assert.That(shiftsAfterFirstGen, Is.EqualTo(1),
            "E001 should have exactly one shift after first generation");

        // Step 6: Click Generate Schedule again using same template/range
        _shiftAssignmentPage.GoTo();
        _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
        // Use the same date range
        _shiftAssignmentPage.SetAssignmentStart("2026-02-17"); // Monday of same test week
        _shiftAssignmentPage.SetAssignmentEnd("2026-02-23"); // Sunday of same test week
        _shiftAssignmentPage.ClickGenerateSchedule();
        
        // Verify generation succeeded
        successMessage = _shiftAssignmentPage.WaitForBrowserAlertText();
        Assert.That(successMessage, Does.Contain("generated").IgnoreCase,
            "Expected success message after generating schedule");

        // Step 7: Verify only one assignment exists for E001 on that date
        _employeeShiftPage.GoTo();
        _employeeShiftPage.SelectEmployee(EmployeeId1);

        var shiftsAfterSecondGen = _employeeShiftPage.CountShiftsOnDate(targetDate);
        Assert.That(shiftsAfterSecondGen, Is.EqualTo(1),
            "E001 should still have exactly one shift after second generation - no duplicates");

        // Verify the shift is still D1
        var shiftNames = _employeeShiftPage.GetShiftNamesOnDate(targetDate);
        Assert.That(shiftNames, Has.Count.EqualTo(1),
            "Only one shift should be assigned");
        Assert.That(string.Join(" ", shiftNames).ToLower(),
            Does.Contain("8:00").Or.Contain("d1").Or.Contain("morning"),
            $"E005 should have shift D4 (12:00-17:00), got: {string.Join(", ", shiftNames)}");

        // Verify no errors occurred
        var errorMessage = _shiftAssignmentPage.GetErrorAlertText();
        Assert.That(errorMessage, Is.Empty, "No error messages should be displayed");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up: Delete the test template and assignments
        try
        {
            // Delete template
            _shiftAssignmentPage.GoTo();
            _shiftAssignmentPage.SelectTemplateFromMenu(TemplateName);
            _shiftAssignmentPage.ClickDeleteTemplate();

            // Delete shift assignment
            string targetDate = "2026-02-17";
            _employeeShiftPage.GoTo();
            _employeeShiftPage.SelectEmployee(EmployeeId1);

            if (_employeeShiftPage.HasShiftOnDate(targetDate))
            {
                _employeeShiftPage.DeleteShiftOnDate(targetDate);
            }
        }
        catch (Exception)
        {
            // Resources might not exist, ignore
        }

        // Close browser
        _shiftAssignmentPage.CloseBrowser();
    }
}