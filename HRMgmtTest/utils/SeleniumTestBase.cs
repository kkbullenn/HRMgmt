using NUnit.Framework;
using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using HRMgmtTest.pages;

namespace HRMgmtTest.utils;

/// <summary>
/// Base class for Selenium UI tests that provides:
/// - Automatic screenshot capture on test failure
/// - Standardized setup/teardown patterns
/// - Common test configuration
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("NUnit", "NUnit1032:An IDisposable field/property should be Disposed in a TearDown method", Justification = "Driver is disposed in BaseTearDown via SafeQuitDriver")]
public abstract class SeleniumTestBase
{
    protected IWebDriver? Driver { get; private set; }
    protected LoginPage? LoginPage { get; private set; }

    /// <summary>
    /// Creates and configures the WebDriver instance.
    /// Override to customize driver creation.
    /// </summary>
    protected virtual IWebDriver CreateDriver()
    {
        return ChromeDriverFactory.CreateChromeDriver();
    }

    /// <summary>
    /// Called during OneTimeSetUp for test fixture-level initialization.
    /// Override to add fixture-level setup (runs once per test class).
    /// </summary>
    protected virtual void OnFixtureSetUp()
    {
    }

    /// <summary>
    /// Called during SetUp for each test.
    /// Override to add test-level setup (runs before each test).
    /// </summary>
    protected virtual void OnTestSetUp()
    {
    }

    /// <summary>
    /// Called during TearDown for each test, regardless of outcome.
    /// Override to add test-level cleanup.
    /// </summary>
    protected virtual void OnTestTearDown()
    {
    }

    /// <summary>
    /// Called during OneTimeTearDown for fixture-level cleanup.
    /// Override to add fixture-level cleanup (runs once per test class).
    /// </summary>
    protected virtual void OnFixtureTearDown()
    {
    }

    [OneTimeSetUp]
    public void FixtureSetUp()
    {
        OnFixtureSetUp();
    }

    [SetUp]
    public void BaseSetUp()
    {
        Driver = CreateDriver();
        LoginPage = new LoginPage(Driver);
        OnTestSetUp();
    }

    [TearDown]
    public void BaseTearDown()
    {
        try
        {
            // Capture screenshot on failure
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                CaptureFailureInfo();
            }

            OnTestTearDown();
        }
        finally
        {
            // Always quit the driver
            SafeQuitDriver();
        }
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        OnFixtureTearDown();
    }

    /// <summary>
    /// Captures screenshot and page info when a test fails.
    /// </summary>
    protected virtual void CaptureFailureInfo()
    {
        if (Driver == null) return;

        var testName = TestContext.CurrentContext.Test.Name;
        
        try
        {
            TestContext.Progress.WriteLine($"\n=== Test Failed: {testName} ===");
            
            // Log page info
            ScreenshotHelper.LogPageInfo(Driver);
            
            // Capture screenshot
            ScreenshotHelper.CaptureScreenshot(Driver, testName);
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"Error capturing failure info: {ex.Message}");
        }
    }

    /// <summary>
    /// Safely quits the WebDriver, handling any exceptions.
    /// </summary>
    protected void SafeQuitDriver()
    {
        try
        {
            Driver?.Quit();
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"Error quitting driver: {ex.Message}");
        }
        finally
        {
            Driver = null;
        }
    }

    /// <summary>
    /// Logs into the application with the QA test account.
    /// </summary>
    protected void LoginAsAdmin()
    {
        LoginPage?.GoTo();
        LoginPage?.Login("qa_test", "123456");
    }

    /// <summary>
    /// Returns true if running in CI environment (GitHub Actions).
    /// </summary>
    protected static bool IsCI =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

    /// <summary>
    /// Waits for a specified duration. Use sparingly - prefer explicit waits.
    /// </summary>
    protected static void Wait(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }

    /// <summary>
    /// Waits a longer duration when running in CI (headless is slower).
    /// </summary>
    protected static void WaitForCI(int normalMs, int ciMs)
    {
        Thread.Sleep(IsCI ? ciMs : normalMs);
    }
}
