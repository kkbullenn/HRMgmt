using HRMgmtTest.pages;
using OpenQA.Selenium;

namespace HRMgmtTest;

public class TC010_DuplicateTemplateNameRejectedTests : BlackboxTestBase
{
    private ShiftAssignmentPage _shiftPage = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _shiftPage = new ShiftAssignmentPage(Driver);
    }

    [Test]
    public void TC010_CreateTemplateWithDuplicateName_IsRejected()
    {
        LoginAsAdminIfCredentialsExist();

        _shiftPage.GoTo(BaseUrl);
        var existingTemplates = _shiftPage.GetTemplateNames();
        if (existingTemplates.Count == 0)
        {
            Assert.Ignore("TC010 requires at least one existing template name in the menu.");
        }
        var duplicateName = existingTemplates[0];

        _shiftPage.ClickCreateNewTemplate();
        _shiftPage.SetTemplateName(duplicateName);

        // Trigger blur/input validation in current implementation.
        Wait.Until(d => d.FindElement(By.Id("weekTypeInput"))).Click();
        var duplicateAlert = AcceptAlertAndGetTextIfPresent(10);

        Assert.That(duplicateAlert, Is.Not.Null.And.Not.Empty, "Expected duplicate-name validation alert.");
        Assert.That(duplicateAlert, Does.Contain("Template name already exists"),
            "Unexpected duplicate-name validation message.");
    }
}
