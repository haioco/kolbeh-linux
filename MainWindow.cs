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
    private string tokenType;

    // Phone number cooldown tracking
    private Dictionary<string, DateTime> phoneCooldowns = new Dictionary<string, DateTime>();
    private const int COOLDOWN_SECONDS = 600; // 10 minutes

    public MainWindow() : base("OTP Authentication")
    {
        SetDefaultSize(400, 500);
        SetPosition(WindowPosition.Center);

        // Initialize pages
        phoneNumberPage = new PhoneNumberPage(this);
        otpPage = new OtpPage(this);
        dashboardPage = new DashboardPage(this);

        // Setup page stack
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
        var (canRequest, remainingTime) = CheckPhoneCooldown(phone);
        
        if (!canRequest)
        {
            return false;
        }

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
                    StartPhoneCooldown(phone);
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
                        tokenType = json["params"]?["data"]?["token_type"]?.ToString();
                        
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

    public void NavigateToDashboard()
    {
        Console.Out.WriteLine("Navigating to Dashboard...");
        pageStack.VisibleChildName = "dashboard";
        dashboardPage.Show();
    }

    public (string AccessToken, string TokenType) GetStoredTokens()
    {
        return (accessToken, tokenType);
    }

    public void NavigateBackToPhone(string phone)
    {
        phoneNumberPage.SetPhoneNumber(phone);
        pageStack.VisibleChildName = "phone";
    }
}
