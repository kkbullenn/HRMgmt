using HRMgmtTest.pages;
using OpenQA.Selenium;

namespace HRMgmtTest.tests.blackbox;

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
        var beforeTemplateCount = existingTemplates.Count;
        if (existingTemplates.Count == 0)
        {
            Assert.Ignore("TC010 requires at least one existing template name in the menu.");
        }
        var duplicateName = existingTemplates.FirstOrDefault(t =>
            t.Contains("QA_WEEKLY_BASE", StringComparison.OrdinalIgnoreCase)) ?? existingTemplates[0];

        _shiftPage.ClickCreateNewTemplate();
        _shiftPage.SetTemplateName(duplicateName);
        var immediateDuplicateAlert = AcceptAlertAndGetTextIfPresent(3);
        if (!string.IsNullOrWhiteSpace(immediateDuplicateAlert) &&
            immediateDuplicateAlert.Contains("Template name already exists", StringComparison.OrdinalIgnoreCase))
        {
            _shiftPage.GoTo(BaseUrl);
            var afterImmediateCount = _shiftPage.GetTemplateNames().Count;
            Assert.That(afterImmediateCount, Is.EqualTo(beforeTemplateCount),
                "Duplicate-name attempt should not create a new template.");
            return;
        }

        // Current save flow validates "has assignment" before duplicate-name server submit.
        // Assign at least one cell so duplicate validation can be reached reliably.
        var (rowIndexText, _) = GetFirstEmployeeFromGrid();
        var rowIndex = int.Parse(rowIndexText);
        const int mondayColumn = 0;
        var (_, shiftLabel, _) = GetFirstShiftOption(rowIndex, mondayColumn);
        SelectShiftByLabel(rowIndex, mondayColumn, shiftLabel);

        // Capture duplicate signal using the current UI path (save click may trigger browser alert).
        var duplicateAlert = AcceptAlertAndGetTextIfPresent(4);
        if (string.IsNullOrWhiteSpace(duplicateAlert))
        {
            try
            {
                _shiftPage.ClickSaveTemplate();
            }
            catch (UnhandledAlertException)
            {
                // Expected when duplicate-name alert is raised by the page.
            }

            duplicateAlert = AcceptAlertAndGetTextIfPresent(8);
        }

        var serverError = _shiftPage.GetErrorAlertText();

        var hasDuplicateSignal =
            (!string.IsNullOrWhiteSpace(duplicateAlert) &&
             duplicateAlert.Contains("Template name already exists", StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(serverError) &&
                serverError.Contains("already exists", StringComparison.OrdinalIgnoreCase));

        Assert.That(hasDuplicateSignal, Is.True,
            $"Expected duplicate-name validation via alert or server error. Alert='{duplicateAlert}', Error='{serverError}'");

        _shiftPage.GoTo(BaseUrl);
        var afterTemplateCount = _shiftPage.GetTemplateNames().Count;
        Assert.That(afterTemplateCount, Is.EqualTo(beforeTemplateCount),
            "Duplicate-name attempt should not create a new template.");
    }
}
