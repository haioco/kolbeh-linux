using System;
using System.Net.Http;
using System.Threading.Tasks;
using Gtk;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class MainWindow : Window
{
    private Stack pageStack;
    private PhoneNumberPage phoneNumberPage;
    private OtpPage otpPage;
    private DashboardPage dashboardPage;

    // Store authentication tokens
    private string accessToken;
    private string refreshToken;
    public static bool DEBUG = false; // Debug flag
    // public static bool DEBUG = true; // Debug flag

    // Phone number cooldown tracking
    private Dictionary<string, DateTime> phoneCooldowns = new Dictionary<string, DateTime>();
    private const int COOLDOWN_SECONDS = 600; // 10 minutes

    public MainWindow() : base("Kolbeh VDI Solution")
    {
        // Add this line near the beginning of the constructor
        DeleteEvent += Window_DeleteEvent;

        SetDefaultSize(700, 550);
        SetPosition(WindowPosition.Center);

        // Initialize pages
        phoneNumberPage = new PhoneNumberPage(this);
        otpPage = new OtpPage(this);
        dashboardPage = new DashboardPage(this);

        // Setup page stack;
        pageStack = new Stack();
        pageStack.AddNamed(phoneNumberPage, "phone");
        pageStack.AddNamed(otpPage, "otp");
        pageStack.AddNamed(dashboardPage, "dashboard");

        Add(pageStack);

        // Show initial page
        pageStack.VisibleChildName = "phone";
        ShowAll();

        // Add CSS styling
        var cssProvider = new CssProvider();
        cssProvider.LoadFromData(@"
            /* Base window styling */
            window {
                background-color: #01172E;
            }
            
            /* Typography */
            .title { 
                font-size: 20px;
                font-weight: bold;
                margin-bottom: 20px;
                color: #FFFCE9;
            }

            label {
                color: #FFFCE9;
            }

            /* Form elements */
            entry {
                background: #FFFCE9;
                color: #01172E;
                border: none;
                border-radius: 4px;
                padding: 8px;
                min-height: 20px;
            }

            entry:focus {
                border: 1px solid #0F76A9;
            }

            button {
                background: #0F76A9;
                color: #FFFCE9;
                border: none;
                border-radius: 4px;
                padding: 8px 16px;
                min-height: 36px;
            }

            button:hover {
                background: #45BD80;
            }

            button:disabled {
                background: #666666;
                opacity: 0.7;
            }

            /* Status indicators */
            .status-running {
                color: #45BD80;
                font-weight: bold;
            }

            .status-stopped {
                color: #e74c3c;
                font-weight: bold;
            }

            .no-vms-message {
                color: #FFFCE9;
                font-style: italic;
            }

            .error-message {
                color: #e74c3c;
            }

            /* Debug elements */
            .debug-button {
                background: #e74c3c;
                color: #FFFCE9;
                padding: 5px 10px;
            }

            /* Dashboard elements */
            .user-info-card {
                border-radius: 10px;
                background-color: #0F76A9;
                padding: 10px;
                box-shadow: 0 2px 4px rgba(0,0,0,0.2);
            }

            .user-info-text {
                color: #FFFCE9;
                font-weight: bold;
            }

            frame {
                background-color: #011732;
                border-radius: 8px;
                border: none;
            }

            frame box {
                background-color: #011732;
            }

            scrolledwindow {
                background-color: #01172E;
            }

            flowbox {
                background-color: #01172E;
            }

            /* VM Connection window */
            .vm-window {
                background-color: #01172E;
            }

            .vm-window box {
                background-color: #01172E;
            }

            .vm-window webview {
                background-color: #FFFCE9;
            }

            /* Action buttons */
            .connect-button {
                background-color: #45BD80;
                color: #FFFCE9;
                font-weight: bold;
            }

            .connect-button:hover {
                background-color: #3aa971;
            }

            .logout-button {
                background-color: #e74c3c;
                color: #FFFCE9;
            }

            .logout-button:hover {
                background-color: #c0392b;
            }

            /* Dialog styling */
            dialog {
                background-color: #01172E;
                border: 1px solid #0F76A9;
                border-radius: 8px;
            }

            dialog box {
                background-color: #01172E;
            }

            dialog label {
                color: #FFFCE9;
            }

            /* Spinner styling */
            spinner {
                color: #45BD80;
            }

            /* Enhanced VM Cards */
            .vm-card {
                background-color: #011E3C;
                border-radius: 16px;
                border: 1px solid rgba(15, 118, 169, 0.3);
                margin: 12px;
                padding: 8px;
                box-shadow: 0 4px 6px rgba(0, 0, 0, 0.2);
                transition: all 200ms ease;
            }

            .vm-card:hover {
                border-color: #0F76A9;
                box-shadow: 0 6px 12px rgba(0, 0, 0, 0.3);
            }

            .specs-box {
                background-color: rgba(15, 118, 169, 0.1);
                border-radius: 12px;
                padding: 15px;
                margin: 8px 0;
            }

            frame box {
                background-color: transparent;
            }

            .vm-title {
                font-size: 18px;
                font-weight: bold;
                color: #FFFCE9;
            }

            .spec-text {
                color: #FFFCE9;
                font-size: 14px;
            }

            .status-online {
                color: #45BD80;
                font-size: 12px;
            }

            .status-offline {
                color: #e74c3c;
                font-size: 12px;
            }

            .connect-button {
                background-color: #45BD80;
                color: #FFFCE9;
                font-weight: bold;
                min-height: 40px;
                border-radius: 20px;
            }

            .connect-button:hover {
                background-color: #3aa971;
            }

            .connect-button:disabled {
                background-color: #666666;
            }

            .message-error {
                color: #e74c3c;
                font-size: 16px;
            }

            .message-info {
                color: #FFFCE9;
                font-size: 16px;
                font-style: italic;
            }
        ");
        StyleContext.AddProviderForScreen(Screen, cssProvider, 800);
    }

    private void Window_DeleteEvent(object sender, DeleteEventArgs args)
    {
        Application.Quit();
        args.RetVal = true;
    }

    public (bool CanRequest, TimeSpan RemainingTime) CheckPhoneCooldown(string phone)
    {
        if (!phoneCooldowns.ContainsKey(phone))
        {
            return (true, TimeSpan.Zero);
        }

        var cooldownEnd = phoneCooldowns[phone].AddSeconds(COOLDOWN_SECONDS);
        var remainingTime = cooldownEnd - DateTime.Now;

        if (remainingTime <= TimeSpan.Zero)
        {
            phoneCooldowns.Remove(phone);
            return (true, TimeSpan.Zero);
        }

        return (false, remainingTime);

    }

    private void StartPhoneCooldown(string phone)
    {
        phoneCooldowns[phone] = DateTime.Now;
    }

    public async Task<bool> RequestOtp(string phone)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Requesting OTP for phone number: {phone}");
        try
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("mobile", phone)
                });

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Sending OTP request to API...");
                HttpResponseMessage response = await client.PostAsync("https://api.haio.ir/v1/user/otp/login", content);

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] API Response Status: {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] OTP request successful");
                    return true;
                }
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] OTP request failed");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error requesting OTP: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> VerifyOtp(string phone, string otp)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("mobile", phone),
                    new KeyValuePair<string, string>("otp_code", otp)
                });

                HttpResponseMessage response = await client.PostAsync("https://api.haio.ir/v1/user/otp/login/verify", content);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(responseString);

                    if (json["status"]?.Value<bool>() == true)
                    {
                        accessToken = json["params"]?["data"]?["access_token"]?.ToString();
                        Console.Out.WriteLine($"ACCESS TOKEN RECEIVED: {accessToken}");
                        refreshToken = json["params"]?["data"]?["refresh_token"]?.ToString();

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            // Clear cooldown on successful verification
                            phoneCooldowns.Remove(phone);
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    public void NavigateToOtpPage(string phone)
    {
        otpPage.SetPhoneNumber(phone);
        pageStack.VisibleChildName = "otp";
    }

    public void InitializeDashboardPage(string accessToken)
    {
        pageStack.AddNamed(dashboardPage, "dashboard");
        Add(pageStack);
    }

    public void NavigateToDashboard()
    {
        Console.Out.WriteLine("Navigating to Dashboard...");
        // Create new DashboardPage if it doesn't exist
        if (dashboardPage == null)
        {
            var (accessToken, _) = GetStoredTokens();
            dashboardPage = new DashboardPage(this);
            pageStack.AddNamed(dashboardPage, "dashboard");
        }
        pageStack.VisibleChildName = "dashboard";
        dashboardPage.Show();
    }

    public (string AccessToken, string TokenType) GetStoredTokens()
    {
        return (accessToken, refreshToken);
    }

    public void NavigateToPhone(string phone)
    {
        phoneNumberPage.SetPhoneNumber(phone);
        pageStack.VisibleChildName = "phone";
    }

    public void Logout()
    {
        // Clear tokens
        accessToken = null;
        refreshToken = null;

        // Reset pages
        pageStack.VisibleChildName = "phone";

        // Clear any sensitive data
        phoneNumberPage.SetPhoneNumber("");

        // Clean up dashboard
        if (dashboardPage != null)
        {
            pageStack.Remove(dashboardPage);
            dashboardPage.Destroy();
            dashboardPage = null;  // Important to set to null after destroying
        }
    }

    public void SetDebugTokens(string token)
    {
        accessToken = token;
        refreshToken = "debug_refresh_token";
    }
}
