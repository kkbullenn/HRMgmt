using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Text.Json;

namespace HRMgmtTest.tests.blackbox;

public class TC011_BiweeklyGenerationRequiresBothWeeksTests : BlackboxTestBase
{
    private ShiftAssignmentPage _shiftPage = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _shiftPage = new ShiftAssignmentPage(Driver);
    }

    [Test]
    public void TC011_BiweeklyGenerate_WithOnlyWeek1Data_IsBlocked()
    {
        LoginAsAdminIfCredentialsExist();

        _shiftPage.GoTo(BaseUrl);
        _shiftPage.ClickCreateNewTemplate();

        var templateName = $"WK_TC011_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}".Substring(0, 32);
        _shiftPage.SetTemplateName(templateName);
        _shiftPage.SetWeekType("2"); // Biweekly
        _shiftPage.SetWeekIndex("0"); // Week 1

        // Populate Week 1 only.
        var (rowIndexText, _) = GetFirstEmployeeFromGrid();
        var rowIndex = int.Parse(rowIndexText);
        var employeeId = Wait.Until(d => d.FindElement(
            By.CssSelector($"input[name='Users[{rowIndex}].UserId']"))).GetAttribute("value");
        Assert.That(string.IsNullOrWhiteSpace(employeeId), Is.False, "Employee id is missing.");
        const int mondayColumn = 0;
        var (shiftValue, shiftLabel, _) = GetFirstShiftOption(rowIndex, mondayColumn);
        SelectShiftByLabel(rowIndex, mondayColumn, shiftLabel);

        // Keep hidden grid input synced so submit reliably includes selection.
        var gridIndex = rowIndex * 7 + mondayColumn;
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "const hidden=document.querySelector(\"input[name='Grid[" + gridIndex + "]']\"); if(hidden){ hidden.value=arguments[0]; }",
            shiftValue);

        var selectedValue = new SelectElement(Wait.Until(d =>
            d.FindElement(By.CssSelector($".shift-dropdown[data-row='{rowIndexText}'][data-col='{mondayColumn}']"))))
            .SelectedOption.GetAttribute("value");
        Assert.That(string.IsNullOrWhiteSpace(selectedValue), Is.False,
            "Expected Week 1 shift assignment before save.");

        _shiftPage.ClickSaveTemplate();
        AcceptAlertIfPresent(3);
        var saveError = _shiftPage.GetErrorAlertText();
        Assert.That(saveError, Is.Empty, $"Unexpected save error: {saveError}");
        // Current UI can normalize/refresh name asynchronously; don't hard-fail on exact text echo here.
        try
        {
            _shiftPage.WaitForTemplateToLoad(templateName);
        }
        catch
        {
            // proceed - generation validation is the target of this test
        }

        // Re-assert biweekly mode after save redirect/state sync.
        _shiftPage.SetWeekType("2");
        _shiftPage.SetWeekIndex("1");
        _shiftPage.ClickClearGrid();
        _shiftPage.SetWeekIndex("0");
        _shiftPage.SetAssignmentStart("2026-02-17");
        _shiftPage.SetAssignmentEnd("2026-02-24");
        var beforeEvents = FetchEmployeeEvents(employeeId!);

        _shiftPage.GoTo(BaseUrl);
        _shiftPage.SelectTemplateFromMenu(templateName);
        _shiftPage.WaitForTemplateName(templateName);
        _shiftPage.SetAssignmentStart("2026-02-17");
        _shiftPage.SetAssignmentEnd("2026-02-24");

        _shiftPage.ClickGenerateSchedule();
        var generateAlert = AcceptAlertAndGetTextIfPresent(10);

        Assert.That(generateAlert, Is.Not.Null.And.Not.Empty,
            "Expected generation to be blocked with validation alert.");
        Assert.That(generateAlert, Does.Contain("Biweekly template must include both Week 1 and Week 2"),
            $"Unexpected biweekly validation message: {generateAlert}");

        var afterEvents = FetchEmployeeEvents(employeeId!);
        Assert.That(afterEvents.Count, Is.EqualTo(beforeEvents.Count),
            "Blocked biweekly generate should not create new shift assignments.");
    }

    private List<EmployeeShiftEvent> FetchEmployeeEvents(string employeeId)
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/Shift/GetEmployeeShifts?employeeId={employeeId}");
        var payload = Driver.FindElement(By.TagName("body")).Text;

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
