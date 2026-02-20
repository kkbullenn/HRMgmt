using System.Text.Json;
using HRMgmtTest.pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.tests.blackbox;

public class TC013_CrossPageConsistencyTests : BlackboxTestBase
{
    private const string ShiftD1Id = "22222222-2222-2222-2222-000000000001";
    private const string ShiftD2Id = "22222222-2222-2222-2222-000000000002";
    private const string ShiftD7Id = "22222222-2222-2222-2222-000000000007";
    private const string ShiftD1Token = "D1 Morning";
    private const string ShiftD2Token = "D2 Day";
    private const string ShiftD7Token = "D7 Early";
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

    [Test]
    public void TC013_TemplateAndEmployeeCalendar_MappingConsistent()
    {
        LoginAsAdminIfCredentialsExist();

        _shiftPage.GoTo(BaseUrl);
        var rowCount = _shiftPage.GetEmployeeCount();
        Assert.That(rowCount, Is.GreaterThanOrEqualTo(3), "TC013 requires at least 3 employees.");

        var e1 = GetEmployeeId(0);
        var e2 = GetEmployeeId(1);
        var e3 = GetEmployeeId(2);

        AssertShiftOptionExists(0, 0, ShiftD1Id); // Monday
        AssertShiftOptionExists(1, 1, ShiftD2Id); // Tuesday
        AssertShiftOptionExists(2, 2, ShiftD7Id); // Wednesday

        ClearAssignmentsOnTargetDates(e1, e2, e3);

        _shiftPage.GoTo(BaseUrl);
        _shiftPage.ClickCreateNewTemplate();
        var templateName = BuildTemplateName("CrossPageMappingTemplate");
        _createdTemplate = templateName;
        _shiftPage.SetTemplateName(templateName);
        _shiftPage.SetWeekType("1");

        _shiftPage.SetShiftCellValue(0, 0, ShiftD1Id);
        _shiftPage.SetShiftCellValue(1, 1, ShiftD2Id);
        _shiftPage.SetShiftCellValue(2, 2, ShiftD7Id);
        SyncHiddenGridValue(0, 0, ShiftD1Id);
        SyncHiddenGridValue(1, 1, ShiftD2Id);
        SyncHiddenGridValue(2, 2, ShiftD7Id);

        _shiftPage.ClickSaveTemplate(waitForMenu: false);
        Assert.That(_shiftPage.GetErrorAlertText(), Is.Empty, "Template save failed.");

        _shiftPage.SetAssignmentStart(TargetMonday);
        _shiftPage.SetAssignmentEnd(TargetWednesday);
        _shiftPage.ClickGenerateSchedule();
        var generateAlert = _shiftPage.WaitForBrowserAlertText();
        Assert.That(generateAlert, Does.Not.Contain("error").IgnoreCase,
            $"Generate returned error alert: {generateAlert}");

        var e1Events = FetchEmployeeEvents(e1);
        var e2Events = FetchEmployeeEvents(e2);
        var e3Events = FetchEmployeeEvents(e3);

        Assert.That(HasExpectedEvent(e1Events, TargetMonday, ShiftD1Token), Is.True,
            $"E1 expected Monday shift token '{ShiftD1Token}' not found.");
        Assert.That(HasExpectedEvent(e2Events, TargetTuesday, ShiftD2Token), Is.True,
            $"E2 expected Tuesday shift token '{ShiftD2Token}' not found.");
        Assert.That(HasExpectedEvent(e3Events, TargetWednesday, ShiftD7Token), Is.True,
            $"E3 expected Wednesday shift token '{ShiftD7Token}' not found.");
    }

    private string GetEmployeeId(int rowIndex)
    {
        return Wait.Until(d => d.FindElement(By.CssSelector($"input[name='Users[{rowIndex}].UserId']")))
            .GetAttribute("value") ?? string.Empty;
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

    private static string BuildTemplateName(string prefix)
    {
        var token = new string(Guid.NewGuid().ToString("N").Where(char.IsLetter).Take(10).ToArray());
        if (string.IsNullOrWhiteSpace(token))
        {
            token = "alpha";
        }

        return $"{prefix}_{token}";
    }

    private void ClearAssignmentsOnTargetDates(params string[] employeeIds)
    {
        _employeeShiftPage.GoTo(BaseUrl);
        var dates = new[] { TargetMonday, TargetTuesday, TargetWednesday };

        foreach (var employeeId in employeeIds)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                continue;
            }

            _employeeShiftPage.SelectEmployee(employeeId);
            foreach (var date in dates)
            {
                while (_employeeShiftPage.HasShiftOnDate(date))
                {
                    _employeeShiftPage.DeleteShiftOnDate(date);
                }
            }
        }

        _shiftPage.GoTo(BaseUrl);
    }

    private List<EmployeeShiftEvent> FetchEmployeeEvents(string employeeId)
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/Shift/GetEmployeeShifts?employeeId={employeeId}");
        var payload = Driver.FindElement(By.TagName("body")).Text;
        var events = JsonSerializer.Deserialize<List<EmployeeShiftEvent>>(payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return events ?? new List<EmployeeShiftEvent>();
    }

    private static bool HasExpectedEvent(IEnumerable<EmployeeShiftEvent> events, string yyyyMmDd, string shiftToken)
    {
        foreach (var e in events)
        {
            if (string.IsNullOrWhiteSpace(e.Start) || e.Start.Length < 10)
            {
                continue;
            }

            if (!e.Start.StartsWith(yyyyMmDd, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(shiftToken))
            {
                return true;
            }

            if ((e.Title ?? string.Empty).Contains(shiftToken, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class EmployeeShiftEvent
    {
        public string Title { get; set; } = string.Empty;
        public string Start { get; set; } = string.Empty;
    }
}
