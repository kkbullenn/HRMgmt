using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.pages;

public class EmployeeShiftPage : BasePage
{
    private const string PagePath = "/Shift/EmployeeShift";

    private IWebElement EmployeeSelect => _wait.Until(d => d.FindElement(By.Id("employeeSelect")));

    public EmployeeShiftPage(IWebDriver driver) : base(driver)
    {
    }

    public void GoTo(string? baseUrl = null)
    {
        var trimmedBase = (baseUrl ?? BaseUrl).TrimEnd('/');
        _driver.Navigate().GoToUrl($"{trimmedBase}{PagePath}");
        WaitForPage();
    }

    public void WaitForPage()
    {
        _wait.Until(d => d.FindElement(By.Id("employeeSelect")));
        _wait.Until(d => d.FindElement(By.Id("calendar")));
    }

    public void SelectEmployee(string employeeId)
    {
        // Wait for the specific employee option to be available
        _wait.Until(_ => 
        {
            try
            {
                var select = new SelectElement(EmployeeSelect);
                var option = select.Options.FirstOrDefault(o => o.GetAttribute("value") == employeeId);
                return option != null;
            }
            catch
            {
                return false;
            }
        });

        var selectElement = new SelectElement(EmployeeSelect);
        selectElement.SelectByValue(employeeId);
        Thread.Sleep(1000); // Wait for calendar to refresh
    }

    public string GetSelectedEmployeeId()
    {
        var select = new SelectElement(EmployeeSelect);
        // SelectedOption is non-null according to SelectElement contract; coalesce attribute to empty string
        return select.SelectedOption.GetAttribute("value") ?? string.Empty;
    }

    public List<string> GetEmployeeOptions()
    {
        var select = new SelectElement(EmployeeSelect);
        return select.Options.Select(o => o.Text).ToList();
    }

    public List<(string value, string text)> GetEmployeeOptionDetails()
    {
        var select = new SelectElement(EmployeeSelect);
        return select.Options.Select(o => (o.GetAttribute("value") ?? string.Empty, o.Text)).ToList();
    }

    public void ClickDate(string dateStr)
    {
        // dateStr should be in format like "2026-02-20"
        var dayCell = _wait.Until(d => d.FindElement(By.CssSelector($"td[data-date='{dateStr}']")));
        ClickElement(dayCell);
        Thread.Sleep(300); // Wait for dropdown to appear
    }

    public void AssignShiftOnDate(string dateStr, string shiftId)
    {
        ClickDate(dateStr);

        // Wait for dropdown to appear
        var dropdown = _wait.Until(d => d.FindElement(By.Id("shiftDropdown")));
        var select = new SelectElement(dropdown);
        select.SelectByValue(shiftId);

        Thread.Sleep(500); // Wait for assignment to complete
    }

