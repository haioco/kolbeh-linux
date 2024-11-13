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
        ");
        StyleContext.AddProviderForScreen(Screen, cssProvider, 800);
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
                return response.IsSuccessStatusCode;
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
                        tokenType = json["params"]?["data"]?["token_type"]?.ToString();
                        return !string.IsNullOrEmpty(accessToken);
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
        pageStack.VisibleChildName = "dashboard";
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
