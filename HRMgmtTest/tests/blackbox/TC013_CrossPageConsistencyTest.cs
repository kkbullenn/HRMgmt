using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using NUnit.Framework;
using System;
using System.Linq;

namespace HRMgmtTest.Tests.Blackbox;

public class TC013_CrossPageConsistencyTest
{
    private IWebDriver _driver;
    private LoginPage _loginPage;
    private ShiftAssignmentPage _assignmentPage;
    private EmployeeShiftPage _employeeShiftPage;
    private const string BaseUrl = "http://localhost:5156"; 

    [SetUp]
    public void Setup()
    {
        var options = new ChromeOptions();
        _driver = new ChromeDriver(options);
        _driver.Manage().Window.Maximize();
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        
        _loginPage = new LoginPage(_driver);
        _assignmentPage = new ShiftAssignmentPage(_driver);
        _employeeShiftPage = new EmployeeShiftPage(_driver);
    }

    [Test]
    public void VerifyShiftConsistency_TemplateToEmployeeCalendar()
    {
        // 1. Login as Admin
        _loginPage.GoTo(BaseUrl, "Admin");
        _loginPage.Login("qa_test", "123456");

        // 2. Open Schedule Page
        _assignmentPage.GoTo(BaseUrl);

        // Clear existing assignments for clean test (optional but good)
        // Note: clearing might not be available or risky, but let's assume we work with current week.
        // We will target specific cells.
        
        // Identify test employees (first 2 rows)
        var emp1Name = _assignmentPage.GetEmployeeNameByIndex(0);
        var emp2Name = _assignmentPage.GetEmployeeNameByIndex(1);
        
        // Use current week dates? The grid usually shows current week. Assuming columns 0=Mon, 1=Tue...
        // Let's use Mon (col 0) for Emp1 and Tue (col 1) for Emp2.
        
        // Get an available shift
        string shiftName;
        try {
            shiftName = _assignmentPage.GetFirstAvailableShiftName();
        } catch (NoSuchElementException) {
            Assert.Inconclusive("No shifts available.");
            return;
        }

        // Assign shifts on the template grid
        _assignmentPage.SelectShiftByText(0, 0, shiftName); // Emp1, Mon
        _assignmentPage.SelectShiftByText(1, 1, shiftName); // Emp2, Tue

        // Extract pure shift name (dropdown: "Name (HH:mm-HH:mm)", calendar: "Name HH:mm-HH:mm")
        string pureShiftName = shiftName.Split('(')[0].Trim();

        // 4. Open Employee Shift Calendar page
        _employeeShiftPage.GoTo(BaseUrl);

        // 5. Verify Emp1 - events are AJAX-loaded so we must query the live DOM, not PageSource
        _employeeShiftPage.SelectEmployee(emp1Name);
        string emp1CalHtml = _employeeShiftPage.GetCalendarInnerHtml();
        Assert.That(emp1CalHtml, Does.Contain(pureShiftName),
            $"Employee {emp1Name} should have shift containing '{pureShiftName}' in their calendar.");

        // 5b. Verify Emp2
        _employeeShiftPage.SelectEmployee(emp2Name);
        string emp2CalHtml = _employeeShiftPage.GetCalendarInnerHtml();
        Assert.That(emp2CalHtml, Does.Contain(pureShiftName),
            $"Employee {emp2Name} should have shift containing '{pureShiftName}' in their calendar.");
        
        // This is a basic consistency check.
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
