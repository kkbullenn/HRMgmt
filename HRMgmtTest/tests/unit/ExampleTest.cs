using HRMgmtTest.pages;
using HRMgmtTest.utils;

namespace HRMgmtTest;

public class Tests
{
    private EmployeeShiftPage _employeeShiftPage;

    [SetUp]
    public void Setup()
    {
        // Initialize WebDriver and page object file here (doesn't have to be BasePage)
        // For example:
        _employeeShiftPage = new EmployeeShiftPage(ChromeDriverFactory.CreateChromeDriver());
    }

    [Test]
    public void Test1()
    {
        _employeeShiftPage.GoTo();
        _employeeShiftPage.SelectEmployee("05d5f549-5153-4a7e-bcca-4393a933eb36");
        Assert.That(_employeeShiftPage.HasShiftOnDate("2026-02-16"), Is.True,
            "E001 should have a shift on Tuesday");
        Assert.Pass();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up WebDriver here
        _employeeShiftPage.CloseBrowser();
    }
}