using HRMgmtTest.pages;
using OpenQA.Selenium;

namespace HRMgmtTest;

public class TC009_EmptySaveIsBlockedTests : BlackboxTestBase
{
    private ShiftAssignmentPage _shiftPage = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _shiftPage = new ShiftAssignmentPage(Driver);
    }

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
