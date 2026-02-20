using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace HRMgmtTest.utils;

/// <summary>
/// Factory for creating ChromeDriver instances with test-friendly configurations.
/// Automatically detects CI environments and applies headless mode with
/// appropriate stability options.
/// </summary>
public static class ChromeDriverFactory
{
    public static IWebDriver CreateChromeDriver()
    {
        var options = new ChromeOptions();
        var isCi = string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase)
                   || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        
        // ── Core stability options (all environments) ──────────────────────────
        options.AddArgument("--no-first-run");
        options.AddArgument("--no-default-browser-check");
        options.AddArgument("--password-store=basic");
        options.AddArgument("--disable-sync");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-default-apps");
        options.AddArgument("--disable-popup-blocking");
        options.AddArgument("--disable-infobars");
        options.AddArgument("--disable-features=PasswordLeakDetection,PasswordManager,PasswordManagerOnboarding,PasswordManagerDesktopSync,PasswordUi,SafeBrowsingEnhancedProtection");
        
        // Disable password manager and notifications via preferences
        options.AddUserProfilePreference("profile.password_manager_enabled", false);
        options.AddUserProfilePreference("profile.password_manager_leak_detection", false);
        options.AddUserProfilePreference("credentials_enable_service", false);
        options.AddUserProfilePreference("safebrowsing.enabled", false);
        options.AddUserProfilePreference("profile.default_content_settings.notifications", 2);

        // ── CI-specific options (headless mode) ────────────────────────────────
        if (isCi)
        {
            // Use new headless mode (better compatibility)
            options.AddArgument("--headless=new");
            
            // Required for running as root in containers
            options.AddArgument("--no-sandbox");
            
            // Overcome limited resource problems in containers
            options.AddArgument("--disable-dev-shm-usage");
            
            // Disable GPU (not available in headless)
            options.AddArgument("--disable-gpu");
            
            // Set explicit window size for consistent rendering
            options.AddArgument("--window-size=1920,1080");
            
            // Additional stability for CI
            options.AddArgument("--disable-background-timer-throttling");
            options.AddArgument("--disable-backgrounding-occluded-windows");
            options.AddArgument("--disable-renderer-backgrounding");
            options.AddArgument("--disable-ipc-flooding-protection");
            
            // Enable verbose logging for debugging CI issues
            options.AddArgument("--enable-logging");
            options.AddArgument("--v=1");
            
            // Increase timeouts for slower CI runners
            options.AddArgument("--remote-debugging-pipe");
        }
        else
        {
            // Local development: visible browser, normal window
            options.AddArgument("--start-maximized");
        }
        
        // Create driver with implicit wait for element locations
        var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(isCi ? 10 : 5);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(isCi ? 60 : 30);
        driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(30);
        
        return driver;
    }
}

