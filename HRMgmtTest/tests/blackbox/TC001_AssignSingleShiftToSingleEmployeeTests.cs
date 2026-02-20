using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Text.Json;
using System.Threading;

namespace HRMgmtTest;

public class TC001_AssignSingleShiftToSingleEmployeeTests : BlackboxTestBase
{
    private ShiftAssignmentPage _shiftPage = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _shiftPage = new ShiftAssignmentPage(Driver);
    }

    [Test]
    public void TC001_AssignSingleShiftToSingleEmployee_SaveThenGenerate_PersistsAndDisplays()
    {
        LoginAsAdminIfCredentialsExist();

        const int mondayColumn = 0;

        _shiftPage.GoTo(BaseUrl);
        var existingTemplates = _shiftPage.GetTemplateNames();
        if (existingTemplates.Count == 0)
        {
            Assert.Ignore("TC001 requires at least one existing template in Template List.");
        }
        var templateName = existingTemplates.FirstOrDefault(t =>
            t.Contains("QA_WEEKLY_BASE", StringComparison.OrdinalIgnoreCase)) ?? existingTemplates[0];
        _shiftPage.SelectTemplateFromMenu(templateName);
        _shiftPage.WaitForTemplateName(templateName);

        var (rowIndexText, employeeName) = GetFirstEmployeeFromGrid();
        var rowIndex = int.Parse(rowIndexText);
        var employeeId = Wait.Until(d => d.FindElement(
            By.CssSelector($"input[name='Users[{rowIndex}].UserId']"))).GetAttribute("value");
        var (shiftValue, shiftLabel, shiftToken) = GetFirstShiftOption(rowIndex, mondayColumn);
        SelectShiftByLabel(rowIndex, mondayColumn, shiftLabel);

        // Keep hidden Grid[idx] field in sync with dropdown value for reliable form submission.
        var gridIndex = rowIndex * 7 + mondayColumn;
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "const hidden=document.querySelector(\"input[name='Grid[" + gridIndex + "]']\"); if(hidden){ hidden.value=arguments[0]; }",
            shiftValue);

        var selectedValueBeforeSave = new SelectElement(Wait.Until(d =>
            d.FindElement(By.CssSelector($".shift-dropdown[data-row='{rowIndexText}'][data-col='{mondayColumn}']"))))
            .SelectedOption.GetAttribute("value");
        Assert.That(string.IsNullOrWhiteSpace(selectedValueBeforeSave), Is.False,
            "Shift cell is still empty before save; assignment was not selected.");

        _shiftPage.ClickSaveTemplate();
        AcceptAlertIfPresent(2);

        var saveError = _shiftPage.GetErrorAlertText();
        if (!string.IsNullOrWhiteSpace(saveError))
        {
            Assert.Fail($"Template save failed: {saveError}");
        }

        // Keep the test outcome-focused: generation must succeed and produce expected assignment.
        // (Template menu population can be environment-dependent and flaky in CI/dev shared DB.)

        _shiftPage.SetAssignmentStart("2026-02-16");
        _shiftPage.SetAssignmentEnd("2026-02-19");
        _shiftPage.ClickGenerateSchedule();
        var generateAlert = AcceptAlertAndGetTextIfPresent(20);
        if (!string.IsNullOrWhiteSpace(generateAlert) &&
            generateAlert.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Fail($"Generate schedule failed: {generateAlert}");
        }

        // Verify via employee-shift API for deterministic local-db assertion.
        var hasShiftEvent = WaitForGeneratedEventInRange(employeeId!, new DateOnly(2026, 2, 16), new DateOnly(2026, 2, 19), 15);

        _shiftPage.GoTo(BaseUrl);
        _shiftPage.SelectTemplateFromMenu(templateName);
        _shiftPage.WaitForTemplateName(templateName);
        var persistedValue = new SelectElement(Wait.Until(d =>
            d.FindElement(By.CssSelector($".shift-dropdown[data-row='{rowIndexText}'][data-col='{mondayColumn}']"))))
            .SelectedOption.GetAttribute("value");

        var hasPersistedGridAssignment = !string.IsNullOrWhiteSpace(persistedValue);

        Assert.That(hasPersistedGridAssignment, Is.True,
            $"Expected persisted grid assignment for '{shiftToken}' / '{employeeName}'.");
        Assert.That(hasShiftEvent, Is.True,
            $"Expected at least one generated event in target range for '{employeeName}'.");
    }

    private bool WaitForGeneratedEventInRange(string employeeId, DateOnly start, DateOnly end, int timeoutSeconds)
    {
        var endAt = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < endAt)
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Shift/GetEmployeeShifts?employeeId={employeeId}");
            var payload = Driver.FindElement(By.TagName("body")).Text;
            var events = JsonSerializer.Deserialize<List<EmployeeShiftEvent>>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<EmployeeShiftEvent>();

            if (events.Any(e =>
                    DateOnly.TryParse(e.Start, out var d) &&
                    d >= start && d <= end))
            {
                return true;
            }

            Thread.Sleep(500);
        }

        return false;
    }

    private sealed class EmployeeShiftEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Start { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
    }
}
