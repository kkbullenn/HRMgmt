using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using NUnit.Framework;
using System;

namespace HRMgmtTest.Tests.Blackbox;

public class TC004_ShiftChronologyValidationTest
{
    private IWebDriver _driver;
    private LoginPage _loginPage;
    private ShiftPage _shiftPage;
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
    }

    [Test]
    public void VerifyShiftChronologyValidation()
    {
        // 1. Login
        _loginPage.GoTo(BaseUrl, "Admin");
        _loginPage.Login("qa_test", "123456");

        // 2. Go to Create Shift
        _shiftPage.GoToCreate(BaseUrl);

        // Common data
        string baseName = "TC004_Invalid_";

        // Scenario 1: End Date before Start Date
        // User example: Start 2026-02-01, End 2026-01-31
        // We'll use relative dates to ensure it's always "invalid chronology" regardless of current date, 
        // though user specific dates are fine too. I'll use dates that are definitely invalid relative to each other.
        _shiftPage.SetStartDate("2026-02-01"); 
        _shiftPage.SetEndDate("2026-01-31");
        _shiftPage.SetStartTime("09:00");
        _shiftPage.SetEndTime("17:00");
        // We need to set a name to avoid Name required error masking the date error
        _driver.FindElement(By.Id("Name")).SendKeys(baseName + "DateError");
        _driver.FindElement(By.Id("RequiredCount")).SendKeys("1");

        _shiftPage.ClickCreateExpectFailure();

        // Verify failure - check URL or specific error
        Assert.That(_driver.Url.ToLower(), Contains.Substring("/shift/create"), "Should stay on Create page for invalid dates.");
        // Check for specific validation errors if possible
        string dateError = _shiftPage.GetGeneralError() + _shiftPage.GetStartDateError() + _shiftPage.GetEndDateError();
        Assert.That(dateError, Is.Not.Empty, "Expected error message for End Date < Start Date");

        // Refresh to clear
        _shiftPage.GoToCreate(BaseUrl);

        // Scenario 2: Start Time > End Time
        // Time: 14:00-13:00
        _driver.FindElement(By.Id("Name")).SendKeys(baseName + "TimeError1");
        _shiftPage.SetStartDate("2026-02-01");
        _shiftPage.SetEndDate("2026-02-01"); // Same day
        _shiftPage.SetStartTime("14:00");
        _shiftPage.SetEndTime("13:00");
        _driver.FindElement(By.Id("RequiredCount")).SendKeys("1");

        _shiftPage.ClickCreateExpectFailure();
        Assert.That(_driver.Url.ToLower(), Contains.Substring("/shift/create"), "Should stay on Create page for Start Time > End Time.");
        
        // Scenario 3: Start Time == End Time
        // Time: 09:00-09:00
        _shiftPage.GoToCreate(BaseUrl); // Reset
        _driver.FindElement(By.Id("Name")).SendKeys(baseName + "TimeError2");
        _shiftPage.SetStartDate("2026-02-01");
        _shiftPage.SetEndDate("2026-02-01");
        _shiftPage.SetStartTime("09:00");
        _shiftPage.SetEndTime("09:00");
        _driver.FindElement(By.Id("RequiredCount")).SendKeys("1");

        _shiftPage.ClickCreateExpectFailure();
        Assert.That(_driver.Url.ToLower(), Contains.Substring("/shift/create"), "Should stay on Create page for Identical Start/End Time.");
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
