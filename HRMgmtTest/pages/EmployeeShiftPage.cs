using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Collections.Generic;
using System.Linq;

namespace HRMgmtTest.pages;

public class EmployeeShiftPage : BasePage
{
    private const string PagePath = "/Shift/EmployeeShift";

    private IWebElement EmployeeSelect => _wait.Until(d => d.FindElement(By.Id("employeeSelect")));

    public EmployeeShiftPage(IWebDriver driver) : base(driver)
    {
    }

    public void GoTo(string baseUrl)
    {
        var trimmedBase = (baseUrl ?? string.Empty).TrimEnd('/');
        _driver.Navigate().GoToUrl($"{trimmedBase}{PagePath}");
        WaitForPage();
    }

    public void WaitForPage()
    {
        _wait.Until(d => d.FindElement(By.Id("employeeSelect")));
        _wait.Until(d => d.FindElement(By.Id("calendar")));
    }

    public void SelectEmployee(string name)
    {
        var select = new SelectElement(EmployeeSelect);
        select.SelectByText(name);

        WaitForEventsToLoad();
    }

    public void WaitForEventsToLoad()
    {
        Thread.Sleep(2500);
    }


    public string GetRenderedCalendarText()
    {
        try
        {
            var calendar = _driver.FindElement(By.Id("calendar"));
            return calendar.Text;
        }
        catch (NoSuchElementException)
        {
            return string.Empty;
        }
    }


    public string GetCalendarInnerHtml()
    {
        try
        {
            var js = (OpenQA.Selenium.IJavaScriptExecutor)_driver;
            return js.ExecuteScript("return document.getElementById('calendar').innerHTML;")?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public bool HasShiftOnDate(string date, string partialShiftName)
    {
        // Find day cell
        // FullCalendar structure: .fc-day[data-date='yyyy-mm-dd']
        try
        {
            var dayCell = _driver.FindElement(By.CssSelector($".fc-day[data-date='{date}']"));

            var events = _driver.FindElements(By.XPath($"//div[contains(@class,'fc-event')] | //div[contains(text(), '{partialShiftName}')]"));

             var cellText = dayCell.Text;
             if (cellText.Contains(partialShiftName, StringComparison.OrdinalIgnoreCase)) return true;
             
             return false;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }
    
    public IReadOnlyList<string> GetEventsForDate(string date)
    {
         // Try to find the cell
         try {
             var dayCell = _driver.FindElement(By.CssSelector($".fc-day[data-date='{date}']"));
             return new List<string> { dayCell.Text }; // Rough approximation
         } catch { return new List<string>(); }
    }
}
