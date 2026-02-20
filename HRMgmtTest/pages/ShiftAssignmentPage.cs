using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.pages;

public class ShiftAssignmentPage : BasePage
{
    private const string PagePath = "/Shift/ShiftAssignment";

    private IWebElement TemplateNameInput => _wait.Until(d => d.FindElement(By.Name("templateName")));
    private IWebElement WeekTypeSelect => _wait.Until(d => d.FindElement(By.Id("weekTypeInput")));
    private IWebElement WeekIndexSelect => _wait.Until(d => d.FindElement(By.Id("weekIndexInput")));
    private IWebElement AssignmentStartInput => _wait.Until(d => d.FindElement(By.Id("assignStart")));
    private IWebElement AssignmentEndInput => _wait.Until(d => d.FindElement(By.Id("assignEnd")));
    private IWebElement SaveTemplateButton => _wait.Until(d => d.FindElement(By.Id("saveTemplateBtn")));
    private IWebElement AutoAssignButton => _wait.Until(d => d.FindElement(By.Id("autoAssignBtn")));
    private IWebElement DeleteTemplateButton => _wait.Until(d => d.FindElement(By.Id("deleteTemplateBtn")));

    private IWebElement ClearGridButton =>
        _wait.Until(d => d.FindElement(By.XPath("//button[contains(@onclick,'clearCurrentWeekGrid')]")));

