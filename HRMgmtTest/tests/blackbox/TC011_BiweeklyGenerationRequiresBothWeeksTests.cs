using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest;

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

        // Save redirects to page default state; reload saved template before generate.
        _shiftPage.GoTo(BaseUrl);
        _shiftPage.SelectTemplateFromMenu(templateName);
        _shiftPage.WaitForTemplateName(templateName);

        _shiftPage.ClickGenerateSchedule();
        var generateAlert = AcceptAlertAndGetTextIfPresent(10);

        Assert.That(generateAlert, Is.Not.Null.And.Not.Empty,
            "Expected generation to be blocked with validation alert.");
        Assert.That(generateAlert, Does.Contain("Biweekly template must include both Week 1 and Week 2"),
            $"Unexpected biweekly validation message: {generateAlert}");
    }
}