    // Helper: wait for one or more elements inside the td[data-date='...'] cell
    private IReadOnlyCollection<IWebElement> WaitForEventsInCell(string dateStr, string selector = "a.fc-event", int timeoutSec = 5)
    {
        try
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSec));
            return wait.Until(d =>
            {
                try
                {
                    var cell = d.FindElement(By.CssSelector($"td[data-date='{dateStr}']"));
                    var elems = cell.FindElements(By.CssSelector(selector));
                    return elems.Count > 0 ? elems : null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            });
        }
        catch (WebDriverTimeoutException)
        {
            return Array.Empty<IWebElement>();
        }
    }

    // Helper: wait for a single element inside the cell and return it (throws on timeout)
    private IWebElement WaitForEventInCell(string dateStr, string selector = ".fc-daygrid-event", int timeoutSec = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSec));
        return wait.Until(d =>
        {
            try
            {
                var cell = d.FindElement(By.CssSelector($"td[data-date='{dateStr}']"));
                var elem = cell.FindElements(By.CssSelector(selector)).FirstOrDefault();
                return elem != null ? elem : null;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        });
    }

    private static bool IsInteractable(IWebElement element)
    {
        return element.Displayed && element.Enabled && element.Size.Height > 0 && element.Size.Width > 0;
    }

    private IWebElement WaitForClickableEventInCell(string dateStr, int timeoutSec = 5)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSec));
        return wait.Until(d =>
        {
            try
            {
                var cell = d.FindElement(By.CssSelector($"td[data-date='{dateStr}']"));
                var candidates = cell.FindElements(By.CssSelector(".fc-daygrid-event, a.fc-event, .fc-event"));
                var clickable = candidates.FirstOrDefault(IsInteractable);
                if (clickable != null)
                {
                    return clickable;
                }

                var harness = cell.FindElements(By.CssSelector(".fc-daygrid-event-harness")).FirstOrDefault(IsInteractable);
                return harness != null ? harness : null;
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        });
    }

    public bool HasShiftOnDate(string dateStr)
    {
        var events = WaitForEventsInCell(dateStr);
        return events.Count > 0;
    }

    public int CountShiftsOnDate(string dateStr)
    {
        var events = WaitForEventsInCell(dateStr);
        return events.Count;
    }

    public void ClickShiftOnDate(string dateStr)
    {
        var evt = WaitForClickableEventInCell(dateStr);
        try
        {
            ClickElement(evt);
        }
        catch
        {
            var js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("arguments[0].click();", evt);
        }
        Thread.Sleep(500); // Increased wait for confirm dialog in CI
    }

    /// <summary>
    /// Deletes a shift assignment on the specified date.
    /// Handles the confirm/result alerts robustly for CI environments.
    /// </summary>
    public void DeleteShiftOnDate(string dateStr)
    {
        // First verify shift exists
        if (!HasShiftOnDate(dateStr))
        {
            throw new InvalidOperationException($"No shift found on date {dateStr} to delete");
        }

        ClickShiftOnDate(dateStr);

        // In CI/headless mode, alerts may appear with delay
        // Handle confirm alert ("Delete this shift assignment?")
        var confirmResult = TryAcceptAlert(TimeSpan.FromSeconds(5));
        
        if (!confirmResult.wasPresent)
        {
            // No confirm alert - maybe the click didn't register properly
            // Try clicking again with JavaScript
            try
            {
                var evt = WaitForClickableEventInCell(dateStr, timeoutSec: 3);
                var js = (IJavaScriptExecutor)_driver;
                js.ExecuteScript("arguments[0].click();", evt);
                Thread.Sleep(500);
                TryAcceptAlert(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore - shift may already be deleted or click handled differently
            }
        }

        // Handle success/failure result alert
        TryAcceptAlert(TimeSpan.FromSeconds(5));

        // Wait for calendar to refresh and shift to be removed
        Thread.Sleep(1000);

        // Verify deletion by checking DOM is updated
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        try
        {
            wait.Until(_ => !HasShiftOnDate(dateStr));
        }
        catch (WebDriverTimeoutException)
        {
            // Shift still showing - this will be caught by test assertion
            System.Diagnostics.Debug.WriteLine($"Warning: Shift on {dateStr} still visible after delete attempt");
        }
    }

    /// <summary>
    /// Attempts to accept an alert, returning info about whether it was present.
    /// </summary>
    private (bool wasPresent, string text) TryAcceptAlert(TimeSpan timeout)
    {
        try
        {
            var wait = new WebDriverWait(_driver, timeout);
            var alert = wait.Until(d => d.SwitchTo().Alert());
            var text = alert.Text ?? string.Empty;
            alert.Accept();
            return (true, text);
        }
        catch (WebDriverTimeoutException)
        {
            return (false, string.Empty);
        }
        catch (NoAlertPresentException)
        {
            return (false, string.Empty);
        }
    }

    public string GetAlertText()
    {
        try
        {
            var alert = _driver.SwitchTo().Alert();
            string text = alert.Text ?? string.Empty;
            alert.Accept();
            return text;
        }
        catch (NoAlertPresentException)
        {
            return string.Empty;
        }
    }

    public bool IsAlertPresent()
    {
        try
        {
            _driver.SwitchTo().Alert();
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
    }

    public void AcceptAlert()
    {
        var alert = _wait.Until(d => d.SwitchTo().Alert());
        alert.Accept();
        Thread.Sleep(300);
    }

    public void DismissAlert()
    {
        var alert = _wait.Until(d => d.SwitchTo().Alert());
        alert.Dismiss();
        Thread.Sleep(300);
    }

    public void NavigateToNextMonth()
    {
        var nextButton = _wait.Until(d => d.FindElement(By.CssSelector(".fc-next-button")));
        ClickElement(nextButton);
        Thread.Sleep(300);
    }

    public void NavigateToPreviousMonth()
    {
        var prevButton = _wait.Until(d => d.FindElement(By.CssSelector(".fc-prev-button")));
        ClickElement(prevButton);
        Thread.Sleep(300);
    }

    public string GetCurrentMonth()
    {
        var title = _wait.Until(d => d.FindElement(By.CssSelector(".fc-toolbar-title")));
        return title.Text;
    }

    public List<string> GetShiftNamesOnDate(string dateStr)
    {
        var shiftNames = new List<string>();
        var events = WaitForEventsInCell(dateStr, ".fc-daygrid-event");

        foreach (var evt in events)
        {
            var title = evt.Text.Trim();
            if (!string.IsNullOrWhiteSpace(title))
            {
                shiftNames.Add(title);
            }
        }

        return shiftNames;
    }

    public void Refresh()
    {
        _driver.Navigate().Refresh();
        WaitForPage();
        Thread.Sleep(500); // Wait for calendar to load
    }
}
