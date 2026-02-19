using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HRMgmtTest.Tests.Blackbox;

/// <summary>
/// TC-014: Concurrent save/generate integrity.
/// 
/// Simulates two admin sessions concurrently editing and saving the same scheduling template,
/// then both clicking Generate Schedule. Verifies the final ShiftAssignments contain no
/// duplicate rows for the same employee + date.
///
/// EXPECTED RESULT: No crash; no duplicate (UserId, ShiftDate) pairs in assignments.
/// If duplicates ARE found, this is logged as a defect — the app should use database-level
/// unique constraints or optimistic concurrency to prevent this.
/// </summary>
public class TC014_ConcurrentSaveIntegrityTest
{
    private IWebDriver _driverA;   // Admin A
    private IWebDriver _driverB;   // Admin B
    private LoginPage _loginA;
    private LoginPage _loginB;
    private ShiftAssignmentPage _pageA;
    private ShiftAssignmentPage _pageB;

    private const string BaseUrl = "http://localhost:5156";
    private const string Username = "qa_test";
    private const string Password = "123456";

    // Use a dedicated test template name so we don't disturb production data.
    private const string TestTemplateName = "TC014_ConcurrentTest";

    // Assignment date range — use a past month so it doesn't conflict with realistic scheduling.
    private const string AssignStart = "2025-01-01";
    private const string AssignEnd   = "2025-01-07";

    [SetUp]
    public void Setup()
    {
        var options = new ChromeOptions();
        // Optional: suppress noise output
        options.AddArgument("--log-level=3");

        _driverA = new ChromeDriver(options);
        _driverA.Manage().Window.Maximize();
        _driverA.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(8);

        _driverB = new ChromeDriver(options);
        _driverB.Manage().Window.Maximize();
        _driverB.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(8);

        _loginA  = new LoginPage(_driverA);
        _loginB  = new LoginPage(_driverB);
        _pageA   = new ShiftAssignmentPage(_driverA);
        _pageB   = new ShiftAssignmentPage(_driverB);
    }

