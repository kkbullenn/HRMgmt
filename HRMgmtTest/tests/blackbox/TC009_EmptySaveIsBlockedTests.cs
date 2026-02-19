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
        _shiftPage.ClickCreateNewTemplate();

        var templateName = $"WK_TC009_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}".Substring(0, 32);
        _shiftPage.SetTemplateName(templateName);

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
