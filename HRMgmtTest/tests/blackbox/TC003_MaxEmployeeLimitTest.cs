using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using NUnit.Framework;
using System;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.Tests.Blackbox;

public class TC003_MaxEmployeeLimitTest
{
    private IWebDriver _driver;
    private LoginPage _loginPage;
    private ShiftPage _shiftPage;
    private ShiftAssignmentPage _assignmentPage;
    private string _shiftName = string.Empty;
    private const string BaseUrl = "http://localhost:5156"; 

    [SetUp]
    public void Setup()
    {
        var options = new ChromeOptions();
        _driver = new ChromeDriver(options);
        _driver.Manage().Window.Maximize();
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        
        _loginPage = new LoginPage(_driver);
        _shiftPage = new ShiftPage(_driver);
        _assignmentPage = new ShiftAssignmentPage(_driver);
    }

    [Test]
    public void VerifyMaxEmployeeLimitPerShift()
    {
        // 1. Login as Admin
        _loginPage.GoTo(BaseUrl, "Admin");
        _loginPage.Login("qa_test", "123456");

        // 2. Open schedule page (skipping Create Shift)
        _assignmentPage.GoTo(BaseUrl);

        // 3. Get existing shift
        try {
            _shiftName = _assignmentPage.GetFirstAvailableShiftName();
        } catch (NoSuchElementException) {
            Assert.Inconclusive("No shifts available in the system to test with. Please seed data.");
        }

        // 4. Verify employee count
        var employeeCount = _assignmentPage.GetEmployeeCount();
        Assert.That(employeeCount, Is.GreaterThanOrEqualTo(11), "Not enough employees to perform the test (need 11+).");

        // 5. Assign 10 employees to the shift
        // Use column 0 (Monday)
        int dayIndex = 0; 
        
        try
        {
            for (int i = 0; i < 10; i++)
            {
                _assignmentPage.SelectShiftByText(i, dayIndex, _shiftName);
            }

            // 6. Assign 11th employee
            _assignmentPage.SelectShiftByText(10, dayIndex, _shiftName);
        }
        catch (NoSuchElementException)
        {
           throw;
        }
            
        // 7. Verify error message
        var errorText = _assignmentPage.GetErrorAlertText();
        
        // Also check if there's a browser alert
        try 
        {
             var alert = _driver.SwitchTo().Alert();
             errorText = alert.Text;
             alert.Accept();
        }
        catch (NoAlertPresentException) 
        {
            // Ignore
        }

        Assert.That(errorText, Does.Contain("limit").Or.Contain("maximum").Or.Contain("10"), 
            "Expected error message regarding shift limit, but found: " + (string.IsNullOrEmpty(errorText) ? "None" : errorText));
    }

    [TearDown]
    public void TearDown()
    {
        try {
            _driver.Quit();
            _driver.Dispose();
        } catch {}
    }
}