    [Test]
    public void VerifyNoDuplicatesOnConcurrentGenerateSchedule()
    {
        // ─── Phase 1: Login both admins ───────────────────────────────────────────
        _loginA.GoTo(BaseUrl, "Admin");
        _loginA.Login(Username, Password);

        _loginB.GoTo(BaseUrl, "Admin");
        _loginB.Login(Username, Password);

        // ─── Phase 2: Both navigate to ShiftAssignment page ───────────────────────
        _pageA.GoTo(BaseUrl);
        _pageB.GoTo(BaseUrl);

        // Ensure at least one shift exists; get its name.
        string shiftName;
        try {
            shiftName = _pageA.GetFirstAvailableShiftName();
        } catch (NoSuchElementException) {
            Assert.Inconclusive("No shifts in the system. Seed at least one shift before running TC-014.");
            return;
        }

        // ─── Phase 3: Admin A edits and saves the template ────────────────────────
        // Admin A creates/overwrites the template (Weekly, row 0 col 0 = shiftName)
        _pageA.SetTemplateName(TestTemplateName);
        _pageA.SelectShiftByText(0, 0, shiftName); // Row 0, Monday
        _pageA.SetAssignmentStart(AssignStart);
        _pageA.SetAssignmentEnd(AssignEnd);

        // ─── Phase 4: Admin B also sets the same template (slightly delayed) ─────
        // Admin B loads and edits the same template  — this simulates overlapping edits.
        Thread.Sleep(300); // small delay so A is slightly ahead
        _pageB.SetTemplateName(TestTemplateName);
        _pageB.SelectShiftByText(0, 0, shiftName); // Same cell
        _pageB.SetAssignmentStart(AssignStart);
        _pageB.SetAssignmentEnd(AssignEnd);

        // ─── Phase 5: Both click "Generate Schedule" concurrently ─────────────────
        // We launch them in parallel threads to maximize overlap.
        var taskA = Task.Run(() => SafeAutoAssign(_driverA, _pageA));
        var taskB = Task.Run(() => SafeAutoAssign(_driverB, _pageB));

        // Wait for both to complete (or timeout after 60s)
        bool bothDone = Task.WhenAll(taskA, taskB).Wait(TimeSpan.FromSeconds(60));
        Assert.That(bothDone, Is.True, "One or both Generate Schedule operations timed out.");

        // Dismiss any alerts still open on either driver before querying the API.
        DismissAnyAlert(_driverA);
        DismissAnyAlert(_driverB);
        Thread.Sleep(1000); // allow page to settle after alert dismissal

        // ─── Phase 6: Detect duplicate assignments via the API ──────────────────
        // Use driverA (still authenticated) to call GetEmployeeShifts for each employee.
        // First, get all employee IDs via the GetEmployees API.
        var employees = GetEmployees(_driverA);
        Assert.That(employees, Is.Not.Empty, "No employees found — cannot verify assignments.");

        var duplicates = new List<string>();
        foreach (var emp in employees)
        {
            var events = GetEmployeeShifts(_driverA, emp.Id);
            var dateCounts = events.GroupBy(e => e.Start).Where(g => g.Count() > 1).ToList();
            foreach (var dup in dateCounts)
            {
                duplicates.Add($"Employee {emp.Name} has {dup.Count()} assignments on {dup.Key}");
            }
        }

        // Assert — if duplicates exist, the test is a defect report.
        Assert.That(duplicates, Is.Empty,
            "Duplicate shift assignments detected after concurrent Generate Schedule:\n" +
            string.Join("\n", duplicates));
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Dismisses any currently open browser alert, safely.</summary>
    private static void DismissAnyAlert(IWebDriver driver)
    {
        // Try up to 3 times in case multiple alerts stack up.
        for (int i = 0; i < 3; i++)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                wait.Until(d => {
                    try { d.SwitchTo().Alert(); return true; }
                    catch (NoAlertPresentException) { return false; }
                });
                driver.SwitchTo().Alert().Accept();
            }
            catch
            {
                break; // no more alerts
            }
        }
    }

    /// <summary>Clicks "Generate Schedule" and dismisses any alerts (success or error).</summary>
    private void SafeAutoAssign(IWebDriver driver, ShiftAssignmentPage page)
    {
        try
        {
            page.ClickGenerateSchedule();

            // Wait briefly for the alert dialog from the auto-assign script
            Thread.Sleep(2000);
            try
            {
                var alert = driver.SwitchTo().Alert();
                alert.Accept();
            }
            catch (NoAlertPresentException) { /* no alert shown — silent success */ }

            // Another alert may follow (some paths show a second confirmation)
            Thread.Sleep(500);
            try
            {
                var alert2 = driver.SwitchTo().Alert();
                alert2.Accept();
            }
            catch (NoAlertPresentException) { }
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"[TC014] SafeAutoAssign exception: {ex.Message}");
        }
    }

    private record EmployeeInfo(string Id, string Name);
    private record ShiftEvent(string Start);

    /// <summary>Calls the /Shift/GetEmployees JSON endpoint via the browser.</summary>
    private List<EmployeeInfo> GetEmployees(IWebDriver driver)
    {
        driver.Navigate().GoToUrl($"{BaseUrl}/Shift/GetEmployees");
        Thread.Sleep(500);
        var body = driver.FindElement(By.TagName("body")).Text;
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.EnumerateArray()
                .Select(e => new EmployeeInfo(
                    e.GetProperty("id").GetString() ?? string.Empty,
                    e.GetProperty("name").GetString() ?? string.Empty))
                .ToList();
        }
        catch
        {
            return new List<EmployeeInfo>();
        }
    }

    /// <summary>Calls the /Shift/GetEmployeeShifts JSON endpoint for one employee.</summary>
    private List<ShiftEvent> GetEmployeeShifts(IWebDriver driver, string employeeId)
    {
        driver.Navigate().GoToUrl($"{BaseUrl}/Shift/GetEmployeeShifts?employeeId={employeeId}");
        Thread.Sleep(500);
        var body = driver.FindElement(By.TagName("body")).Text;
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.EnumerateArray()
                .Select(e => new ShiftEvent(e.GetProperty("start").GetString() ?? string.Empty))
                .ToList();
        }
        catch
        {
            return new List<ShiftEvent>();
        }
    }

    [TearDown]
    public void TearDown()
    {
        // Attempt to clean up the test template via one of the active drivers.
        try
        {
            // Navigate back to assignment page and delete the test template
            _pageA.GoTo(BaseUrl);
            Thread.Sleep(600);
            var templates = _pageA.GetTemplateNames();
            if (templates.Contains(TestTemplateName))
            {
                _pageA.SelectTemplateFromMenu(TestTemplateName);
                Thread.Sleep(600);
                // Dismiss any pre-existing alert
                try { _driverA.SwitchTo().Alert().Accept(); } catch (NoAlertPresentException) {}
                _pageA.ClickDeleteTemplate();
                Thread.Sleep(500);
                try { _driverA.SwitchTo().Alert().Accept(); } catch (NoAlertPresentException) {}
            }
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"[TC014] TearDown cleanup warning: {ex.Message}");
        }
        finally
        {
            _driverA?.Quit();
            _driverA?.Dispose();
            _driverB?.Quit();
            _driverB?.Dispose();
        }
    }
}
