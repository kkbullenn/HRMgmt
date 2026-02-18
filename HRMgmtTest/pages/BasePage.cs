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
}