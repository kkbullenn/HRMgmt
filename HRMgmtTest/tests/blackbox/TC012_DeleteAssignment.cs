using HRMgmtTest.pages;
using HRMgmtTest.utils;

namespace HRMgmtTest.tests.blackbox;

/// <summary>
/// TC-012 – Delete Assignment from Employee Calendar
/// Owner: Sam
/// Purpose: Verify deleting an existing assignment works and persists after refresh.
/// </summary>
public class TC012_DeleteAssignment
{
    private EmployeeShiftPage _employeeShiftPage;
    private LoginPage _loginPage;

    // Test data - Matches SQL insert statements in testData/test_employees_shifts.sql
    // Employee: E001
    // Any shift for testing deletion
    private const string EmployeeId1 = "11111111-1111-1111-1111-000000000001"; // E001
    private const string TestShift = "22222222-2222-2222-2222-000000000001"; // D1 Morning (08:00-16:00)

    [SetUp]
    public void Setup()
    {
        var driver = ChromeDriverFactory.CreateChromeDriver();
        _employeeShiftPage = new EmployeeShiftPage(driver);
        _loginPage = new LoginPage(driver);

        // Pre-condition: Create an assignment to delete
        SetupTestAssignment();
    }

    private void SetupTestAssignment()
    {
        _loginPage.GoTo();
        _loginPage.Login("qa_test", "123456");
        // Open Employee Shift Page and assign a shift for testing
        _employeeShiftPage.GoTo();
        _employeeShiftPage.SelectEmployee(EmployeeId1);

        string targetDate = "2026-02-25"; // Wednesday, Feb 25, 2026

        // Clean up any existing assignment first
        if (_employeeShiftPage.HasShiftOnDate(targetDate))
        {
            _employeeShiftPage.DeleteShiftOnDate(targetDate);
            var deleteAlert = _employeeShiftPage.GetAlertText();
            Assert.That(deleteAlert, Does.Contain("deleted").IgnoreCase,
                "Expected success message after deletion");
        }

        // Create a test assignment
        _employeeShiftPage.AssignShiftOnDate(targetDate, TestShift);

        // Verify assignment was created
        var alertText = _employeeShiftPage.GetAlertText();
        if (alertText.Contains("success", StringComparison.OrdinalIgnoreCase))
        {
            // Assignment successful
        }
    }

    [Test]
    public void TC012_DeleteAssignment_Test()
    {
        // Step 1: Login as Admin (done)
        // Step 2: Open Employee Shift Page
        _employeeShiftPage.GoTo();

        // Step 3: Select employee E001
        _employeeShiftPage.SelectEmployee(EmployeeId1);

        // Step 4: Verify assignment exists on target date
        string targetDate = "2026-02-25"; // Wednesday, Feb 25, 2026

        Assert.That(_employeeShiftPage.HasShiftOnDate(targetDate), Is.True,
            "E001 should have an existing assignment on the target date (precondition)");

        var shiftsBeforeDelete = _employeeShiftPage.CountShiftsOnDate(targetDate);
        Assert.That(shiftsBeforeDelete, Is.GreaterThan(0),
            "At least one shift should exist before deletion");

        // Step 5: Click assigned shift event
        // Step 6: Confirm delete
        _employeeShiftPage.DeleteShiftOnDate(targetDate);

        // Verify deletion confirmation alert
        if (_employeeShiftPage.IsAlertPresent())
        {
            var alertText = _employeeShiftPage.GetAlertText();
            Assert.That(alertText, Does.Contain("deleted").IgnoreCase,
                "Expected success message after deletion");
        }

        // Verify assignment is deleted immediately
        var shiftsAfterDelete = _employeeShiftPage.CountShiftsOnDate(targetDate);
        Assert.That(shiftsAfterDelete, Is.EqualTo(0),
            "No shifts should exist on the target date after deletion");

        // Step 7: Refresh page
        _employeeShiftPage.Refresh();
        _employeeShiftPage.SelectEmployee(EmployeeId1);

        // Step 8: Verify assignment no longer exists after refresh
        var shiftsAfterRefresh = _employeeShiftPage.CountShiftsOnDate(targetDate);
        Assert.That(shiftsAfterRefresh, Is.EqualTo(0),
            "Assignment should not reappear after page refresh");

        Assert.That(_employeeShiftPage.HasShiftOnDate(targetDate), Is.False,
            "E001 should not have any shift on the target date after deletion and refresh");

        // Verify no errors occurred
        Assert.DoesNotThrow(() => _employeeShiftPage.SelectEmployee(EmployeeId1),
            "No errors should occur during the deletion process");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up: Ensure test assignment is deleted
        try
        {
            string targetDate = "2026-02-25";
            _employeeShiftPage.GoTo();
            _employeeShiftPage.SelectEmployee(EmployeeId1);

            if (_employeeShiftPage.HasShiftOnDate(targetDate))
            {
                _employeeShiftPage.DeleteShiftOnDate(targetDate);
                System.Threading.Thread.Sleep(500);
            }
        }
        catch (Exception)
        {
            // Assignment might already be deleted, ignore
        }

        // Close browser
        _employeeShiftPage.CloseBrowser();
    }
}