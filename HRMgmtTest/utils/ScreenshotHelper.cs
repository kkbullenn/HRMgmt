using NUnit.Framework;
using OpenQA.Selenium;

namespace HRMgmtTest.utils;

/// <summary>
/// Helper class for capturing screenshots on test failure.
/// Screenshots are saved to SELENIUM_SCREENSHOTS_DIR environment variable path,
/// or to the current directory if not set.
/// </summary>
public static class ScreenshotHelper
{
    private static readonly string ScreenshotsDir =
        Environment.GetEnvironmentVariable("SELENIUM_SCREENSHOTS_DIR")
        ?? Directory.GetCurrentDirectory();

    /// <summary>
    /// Captures a screenshot from the given WebDriver instance.
    /// </summary>
    /// <param name="driver">The WebDriver to capture screenshot from.</param>
    /// <param name="testName">Optional test name for the filename.</param>
    /// <returns>The path to the saved screenshot, or null if capture failed.</returns>
    public static string? CaptureScreenshot(IWebDriver driver, string? testName = null)
    {
        try
        {
            if (driver is not ITakesScreenshot screenshotDriver)
            {
                TestContext.Progress.WriteLine("Driver does not support screenshots");
                return null;
            }

            // Ensure directory exists
            Directory.CreateDirectory(ScreenshotsDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var safeName = SanitizeFileName(testName ?? "unknown");
            var fileName = $"screenshot_{safeName}_{timestamp}.png";
            var filePath = Path.Combine(ScreenshotsDir, fileName);

            var screenshot = screenshotDriver.GetScreenshot();
            screenshot.SaveAsFile(filePath);

            TestContext.Progress.WriteLine($"Screenshot saved: {filePath}");
            
            // Also log page source for debugging
            try
            {
                var pageSourcePath = Path.Combine(ScreenshotsDir, $"pagesource_{safeName}_{timestamp}.html");
                File.WriteAllText(pageSourcePath, driver.PageSource);
                TestContext.Progress.WriteLine($"Page source saved: {pageSourcePath}");
            }
            catch
            {
                // Ignore page source errors
            }

            return filePath;
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"Failed to capture screenshot: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Logs current page information for debugging.
    /// </summary>
    public static void LogPageInfo(IWebDriver driver)
    {
        try
        {
            TestContext.Progress.WriteLine($"Current URL: {driver.Url}");
            TestContext.Progress.WriteLine($"Page Title: {driver.Title}");

            // Log any JavaScript errors if console logging is available
            var logs = driver.Manage().Logs;
            try
            {
                var browserLogs = logs.GetLog(LogType.Browser);
                if (browserLogs.Any())
                {
                    TestContext.Progress.WriteLine("Browser Console Logs:");
                    foreach (var entry in browserLogs.TakeLast(20))
                    {
                        TestContext.Progress.WriteLine($"  [{entry.Level}] {entry.Message}");
                    }
                }
            }
            catch
            {
                // Console logs may not be available in all browsers
            }
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"Failed to log page info: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries))
            .Replace(" ", "_")
            .Replace(".", "_");
    }
}
