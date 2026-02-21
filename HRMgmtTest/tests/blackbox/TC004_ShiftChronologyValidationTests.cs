using OpenQA.Selenium;

namespace HRMgmtTest.tests.blackbox;

public class TC004_ShiftChronologyValidationTests : BlackboxTestBase
{
    [Explicit("Known QA finding: invalid shift chronology/duration is accepted in current build.")]
    [Test]
    public void TC004_InvalidShiftChronologyAndDuration_IsRejectedExpected()
    {
        LoginAsAdminIfCredentialsExist();

        var defects = new List<string>();

        var case1 = SubmitShiftCreateForm(
            BuildShiftName("ChronologyInvalidShift"),
            requiredCount: "1",
            startDate: "2026-02-01",
            endDate: "2026-01-31",
            startTime: "14:00",
            endTime: "13:00");

        if (!case1.blocked)
        {
            defects.Add(
                $"Case1 accepted invalid chronology (end<start and startTime>endTime). " +
                $"Url='{case1.url}', Error='{case1.errorText}'.");
        }

        var case2 = SubmitShiftCreateForm(
            BuildShiftName("ZeroDurationShift"),
            requiredCount: "1",
            startDate: "2026-02-02",
            endDate: "2026-02-02",
            startTime: "09:00",
            endTime: "09:00");

        if (!case2.blocked)
        {
            defects.Add(
                $"Case2 accepted zero-duration shift (startTime==endTime). " +
                $"Url='{case2.url}', Error='{case2.errorText}'.");
        }

        Assert.That(defects, Is.Empty,
            "Invalid shift chronology/duration should be rejected. " + string.Join(" | ", defects));
    }

    private (bool blocked, string url, string errorText) SubmitShiftCreateForm(
        string name,
        string requiredCount,
        string startDate,
        string endDate,
        string startTime,
        string endTime)
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/Shift/Create");
        Wait.Until(d => d.FindElement(By.Id("Name")));

        Driver.FindElement(By.Id("Name")).Clear();
        Driver.FindElement(By.Id("Name")).SendKeys(name);

        var required = Driver.FindElement(By.Id("RequiredCount"));
        required.Clear();
        required.SendKeys(requiredCount);

        SetInputByJs("StartDate", startDate);
        SetInputByJs("EndDate", endDate);
        SetInputByJs("StartTime", startTime);
        SetInputByJs("EndTime", endTime);

        var submit = Driver.FindElement(By.CssSelector("button[type='submit']"));
        try
        {
            submit.Click();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", submit);
        }

        Thread.Sleep(500);
        var currentUrl = Driver.Url;
        var errorText = string.Join(" ",
            Driver.FindElements(By.CssSelector(".text-danger, .alert.alert-danger"))
                .Select(e => e.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t)));

        var stayedOnCreate = currentUrl.Contains("/Shift/Create", StringComparison.OrdinalIgnoreCase);
        var blocked = stayedOnCreate || !string.IsNullOrWhiteSpace(errorText);

        return (blocked, currentUrl, errorText);
    }

    private void SetInputByJs(string id, string value)
    {
        ((IJavaScriptExecutor)Driver).ExecuteScript(
            "const el=document.getElementById(arguments[0]); if(el){ el.value=arguments[1]; el.dispatchEvent(new Event('input',{bubbles:true})); el.dispatchEvent(new Event('change',{bubbles:true})); }",
            id, value);
    }

    private static string BuildShiftName(string prefix)
    {
        var token = new string(Guid.NewGuid().ToString("N").Where(char.IsLetter).Take(8).ToArray());
        if (string.IsNullOrWhiteSpace(token))
        {
            token = "alpha";
        }

        return $"{prefix}_{token}";
    }
}
