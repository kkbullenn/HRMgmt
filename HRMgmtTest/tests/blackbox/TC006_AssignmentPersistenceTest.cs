using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;

namespace HRMgmtTest.Tests.Blackbox;


public class TC006_AssignmentPersistenceTest
{
    private IWebDriver _driver;
    private LoginPage _loginPage;
    private ShiftAssignmentPage _assignmentPage;
    private EmployeeShiftPage _employeeShiftPage;

    private const string BaseUrl  = "http://localhost:5156";
    private const string Username = "qa_test";
    private const string Password = "123456";

    // Use a timestamp suffix so each run gets a fresh name even if TearDown failed before.
    private string TemplateName;
    private string AssignStart;
    private string AssignEnd;

    [SetUp]
    public void Setup()
    {
        TemplateName = "TC006_" + DateTime.Now.ToString("yyyyMMddHHmmss");

        // Calculate this week's Monday to ensure reload (which defaults to current week) shows our data.
        DateTime today = DateTime.Today;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        DateTime monday = today.AddDays(-diff);
        AssignStart = monday.ToString("yyyy-MM-dd");
        AssignEnd = monday.AddDays(2).ToString("yyyy-MM-dd"); // Wednesday

        var options = new ChromeOptions();
        options.AddArgument("--log-level=3");

        _driver = new ChromeDriver(options);
        _driver.Manage().Window.Maximize();
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(8);

        _loginPage         = new LoginPage(_driver);
        _assignmentPage    = new ShiftAssignmentPage(_driver);
        _employeeShiftPage = new EmployeeShiftPage(_driver);
    }

