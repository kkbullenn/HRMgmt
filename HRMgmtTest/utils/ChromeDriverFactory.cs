using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace HRMgmtTest.utils;

/// <summary>
/// Factory for creating ChromeDriver instances with test-friendly configurations.
/// Disables password manager prompts, leak detection, and other browser notifications.
/// </summary>
public static class ChromeDriverFactory
{
    public static IWebDriver CreateChromeDriver()
    {
        var options = new ChromeOptions();
        
        // Disable password manager, leak detection, and credential features at the feature flag level.
        // PasswordLeakDetection is the feature that checks passwords against known breaches
        // and shows the "Change your password" dialog.
        options.AddArgument("--no-first-run");
        options.AddArgument("--no-default-browser-check");
        options.AddArgument("--password-store=basic");
        options.AddArgument("--disable-sync");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-default-apps");
        options.AddArgument("--disable-features=PasswordLeakDetection,PasswordManager,PasswordManagerOnboarding,PasswordManagerDesktopSync,PasswordUi,SafeBrowsingEnhancedProtection");
        
        // Disable password manager, leak detection, and safe browsing via user preferences
        options.AddUserProfilePreference("profile.password_manager_enabled", false);
        options.AddUserProfilePreference("profile.password_manager_leak_detection", false);
        options.AddUserProfilePreference("credentials_enable_service", false);
        options.AddUserProfilePreference("safebrowsing.enabled", false);
        options.AddUserProfilePreference("profile.default_content_settings.notifications", 2);
        
        return new ChromeDriver(options);
    }
}

