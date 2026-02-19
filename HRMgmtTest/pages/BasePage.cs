using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace HRMgmtTest.pages;

public class BasePage
{
    protected readonly IWebDriver _driver;
    protected readonly WebDriverWait _wait;

    public BasePage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }
    
    public void CloseBrowser()
    {
        _driver.Quit();
    }

    protected void SetDateValue(IWebElement input, string yyyyMmDd)
    {
        input.Clear();
        input.SendKeys(yyyyMmDd);

        var js = (IJavaScriptExecutor)_driver;
        js.ExecuteScript(
            "arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', { bubbles: true })); arguments[0].dispatchEvent(new Event('change', { bubbles: true }));",
            input,
            yyyyMmDd);
    }

    protected void ClickElement(IWebElement element)
    {
        try
        {
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            var js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", element);
            js.ExecuteScript("arguments[0].click();", element);
        }
    }
}