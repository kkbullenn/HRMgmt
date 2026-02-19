using OpenQA.Selenium;

namespace HRMgmtTest.pages;

public class ShiftPage : BasePage
{
    private const string CreatePagePath = "/Shift/Create";
    private const string IndexPagePath = "/Shift";

    private IWebElement NameInput => _wait.Until(d => d.FindElement(By.Id("Name")));
    private IWebElement StartTimeInput => _wait.Until(d => d.FindElement(By.Id("StartTime")));
    private IWebElement EndTimeInput => _wait.Until(d => d.FindElement(By.Id("EndTime")));
    private IWebElement RequiredCountInput => _wait.Until(d => d.FindElement(By.Id("RequiredCount")));
    private IWebElement StartDateInput => _wait.Until(d => d.FindElement(By.Id("StartDate")));
    private IWebElement EndDateInput => _wait.Until(d => d.FindElement(By.Id("EndDate")));
    private IWebElement CreateButton => _wait.Until(d => d.FindElement(By.CssSelector("input[type='submit']")));

    public ShiftPage(IWebDriver driver) : base(driver)
    {
    }

    public void GoToCreate(string baseUrl)
    {
        var trimmedBase = (baseUrl ?? string.Empty).TrimEnd('/');
        _driver.Navigate().GoToUrl($"{trimmedBase}{CreatePagePath}");
    }

    public void CreateShift(string name, string startTime, string endTime, int requiredCount)
    {
        NameInput.SendKeys(name);
        
        // Time inputs might need specific format depending on browser/locale, usually HH:mm
        // Clearing first is good practice
        StartTimeInput.SendKeys(startTime);
        EndTimeInput.SendKeys(endTime);
        
        RequiredCountInput.Clear();
        RequiredCountInput.SendKeys(requiredCount.ToString());

        var today = DateTime.Today.ToString("yyyy-MM-dd");
        SetDateValue(StartDateInput, today);
        SetDateValue(EndDateInput, today);

        ClickElement(CreateButton);
        _wait.Until(d => d.Url.EndsWith(IndexPagePath, StringComparison.OrdinalIgnoreCase) 
                         || d.Url.EndsWith("/Shift/", StringComparison.OrdinalIgnoreCase));
    }
    public void SetStartDate(string date) => SetDateValue(StartDateInput, date);
    public void SetEndDate(string date) => SetDateValue(EndDateInput, date);
    public void SetStartTime(string time) 
    {
        StartTimeInput.Clear();
        StartTimeInput.SendKeys(time);
    }
    public void SetEndTime(string time) 
    {
        EndTimeInput.Clear();
        EndTimeInput.SendKeys(time);
    }

    public void ClickCreateExpectFailure()
    {
        ClickElement(CreateButton);
        // Wait specifically for validation errors or stay on page
        // If validation is client-side, it's fast. If server-side, it reloads.
        // We can wait for the URL *not* to change, or better, wait for a validation span to have text.
        // For now, simple wait.
        Thread.Sleep(500); // Short wait for JS/postback
    }

    public string GetStartTimeError() => GetValidationError("StartTime");
    public string GetEndTimeError() => GetValidationError("EndTime");
    public string GetStartDateError() => GetValidationError("StartDate");
    public string GetEndDateError() => GetValidationError("EndDate");
    public string GetGeneralError() 
    {
         try {
            return _driver.FindElement(By.CssSelector(".alert.alert-danger")).Text;
         } catch (NoSuchElementException) { return string.Empty; }
    }

    private string GetValidationError(string fieldName)
    {
        try
        {
            // asp-validation-for generates data-valmsg-for field name
            var span = _driver.FindElement(By.CssSelector($"span[data-valmsg-for='{fieldName}']"));
            return span.Text;
        }
        catch (NoSuchElementException)
        {
            return string.Empty;
        }
    }
}
