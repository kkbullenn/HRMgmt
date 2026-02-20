using HRMgmtTest.pages;
using HRMgmtTest.utils;

namespace HRMgmtTest;

public class AcceptanceTests
{
    private BasePage basePage;

    [SetUp]
    public void Setup()
    {
        // Initialize WebDriver and page object file here (doesn't have to be BasePage)
        // For example:
        basePage = new BasePage(ChromeDriverFactory.CreateChromeDriver());
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up WebDriver here
        basePage.CloseBrowser();
    }
}