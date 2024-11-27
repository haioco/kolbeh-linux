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
    // public static bool DEBUG = false; // Debug flag
    public static bool DEBUG = false; // Debug flag

    // Phone number cooldown tracking
    private Dictionary<string, DateTime> phoneCooldowns = new Dictionary<string, DateTime>();
    private const int COOLDOWN_SECONDS = 600; // 10 minutes

    public MainWindow() : base("Kolbeh VDI Solution")
    {
        SetDefaultSize(700, 500);
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
            .title { 
                font-size: 20px;
                font-weight: bold;
                margin-bottom: 20px;
            }
            .status-running {
                color: #2ecc71;
                font-weight: bold;
            }
            .status-stopped {
                color: #e74c3c;
                font-weight: bold;
            }
            .no-vms-message {
                color: #7f8c8d;
                font-style: italic;
            }
            .error-message {
                color: #c0392b;
            }
            .debug-button {
                background: #e74c3c;
                color: white;
                padding: 5px 10px;
            }
            .user-info-card {
                border-radius: 10px;
                background-color: #0092E1;
            }
            .user-info-text {
                color: #FFFFFF;
            }
        ");
        StyleContext.AddProviderForScreen(Screen, cssProvider, 800);
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
        try
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("mobile", phone)
                });

                HttpResponseMessage response = await client.PostAsync("https://api.haio.ir/v1/user/otp/login", content);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                return false;
            }
        }
        catch
        {
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

