using HRMgmtTest.pages;
using OpenQA.Selenium.Chrome;

namespace HRMgmtTest;

public class Tests
{
    private BasePage basePage;

    [SetUp]
    public void Setup()
    {
        // Initialize WebDriver and page object file here (doesn't have to be BasePage)
        // For example:
        basePage = new BasePage(new ChromeDriver());
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