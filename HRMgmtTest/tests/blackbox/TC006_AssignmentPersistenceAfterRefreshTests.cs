using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.tests.blackbox;

public class TC006_AssignmentPersistenceAfterRefreshTests : BlackboxTestBase
{
    private const string ShiftD1Id = "22222222-2222-2222-2222-000000000001";
    private const string ShiftD2Id = "22222222-2222-2222-2222-000000000002";
    private const string ShiftD7Id = "22222222-2222-2222-2222-000000000007";
    private const string TargetMonday = "2026-02-16";
    private const string TargetTuesday = "2026-02-17";
    private const string TargetWednesday = "2026-02-18";

    private ShiftAssignmentPage _shiftPage = null!;
    private EmployeeShiftPage _employeeShiftPage = null!;
    private string? _createdTemplate;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _shiftPage = new ShiftAssignmentPage(Driver);
        _employeeShiftPage = new EmployeeShiftPage(Driver);
        _createdTemplate = null;
    }

    [TearDown]
    public override void TearDown()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_createdTemplate))
            {
                _shiftPage.GoTo(BaseUrl);
                _shiftPage.SelectTemplateFromMenu(_createdTemplate);
                _shiftPage.ClickDeleteTemplate();
            }
        }
        catch
        {
            // Best effort cleanup only.
        }

        base.TearDown();
    }

    [Explicit("Known QA finding: Monday generated shift is missing in current build.")]
    [Test]
    public void TC006_AssignmentsPersist_AfterRefreshAndNavigation()
    {
        LoginAsAdminIfCredentialsExist();

        _shiftPage.GoTo(BaseUrl);

        var employeeId = Wait.Until(d => d.FindElement(By.CssSelector("input[name='Users[0].UserId']")))
            .GetAttribute("value") ?? string.Empty;
        Assert.That(string.IsNullOrWhiteSpace(employeeId), Is.False, "Missing employee id for row 0.");

        AssertShiftOptionExists(0, 0, ShiftD1Id);
        AssertShiftOptionExists(0, 1, ShiftD2Id);
        AssertShiftOptionExists(0, 2, ShiftD7Id);

        ClearAssignmentsOnTargetDates(employeeId);

        _shiftPage.GoTo(BaseUrl);
        _shiftPage.ClickCreateNewTemplate();

        var templateName = BuildTemplateName("PersistenceTemplate");
        _createdTemplate = templateName;
        _shiftPage.SetTemplateName(templateName);
        _shiftPage.SetWeekType("1");

        _shiftPage.SetShiftCellValue(0, 0, ShiftD1Id);
        _shiftPage.SetShiftCellValue(0, 1, ShiftD2Id);
        _shiftPage.SetShiftCellValue(0, 2, ShiftD7Id);
        SyncHiddenGridValue(0, 0, ShiftD1Id);
        SyncHiddenGridValue(0, 1, ShiftD2Id);
        SyncHiddenGridValue(0, 2, ShiftD7Id);

        _shiftPage.ClickSaveTemplate(waitForMenu: false);
        var saveError = _shiftPage.GetErrorAlertText();
        Assert.That(saveError, Is.Empty, $"Save failed: {saveError}");

        _shiftPage.SetAssignmentStart(TargetMonday);
        _shiftPage.SetAssignmentEnd(TargetWednesday);
        _shiftPage.ClickGenerateSchedule();
        var generateAlert = _shiftPage.WaitForBrowserAlertText();
        Assert.That(generateAlert, Does.Not.Contain("error").IgnoreCase,
            $"Generate returned error alert: {generateAlert}");

        Driver.Navigate().Refresh();
        _shiftPage.WaitForPage();
        _shiftPage.SelectTemplateFromMenu(templateName);

        Assert.That(_shiftPage.GetShiftCellValue(0, 0), Is.EqualTo(ShiftD1Id));
        Assert.That(_shiftPage.GetShiftCellValue(0, 1), Is.EqualTo(ShiftD2Id));
        Assert.That(_shiftPage.GetShiftCellValue(0, 2), Is.EqualTo(ShiftD7Id));

        Driver.Navigate().GoToUrl($"{BaseUrl}/Home/Index");
        _shiftPage.GoTo(BaseUrl);
        _shiftPage.SelectTemplateFromMenu(templateName);

        Assert.That(_shiftPage.GetShiftCellValue(0, 0), Is.EqualTo(ShiftD1Id));
        Assert.That(_shiftPage.GetShiftCellValue(0, 1), Is.EqualTo(ShiftD2Id));
        Assert.That(_shiftPage.GetShiftCellValue(0, 2), Is.EqualTo(ShiftD7Id));

        _employeeShiftPage.GoTo(BaseUrl);
        _employeeShiftPage.SelectEmployee(employeeId);
        Assert.That(_employeeShiftPage.HasShiftOnDate(TargetMonday), Is.True, "Missing Monday generated shift.");
        Assert.That(_employeeShiftPage.HasShiftOnDate(TargetTuesday), Is.True, "Missing Tuesday generated shift.");
        Assert.That(_employeeShiftPage.HasShiftOnDate(TargetWednesday), Is.True, "Missing Wednesday generated shift.");
    }

    private void AssertShiftOptionExists(int rowIndex, int colIndex, string shiftId)
    {
        var select = new SelectElement(Wait.Until(d =>
            d.FindElement(By.CssSelector($".shift-dropdown[data-row='{rowIndex}'][data-col='{colIndex}']"))));
        var optionExists = select.Options.Any(o => string.Equals(o.GetAttribute("value"), shiftId, StringComparison.OrdinalIgnoreCase));
        Assert.That(optionExists, Is.True, $"Expected shift option '{shiftId}' for row={rowIndex}, col={colIndex}");
    }

    private void SyncHiddenGridValue(int rowIndex, int colIndex, string shiftValue)
    {
        var gridIndex = rowIndex * 7 + colIndex;
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "const hidden=document.querySelector(\"input[name='Grid[" + gridIndex + "]']\"); if(hidden){ hidden.value=arguments[0]; }",
            shiftValue);
    }

    private void ClearAssignmentsOnTargetDates(string employeeId)
    {
        _employeeShiftPage.GoTo(BaseUrl);
        _employeeShiftPage.SelectEmployee(employeeId);

        var dates = new[] { TargetMonday, TargetTuesday, TargetWednesday };
        foreach (var date in dates)
        {
            while (_employeeShiftPage.HasShiftOnDate(date))
            {
                _employeeShiftPage.DeleteShiftOnDate(date);
            }
        }

        _shiftPage.GoTo(BaseUrl);
    }

    private static string BuildTemplateName(string prefix)
    {
        var token = new string(Guid.NewGuid().ToString("N").Where(char.IsLetter).Take(10).ToArray());
        if (string.IsNullOrWhiteSpace(token))
        {
            token = "alpha";
        }

        return $"{prefix}_{token}";
    }
}
