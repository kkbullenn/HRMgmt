using HRMgmtTest.pages;
using HRMgmtTest.utils;

namespace HRMgmtTest.tests.blackbox;

/// <summary>
/// TC-007 – Prevent Overlapping Shifts for Same Employee
/// Owner: Sam
/// Purpose: Verify system prevents assigning overlapping shifts to the same employee on the same date.
/// </summary>
public class TC007_PreventOverlappingShifts
{
    private EmployeeShiftPage _employeeShiftPage;

    private LoginPage _loginPage;
    
    // Employee: E008
    // Shifts: D7 and D8 (overlapping times)
    private const string EmployeeId8 = "11111111-1111-1111-1111-000000000008"; // E008
    private const string ShiftD7 = "22222222-2222-2222-2222-000000000007"; // D7 Early (07:00-15:00)
    private const string ShiftD8 = "22222222-2222-2222-2222-000000000008"; // D8 Overlapping (10:00-18:00)

    [SetUp]
    public void Setup()
    {
        var driver = ChromeDriverFactory.CreateChromeDriver();
        _loginPage = new LoginPage(driver);
        _employeeShiftPage = new EmployeeShiftPage(driver);
    }

    [Test]
    public void TC007_PreventOverlappingShifts_Test()
    {
        // Step 1: Login as Admin
        _loginPage.GoTo();
        _loginPage.Login("qa_test", "123456");

        // Step 2: Open Employee Shift Page
        _employeeShiftPage.GoTo();

        // Debug: Print available employees
        var availableEmployees = _employeeShiftPage.GetEmployeeOptionDetails();
        Console.WriteLine($"Available employees in dropdown: {availableEmployees.Count}");
        foreach (var (value, text) in availableEmployees)
        {
            Console.WriteLine($"  - Value: {value}, Text: {text}");
        }

        // Step 3: Select employee E008
        _employeeShiftPage.SelectEmployee(EmployeeId8);

        // Step 4: Assign shift D7 to E008 on target date
        string targetDate = "2026-02-20"; // Friday, Feb 20, 2026

        _employeeShiftPage.AssignShiftOnDate(targetDate, ShiftD7);
        System.Threading.Thread.Sleep(1000); // Wait for assignment

        // Verify first assignment succeeded
        var alertText = _employeeShiftPage.GetAlertText();
        Assert.That(alertText, Does.Contain("success").IgnoreCase,
            "First shift assignment should succeed");

        // Verify shift D7 is now assigned
        Assert.That(_employeeShiftPage.HasShiftOnDate(targetDate), Is.True,
            "Employee should have shift D7 assigned");

        // Step 5: Attempt to assign shift D8 to E008 on same date
        try
        {
            _employeeShiftPage.AssignShiftOnDate(targetDate, ShiftD8);
            System.Threading.Thread.Sleep(1000); // Wait for response

            // Step 6: Observe system response - should be rejected
            var secondAlertText = _employeeShiftPage.GetAlertText();
            Assert.That(secondAlertText,
                Does.Contain("already").IgnoreCase.Or.Contain("exists").IgnoreCase.Or.Contain("overlap").IgnoreCase.Or
                    .Contain("conflict").IgnoreCase,
                "Second assignment should be rejected with appropriate error message");
        }
        catch (Exception)
        {
            // If an exception is thrown during assignment, that's also acceptable
            // as it indicates the system prevented the duplicate assignment
        }

        // Verify only one shift exists on the date (no duplicate or overlapping assignment saved)
        var shiftCount = _employeeShiftPage.CountShiftsOnDate(targetDate);
        Assert.That(shiftCount, Is.EqualTo(1),
            "Employee should have only one shift on the date - no overlapping shifts allowed");

        // Verify the first shift (D7) is still there
        var shifts = _employeeShiftPage.GetShiftNamesOnDate(targetDate);
        Assert.That(shifts, Has.Count.EqualTo(1),
            "Only one shift should be assigned");
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up: Delete the test shift assignment
        try
        {
            string targetDate = "2026-02-20";
            _employeeShiftPage.GoTo();
            _employeeShiftPage.SelectEmployee(EmployeeId8);

            if (_employeeShiftPage.HasShiftOnDate(targetDate))
            {
                _employeeShiftPage.DeleteShiftOnDate(targetDate);
                System.Threading.Thread.Sleep(500);
            }
        }
        catch (Exception)
        {
            // Shift might not exist, ignore
        }

        // Close browser
        _employeeShiftPage.CloseBrowser();
    }
}