    public ShiftAssignmentPage(IWebDriver driver) : base(driver)
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
        _wait.Until(d => d.FindElement(By.Id("templateNameInput")));
    }

    public void SetTemplateName(string name)
    {
        var input = TemplateNameInput;
        input.Clear();
        input.SendKeys(name);
    }

    public string GetTemplateName()
    {
        return TemplateNameInput.GetAttribute("value") ?? string.Empty;
    }

    public void SetWeekType(string value)
    {
        var select = new SelectElement(WeekTypeSelect);
        select.SelectByValue(value);
    }

    public string GetWeekType()
    {
        var select = new SelectElement(WeekTypeSelect);
        return select.SelectedOption?.GetAttribute("value") ?? string.Empty;
    }

    public void SetWeekIndex(string value)
    {
        var select = new SelectElement(WeekIndexSelect);
        select.SelectByValue(value);
    }

    public string GetWeekIndex()
    {
        var select = new SelectElement(WeekIndexSelect);
        return select.SelectedOption?.GetAttribute("value") ?? string.Empty;
    }

    public void SetAssignmentStart(string yyyyMmDd)
    {
        SetDateValue(AssignmentStartInput, yyyyMmDd);
    }

    public string GetAssignmentStart()
    {
        return AssignmentStartInput.GetAttribute("value") ?? string.Empty;
    }

    public void SetAssignmentEnd(string yyyyMmDd)
    {
        SetDateValue(AssignmentEndInput, yyyyMmDd);
    }

    public string GetAssignmentEnd()
    {
        return AssignmentEndInput.GetAttribute("value") ?? string.Empty;
    }

    /// <summary>
    /// Clicks Save Template and waits for confirmation.
    /// </summary>
    /// <param name="waitForMenu">If true, also waits for the template to appear in the menu.</param>
    public void ClickSaveTemplate(bool waitForMenu = true)
    {
        // Get template name before save for menu verification
        var templateName = GetTemplateName();
        
        ClickElement(SaveTemplateButton);
        
        // Wait for success alert which indicates save completed
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        try
        {
            wait.Until(d =>
            {
                var alerts = d.FindElements(By.CssSelector(".alert.alert-success"));
                return alerts.Any(a => a.Displayed && !string.IsNullOrWhiteSpace(a.Text));
            });
        }
        catch (WebDriverTimeoutException)
        {
            // Check if there's an error alert instead
            var errorAlerts = _driver.FindElements(By.CssSelector(".alert.alert-danger"));
            var errorText = errorAlerts.FirstOrDefault()?.Text ?? "No error message";
            throw new Exception($"Save template failed. Error: {errorText}");
        }

        // Allow DOM updates to settle
        System.Threading.Thread.Sleep(500);

        // If requested, verify template appears in menu
        if (waitForMenu && !string.IsNullOrWhiteSpace(templateName))
        {
            try
            {
                wait.Until(d => FindTemplateMenuItem(d, templateName) != null);
            }
            catch (WebDriverTimeoutException)
            {
                // Template not in menu after save - this might be OK if menu needs refresh
                System.Diagnostics.Debug.WriteLine($"Template '{templateName}' not yet visible in menu after save");
            }
        }
    }

    public void ClickGenerateSchedule()
    {
        ClickElement(AutoAssignButton);
        System.Threading.Thread.Sleep(1000); // Wait for generation
    }

    public void ClickDeleteTemplate()
    {
        ClickElement(DeleteTemplateButton);
        // Wait for confirmation alert and accept to confirm deletion
        var deleteAlert = _wait.Until(d => d.SwitchTo().Alert());
        deleteAlert.Accept();
        System.Threading.Thread.Sleep(500); // Wait for deletion to complete

        var deleteAcceptAlert = _wait.Until(d => d.SwitchTo().Alert());
        deleteAcceptAlert.Accept();
    }

    public void ClickClearGrid()
    {
        ClickElement(ClearGridButton);
    }

    /// <summary>
    /// Selects a template from the left-hand menu by name.
    /// Includes retry logic with page refresh for CI stability.
    /// </summary>
    public void SelectTemplateFromMenu(string templateName)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
        IWebElement? item = null;
        const int maxAttempts = 3;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                // Log available templates for debugging
                var availableTemplates = GetTemplateNames();
                System.Diagnostics.Debug.WriteLine(
                    $"[Attempt {attempt}] Looking for template '{templateName}'. Available: [{string.Join(", ", availableTemplates)}]");

                item = wait.Until(d => FindTemplateMenuItem(d, templateName));
                if (item != null) break;
            }
            catch (WebDriverTimeoutException)
            {
                if (attempt < maxAttempts)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Template '{templateName}' not found on attempt {attempt}. Refreshing page...");
                    _driver.Navigate().Refresh();
                    WaitForPage();
                    System.Threading.Thread.Sleep(500);
                }
                else
                {
                    // Final attempt failed - log diagnostics and throw
                    var available = GetTemplateNames();
                    throw new WebDriverTimeoutException(
                        $"Template '{templateName}' not found in menu after {maxAttempts} attempts. " +
                        $"Available templates: [{string.Join(", ", available)}]");
                }
            }
        }

        if (item == null)
        {
            throw new NoSuchElementException($"Template menu item '{templateName}' not found");
        }

        // Use JavaScript click for reliability in headless mode
        try
        {
            ClickElement(item);
        }
        catch
        {
            var js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("arguments[0].click();", item);
        }

        // Wait for template to be fully loaded
        wait.Until(d =>
        {
            var activeName = GetActiveTemplateNameFromDriver(d);
            if (activeName.Equals(templateName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var currentName = d.FindElement(By.Name("templateName")).GetAttribute("value") ?? string.Empty;
            return currentName.Trim().Equals(templateName, StringComparison.OrdinalIgnoreCase);
        });

        // Allow any animations/transitions to complete
        System.Threading.Thread.Sleep(300);
    }

    public void WaitForTemplateToLoad(string templateName)
    {
        // Wait for template list item to have "active" class
        _wait.Until(d =>
        {
            var element =
                d.FindElement(By.XPath($"//ul[@id='templateMenu']/li[normalize-space()='{templateName}']"));
            return element.GetAttribute("class").Contains("active");
        });
    }

    public IReadOnlyList<string> GetTemplateNames()
    {
        var items = _driver.FindElements(By.CssSelector("#templateMenu li"));
        return items
            .Select(item => (item.Text ?? string.Empty).Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
    }

    public string GetActiveTemplateName()
    {
        var items = _driver.FindElements(By.CssSelector("#templateMenu li.active"));
        var item = items.FirstOrDefault();
        return item?.Text.Trim() ?? string.Empty;
    }

    public void ClickCreateNewTemplate()
    {
        var link = _wait.Until(d =>
            d.FindElement(
                By.XPath("//div[contains(@class,'assigngrid-menu')]//a[contains(@onclick,'createNewTemplate')]")));
        ClickElement(link);
    }

    public bool IsDeleteTemplateEnabled()
    {
        return DeleteTemplateButton.Enabled;
    }

    public string GetTemplateModeTitle()
    {
        var title = _wait.Until(d => d.FindElement(By.Id("templatePageModeTitle")));
        return title.Text.Trim();
    }

    public string GetSuccessAlertText()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        var alert = wait.Until(d =>
        {
            var elem = d.FindElements(By.CssSelector(".alert.alert-success")).FirstOrDefault();
            return !string.IsNullOrWhiteSpace(elem?.Text) ? elem : null;
        });

        return alert.Text.Trim();
    }

    public string GetErrorAlertText()
    {
        var alerts = _driver.FindElements(By.CssSelector(".alert.alert-danger"));
        return alerts.FirstOrDefault()?.Text.Trim() ?? string.Empty;
    }

    public void WaitForTemplateName(string templateName)
    {
        _wait.Until(d => (TemplateNameInput.GetAttribute("value") ?? string.Empty) == templateName);
    }

    public string GetWeekHeaderText()
    {
        var header = _wait.Until(d => d.FindElement(By.Id("gridWeekHeader")));
        return header.Text.Trim();
    }

    public string GetEmployeeNameByIndex(int rowIndex)
    {
        var empCell = _wait.Until(d => d.FindElement(By.CssSelector($"tr[data-row='{rowIndex}'] .emp-name")));
        return empCell.Text.Trim();
    }

    public int GetEmployeeCount()
    {
        var rows = _driver.FindElements(By.CssSelector("tr[data-row]"));
        return rows.Count;
    }

    public bool IsRowSelected(int rowIndex)
    {
        var checkbox = GetRowCheckbox(rowIndex);
        return checkbox.Selected;
    }

    public IReadOnlyList<int> GetSelectedRowIndices()
    {
        var checkboxes = _driver.FindElements(By.CssSelector(".row-select:checked"));
        return checkboxes
            .Select(cb => cb.GetAttribute("data-row"))
            .Where(attr => !string.IsNullOrWhiteSpace(attr))
            .Select(attr => int.TryParse(attr, out var idx) ? idx : -1)
            .Where(idx => idx >= 0)
            .ToList();
    }

    public string GetBatchShiftValue(int columnIndex)
    {
        var select = new SelectElement(GetBatchSelect(columnIndex));
        return select.SelectedOption?.GetAttribute("value") ?? string.Empty;
    }

    public void SetRowSelectedByIndex(int rowIndex, bool selected)
    {
        var checkbox = GetRowCheckbox(rowIndex);
        if (checkbox.Selected != selected)
        {
            ClickElement(checkbox);
        }
    }

    public void SetRowSelectedByName(string employeeName, bool selected)
    {
        var checkbox = _wait.Until(d =>
            d.FindElement(By.XPath(
                $"//tr[.//td[contains(@class,'emp-name') and normalize-space()='{employeeName}']]//input[contains(@class,'row-select')]")));
        if (checkbox.Selected != selected)
        {
            ClickElement(checkbox);
        }
    }

    public void SetShiftCellValue(int rowIndex, int columnIndex, string shiftId)
    {
        var select = new SelectElement(GetShiftCell(rowIndex, columnIndex));
        select.SelectByValue(shiftId ?? string.Empty);
    }

    public string GetShiftCellValue(int rowIndex, int columnIndex)
    {
        var dropdown = GetShiftCell(rowIndex, columnIndex);
        _wait.Until(d => dropdown.Displayed);

        var select = new SelectElement(dropdown);
        var value = select.SelectedOption?.GetAttribute("value") ?? string.Empty;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public void ApplyBatchShiftForColumn(int columnIndex, string shiftId)
    {
        var select = new SelectElement(GetBatchSelect(columnIndex));
        select.SelectByValue(shiftId ?? string.Empty);
        GetBatchApplyButton(columnIndex).Click();
    }

    private IWebElement GetShiftCell(int rowIndex, int columnIndex)
    {
        return _wait.Until(d =>
            d.FindElement(By.CssSelector($".shift-dropdown[data-row='{rowIndex}'][data-col='{columnIndex}']")));
    }

    private IWebElement GetRowCheckbox(int rowIndex)
    {
        return _wait.Until(d => d.FindElement(By.CssSelector($".row-select[data-row='{rowIndex}']")));
    }

    private IWebElement GetBatchSelect(int columnIndex)
    {
        return _wait.Until(d => d.FindElement(By.Id($"batchShift-{columnIndex}")));
    }

    private IWebElement GetBatchApplyButton(int columnIndex)
    {
        return _wait.Until(d =>
            d.FindElement(By.XPath($"//select[@id='batchShift-{columnIndex}']/following-sibling::button")));
    }

    public string WaitForBrowserAlertText(int timeoutSeconds = 40)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        var alert = wait.Until(d => d.SwitchTo().Alert());
        var text = alert.Text ?? string.Empty;
        alert.Accept();
        return text.Trim();
    }

    private static IWebElement? FindTemplateMenuItem(IWebDriver driver, string templateName)
    {
        var items = driver.FindElements(By.CssSelector("#templateMenu li"));
        return items.FirstOrDefault(li =>
            (li.Text ?? string.Empty).Trim().Equals(templateName, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetActiveTemplateNameFromDriver(IWebDriver driver)
    {
        var active = driver.FindElements(By.CssSelector("#templateMenu li.active")).FirstOrDefault();
        return (active?.Text ?? string.Empty).Trim();
    }
}