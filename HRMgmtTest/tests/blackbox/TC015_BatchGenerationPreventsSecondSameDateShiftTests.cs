using System.Net.Http;
using System.Text.Json;
using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest;

public class TC015_BatchGenerationPreventsSecondSameDateShiftTests : BlackboxTestBase
{
    private ShiftAssignmentPage _shiftPage = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _shiftPage = new ShiftAssignmentPage(Driver);
    }

    [Test]
    public void TC015_BatchGeneration_PreventsSecondSameDateShiftForEmployee()
    {
        LoginAsAdminIfCredentialsExist();

        const int mondayColumn = 0;

        _shiftPage.GoTo(BaseUrl);
        var existingTemplates = _shiftPage.GetTemplateNames();
        if (existingTemplates.Count == 0)
        {
            Assert.Ignore("TC015 requires at least one existing template in Template List.");
        }
        var templateName = existingTemplates[0];
        _shiftPage.SelectTemplateFromMenu(templateName);
        _shiftPage.WaitForTemplateName(templateName);

        var (rowIndexText, _) = GetFirstEmployeeFromGrid();
        var rowIndex = int.Parse(rowIndexText);
        var employeeId = Wait.Until(d => d.FindElement(
            By.CssSelector($"input[name='Users[{rowIndex}].UserId']"))).GetAttribute("value");
        Assert.That(string.IsNullOrWhiteSpace(employeeId), Is.False, "Employee id is missing.");

        var shiftSelect = new SelectElement(Wait.Until(d =>
            d.FindElement(By.CssSelector($".shift-dropdown[data-row='{rowIndexText}'][data-col='{mondayColumn}']"))));
        var shiftOptions = shiftSelect.Options
            .Where(o => !string.IsNullOrWhiteSpace(o.GetAttribute("value")))
            .Select(o => new
            {
                Value = o.GetAttribute("value") ?? string.Empty,
                Label = o.Text.Trim(),
                Token = o.Text.Split('(')[0].Trim()
            })
            .Where(o => !string.IsNullOrWhiteSpace(o.Value) && !string.IsNullOrWhiteSpace(o.Token))
            .GroupBy(o => o.Value)
            .Select(g => g.First())
            .ToList();

        if (shiftOptions.Count < 2)
        {
            Assert.Ignore("TC015 requires at least two available shifts in the assignment dropdown.");
        }

        var firstShift = shiftOptions[0];
        var secondShift = shiftOptions[1];

        SetTemplateCell(rowIndex, mondayColumn, firstShift.Value, firstShift.Label);
        SaveTemplateAndEnsureNoError();

        _shiftPage.GoTo(BaseUrl);
        _shiftPage.SelectTemplateFromMenu(templateName);
        _shiftPage.WaitForTemplateName(templateName);
        _shiftPage.ClickGenerateSchedule();
        EnsureGenerateDidNotFail();

        var firstEvents = FetchEmployeeEvents(employeeId!);
        var firstShiftEvent = firstEvents.FirstOrDefault(e =>
            !string.IsNullOrWhiteSpace(e.Start) &&
            e.Title.Contains(firstShift.Token, StringComparison.OrdinalIgnoreCase));
        Assert.That(firstShiftEvent, Is.Not.Null, "Expected at least one generated event for first shift.");
        var targetDate = firstShiftEvent!.Start;

        _shiftPage.GoTo(BaseUrl);
        _shiftPage.SelectTemplateFromMenu(templateName);
        _shiftPage.WaitForTemplateName(templateName);
        SetTemplateCell(rowIndex, mondayColumn, secondShift.Value, secondShift.Label);
        SaveTemplateAndEnsureNoError();

        _shiftPage.GoTo(BaseUrl);
        _shiftPage.SelectTemplateFromMenu(templateName);
        _shiftPage.WaitForTemplateName(templateName);
        _shiftPage.ClickGenerateSchedule();
        EnsureGenerateDidNotFail();

        var secondEvents = FetchEmployeeEvents(employeeId!);
        var eventsOnTargetDate = secondEvents.Where(e => e.Start == targetDate).ToList();

        Assert.That(eventsOnTargetDate.Count, Is.EqualTo(1),
            $"Expected exactly one assignment on {targetDate} for employee {employeeId}.");
        Assert.That(eventsOnTargetDate[0].Title.Contains(firstShift.Token, StringComparison.OrdinalIgnoreCase), Is.True,
            "Original assignment should remain on the date after second generate.");
        Assert.That(eventsOnTargetDate[0].Title.Contains(secondShift.Token, StringComparison.OrdinalIgnoreCase), Is.False,
            "Second shift should not be added for same employee/date.");
    }

    private void SetTemplateCell(int rowIndex, int dayColumn, string shiftValue, string shiftLabel)
    {
        SelectShiftByLabel(rowIndex, dayColumn, shiftLabel);

        var gridIndex = rowIndex * 7 + dayColumn;
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "const hidden=document.querySelector(\"input[name='Grid[" + gridIndex + "]']\"); if(hidden){ hidden.value=arguments[0]; }",
            shiftValue);
    }

    private void SaveTemplateAndEnsureNoError()
    {
        _shiftPage.ClickSaveTemplate();
        AcceptAlertIfPresent(3);
        var saveError = _shiftPage.GetErrorAlertText();
        Assert.That(saveError, Is.Empty, $"Unexpected save error: {saveError}");
    }

    private void EnsureGenerateDidNotFail()
    {
        var generateAlert = AcceptAlertAndGetTextIfPresent(20);
        if (!string.IsNullOrWhiteSpace(generateAlert) &&
            generateAlert.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Fail($"Generate schedule failed: {generateAlert}");
        }
    }

    private List<EmployeeShiftEvent> FetchEmployeeEvents(string employeeId)
    {
        using var client = new HttpClient();
        var response = client
            .GetAsync($"{BaseUrl}/Shift/GetEmployeeShifts?employeeId={employeeId}")
            .GetAwaiter()
            .GetResult();
        response.EnsureSuccessStatusCode();
        var payload = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        var events = JsonSerializer.Deserialize<List<EmployeeShiftEvent>>(payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return events ?? new List<EmployeeShiftEvent>();
    }

    private sealed class EmployeeShiftEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Start { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
    }
}
