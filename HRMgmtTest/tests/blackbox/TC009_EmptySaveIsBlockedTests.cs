using HRMgmtTest.pages;
using OpenQA.Selenium;

namespace HRMgmtTest;

// TC009 covers two save scenarios:
//
// 1) No-change save on existing template:
//    Open an existing template, make no edits, click Save.
//    Verifies behavior for unchanged state.
//
// 2) Empty-state save blocked:
//    Clear all assignments, then click Save.
//    Verifies validation blocks saving when no shifts are assigned.
//
// Note:
// - "No changes at all" is valid only if the loaded template is already empty.
// - "Force empty state" (clear all cells) is used when templates may already contain assignments.

public class TC009_EmptySaveIsBlockedTests : BlackboxTestBase
{
    private ShiftAssignmentPage _shiftPage = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _shiftPage = new ShiftAssignmentPage(Driver);
    }

    // Scenario 1: No-change save on existing template.
    [Test]
    public void TC009A_NoChangeSave_OnExistingTemplate_PreservesState()
    {
        LoginAsAdminIfCredentialsExist();

        _shiftPage.GoTo(BaseUrl);
        var existingTemplates = _shiftPage.GetTemplateNames();
        if (existingTemplates.Count == 0)
        {
            Assert.Ignore("TC009A requires at least one existing template.");
        }

        var templateName = existingTemplates[0];
        _shiftPage.SelectTemplateFromMenu(templateName);
        _shiftPage.WaitForTemplateName(templateName);

        // Capture current cell value and save without edits.
        var beforeCellValue = _shiftPage.GetShiftCellValue(0, 0);

        var saveButton = Wait.Until(d => d.FindElement(By.Id("saveTemplateBtn")));
        Assert.That(saveButton.Enabled, Is.True, "Save button should be clickable.");

        _shiftPage.ClickSaveTemplate();
        var alertText = AcceptAlertAndGetTextIfPresent(5);
        if (!string.IsNullOrWhiteSpace(alertText))
        {
            Assert.That(alertText, Does.Not.Contain("error").IgnoreCase,
                $"Unexpected error alert on no-change save: {alertText}");
        }

        var saveError = _shiftPage.GetErrorAlertText();
        Assert.That(saveError, Is.Empty, "No server-side error banner expected for no-change save.");

        // Re-open same template and ensure state was not altered by no-change save.
        _shiftPage.GoTo(BaseUrl);
        _shiftPage.SelectTemplateFromMenu(templateName);
        _shiftPage.WaitForTemplateName(templateName);
        var afterCellValue = _shiftPage.GetShiftCellValue(0, 0);
        Assert.That(afterCellValue, Is.EqualTo(beforeCellValue),
            "No-change save should preserve existing template cell values.");
    }

    // Scenario 2: Empty-state save blocked by validation.
    [Test]
    public void TC009_SaveWithoutAssignments_ShowsValidationAlert()
    {
        LoginAsAdminIfCredentialsExist();

        _shiftPage.GoTo(BaseUrl);
        var existingTemplates = _shiftPage.GetTemplateNames();
        if (existingTemplates.Count == 0)
        {
            Assert.Ignore("TC009 requires at least one existing template in Template List.");
        }

        // Prefer a Weekly template to make empty-save validation deterministic.
        string? weeklyTemplateName = null;
        foreach (var name in existingTemplates)
        {
            _shiftPage.SelectTemplateFromMenu(name);
            _shiftPage.WaitForTemplateName(name);
            if (_shiftPage.GetWeekType() == "1")
            {
                weeklyTemplateName = name;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(weeklyTemplateName))
        {
            Assert.Ignore("TC009 requires at least one existing Weekly template in Template List.");
        }

        // Ensure no assignments remain before save attempt.
        _shiftPage.ClickClearGrid();
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "document.querySelectorAll(\".shift-dropdown\").forEach(s=>{s.value='';s.dispatchEvent(new Event('change',{bubbles:true}));});" +
            "document.querySelectorAll(\"input[name^='Grid[']\").forEach(i=>i.value='');");

        var anyAssigned = Driver.FindElements(By.CssSelector(".shift-dropdown"))
            .Any(s => !string.IsNullOrWhiteSpace(s.GetAttribute("value")));
        Assert.That(anyAssigned, Is.False, "Expected all grid cells to be empty before save.");

        if (_shiftPage.GetWeekType() == "2")
        {
            _shiftPage.SetWeekIndex("1");
            _shiftPage.ClickClearGrid();
            _shiftPage.SetWeekIndex("0");
        }

        // In the current app code, Save is clickable and validation is done via alert.
        var saveButton = Wait.Until(d => d.FindElement(By.Id("saveTemplateBtn")));
        Assert.That(saveButton.Enabled, Is.True, "Save button should be clickable in current implementation.");

        _shiftPage.ClickSaveTemplate();
        var alertText = AcceptAlertAndGetTextIfPresent(10);

        Assert.That(alertText, Is.Not.Null.And.Not.Empty,
            "Expected a validation alert when saving with no assignments.");
        Assert.That(alertText, Does.Contain("Please assign at least one shift before saving."),
            "Unexpected validation message for empty save.");

        var saveError = _shiftPage.GetErrorAlertText();
        Assert.That(saveError, Is.Empty, "No server-side error banner expected for client-side empty-save validation.");
    }
}
