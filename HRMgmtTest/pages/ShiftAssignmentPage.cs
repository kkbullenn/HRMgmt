using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.pages;

public class ShiftAssignmentPage : BasePage
{
    private const string PagePath = "/Shift/ShiftAssignment";

    private IWebElement TemplateNameInput => _wait.Until(d => d.FindElement(By.Id("templateNameInput")));
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

    public void GoTo(string baseUrl)
    {
        var trimmedBase = (baseUrl ?? string.Empty).TrimEnd('/');
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

    public void ClickSaveTemplate()
    {
        ClickElement(SaveTemplateButton);
    }

    public void ClickGenerateSchedule()
    {
        ClickElement(AutoAssignButton);
    }

    public void ClickDeleteTemplate()
    {
        ClickElement(DeleteTemplateButton);
    }

    public void ClickClearGrid()
    {
        ClickElement(ClearGridButton);
    }

    public void SelectTemplateFromMenu(string templateName)
    {
        var item = _wait.Until(d =>
            d.FindElement(By.XPath($"//ul[@id='templateMenu']/li[normalize-space()='{templateName}']")));
        ClickElement(item);
    }

    public IReadOnlyList<string> GetTemplateNames()
    {
        var items = _driver.FindElements(By.CssSelector("#templateMenu li"));
        return items
            .Select(item => item.Text.Trim())
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
        var alerts = _driver.FindElements(By.CssSelector(".alert.alert-success"));
        return alerts.FirstOrDefault()?.Text.Trim() ?? string.Empty;
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
        var select = new SelectElement(GetShiftCell(rowIndex, columnIndex));
        return select.SelectedOption?.GetAttribute("value") ?? string.Empty;
    }

    public void SelectShiftByText(int rowIndex, int columnIndex, string partialText)
    {
        var select = new SelectElement(GetShiftCell(rowIndex, columnIndex));
        foreach (var option in select.Options)
        {
            if (option.Text.Contains(partialText))
            {
                select.SelectByText(option.Text);
                return;
            }
        }
        var optionsText = select.Options.Select(o => o.Text).ToList();
        var available = string.Join(", ", optionsText);
        throw new NoSuchElementException($"Cannot find shift with text containing '{partialText}'. Available options: [{available}]");
    }

    public string GetFirstAvailableShiftName()
    {
        // Try row 0, col 0
        var select = new SelectElement(GetShiftCell(0, 0));
        // Skip placeholders if any (usually empty value or "Select")
        var option = select.Options.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.Text) && !string.IsNullOrWhiteSpace(o.GetAttribute("value")));
        
        if (option == null) 
        {
            var optionsText = string.Join(", ", select.Options.Select(o => o.Text));
             throw new NoSuchElementException($"No available shifts found in the dropdown to test with. Options: [{optionsText}]");
        }
        return option.Text;
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
}