    [Test]
    public void VerifyTemplateAndAssignmentsPersistAfterRefreshAndNavigation()
    {
        // ── 1. Login ────────────────────────────────────────────────────────────
        _loginPage.GoTo(BaseUrl, "Admin");
        _loginPage.Login(Username, Password);

        // ── 2. Open Schedule Page ───────────────────────────────────────────────
        _assignmentPage.GoTo(BaseUrl);

        // ── 3a. Verify at least one employee exists ────────────────────────────
        string emp1Name;
        try { emp1Name = _assignmentPage.GetEmployeeNameByIndex(0); }
        catch (WebDriverTimeoutException)
        {
            Assert.Inconclusive("No employees found. Please seed employee data.");
            return;
        }

        // ── 3b. Get shift names for Mon/Tue/Wed (re-use same if fewer than 3) ──
        // ── 3c. Fill template grid using first valid option for each column ────
        _assignmentPage.SetTemplateName(TemplateName);
        _assignmentPage.ApplyBatchShiftForColumn(0, "shiftMon"); // E001 Mon
        _assignmentPage.ApplyBatchShiftForColumn(0, "shiftTue"); // E001 Tue
        _assignmentPage.ApplyBatchShiftForColumn(0, "shiftWed"); // E001 Wed

        // ── 4. Generate Schedule (User says: "it has to generate first") ───────
        _assignmentPage.SetAssignmentStart(AssignStart);
        _assignmentPage.SetAssignmentEnd(AssignEnd);
        _assignmentPage.ClickGenerateSchedule();

        // The JS shows an alert then reloads the page. Wait for alert and dismiss.
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
        try {
            wait.Until(d => {
                try { d.SwitchTo().Alert(); return true; } catch (NoAlertPresentException) { return false; }
            });
            DismissAlert();
        } catch (WebDriverTimeoutException) { /* Proceed if no alert immediately */ }
        
        // After alert dismissal, JS calls window.location.reload(). Wait for reload.
        Thread.Sleep(2500);
        _assignmentPage.WaitForPage();

        // ── 5. Save Template ───────────────────────────────────────────────────
        // Now that we generated, we save the template.
        // We must re-enter the name as reload likely cleared it.
        _assignmentPage.SetTemplateName(TemplateName);
        _assignmentPage.ClickSaveTemplate();

        var saveWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
        try
        {
            saveWait.Until(d => {
                try { d.SwitchTo().Alert(); return true; } catch (NoAlertPresentException) {}
                
                // Only wait for the menu to update. The success alert might be stale from Generate.
                return d.FindElements(By.CssSelector("#templateMenu li"))
                        .Any(li => li.Text.Trim() == TemplateName);
            });
        }
        catch (WebDriverTimeoutException)
        {
             // Fallthrough
        }

        // Check for blocking alert
        try
        {
            var alert = _driver.SwitchTo().Alert();
            string text = alert.Text;
            alert.Accept();
            if (!text.Contains("Success") && !text.Contains("saved"))
                 Assert.Fail($"Save blocked by alert: {text}");
        }
        catch (NoAlertPresentException) { }

        // ── 6. Verify template now appears in side menu ─────────────────────────
        var templateNames = _assignmentPage.GetTemplateNames();
        Assert.That(templateNames, Does.Contain(TemplateName),
            $"Template '{TemplateName}' should appear in the template menu after saving.");

        // ── 6. Load template; verify cells are filled ──────────────────────────
        _assignmentPage.SelectTemplateFromMenu(TemplateName);
        Thread.Sleep(800); // allow JS grid to populate

        string cellMon = _assignmentPage.GetShiftCellValue(0, 0);
        string cellTue = _assignmentPage.GetShiftCellValue(0, 1);
        string cellWed = _assignmentPage.GetShiftCellValue(0, 2);

        Assert.Multiple(() =>
        {
            Assert.That(cellMon, Is.Not.Null.And.Not.Empty, $"{emp1Name} Monday cell should be filled after save.");
            Assert.That(cellTue, Is.Not.Null.And.Not.Empty, $"{emp1Name} Tuesday cell should be filled after save.");
            Assert.That(cellWed, Is.Not.Null.And.Not.Empty, $"{emp1Name} Wednesday cell should be filled after save.");
        });

        // ── 7. Navigate away and back ──────────────────────────────────────────
        _driver.Navigate().GoToUrl($"{BaseUrl}/Shift/Index");
        Thread.Sleep(400);
        _assignmentPage.GoTo(BaseUrl);
        _assignmentPage.WaitForPage();

        _assignmentPage.SelectTemplateFromMenu(TemplateName);
        
        // Wait for grid to populate (AJAX)
        var loadWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        loadWait.Until(d => !string.IsNullOrEmpty(_assignmentPage.GetShiftCellValue(0, 0)));

        string cellMonNav = _assignmentPage.GetShiftCellValue(0, 0);
        string cellTueNav = _assignmentPage.GetShiftCellValue(0, 1);
        string cellWedNav = _assignmentPage.GetShiftCellValue(0, 2);

        Assert.Multiple(() =>
        {
            Assert.That(cellMonNav, Is.Not.Null.And.Not.Empty, $"{emp1Name} Monday cell should persist after navigation.");
            Assert.That(cellTueNav, Is.Not.Null.And.Not.Empty, $"{emp1Name} Tuesday cell should persist after navigation.");
            Assert.That(cellWedNav, Is.Not.Null.And.Not.Empty, $"{emp1Name} Wednesday cell should persist after navigation.");
        });

        // ── 8. Generate Schedule ───────────────────────────────────────────────
        // Leave date range empty so the controller uses the shift's own date range.
        _assignmentPage.ClickGenerateSchedule();

        // The JS shows an alert then reloads the page. Wait for alert and dismiss.
        Thread.Sleep(3000);
        DismissAlert();
        // After alert dismissal, JS calls window.location.reload(). Wait for reload.
        Thread.Sleep(2000);
        _assignmentPage.WaitForPage();

        // ── 9. Verify employee calendar shows generated shifts ─────────────────
        _employeeShiftPage.GoTo(BaseUrl);
        _employeeShiftPage.SelectEmployee(emp1Name);

        string pureShiftName = "shiftMon"; 
        string calHtml = _employeeShiftPage.GetCalendarInnerHtml();

        Assert.That(calHtml, Does.Contain(pureShiftName),
            $"Employee calendar should show '{pureShiftName}' after Generate Schedule.");
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────



    private void DismissAlert()
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
                wait.Until(d => {
                    try { d.SwitchTo().Alert(); return true; }
                    catch (NoAlertPresentException) { return false; }
                });
                _driver.SwitchTo().Alert().Accept();
            }
            catch { break; }
        }
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            _assignmentPage.GoTo(BaseUrl);
            Thread.Sleep(600);
            DismissAlert(); // dismiss any leftover alert before inspecting menu
            var names = _assignmentPage.GetTemplateNames();
            if (names.Contains(TemplateName))
            {
                _assignmentPage.SelectTemplateFromMenu(TemplateName);
                Thread.Sleep(600);
                _assignmentPage.ClickDeleteTemplate();
                Thread.Sleep(500);
                try { _driver.SwitchTo().Alert().Accept(); } catch (NoAlertPresentException) {}
            }
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"[TC006] TearDown warning: {ex.Message}");
        }
        finally
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}
