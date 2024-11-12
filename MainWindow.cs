using System;
using System.Net.Http;
using System.Threading.Tasks;
using Gtk;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class MainWindow : Window
{
    private Entry mobileEntry;
    private Entry otpEntry;
    private Button requestOtpButton;
    private Button verifyOtpButton;
    private Label alertLabel;
    private Label statusBar;
    private Spinner spinner;
    private VBox otpContainer;

    // Store authentication tokens
    private string accessToken;
    private string tokenType;

    public MainWindow() : base("OTP Authentication")
    {
        SetDefaultSize(400, 300);
        SetPosition(WindowPosition.Center);

        VBox vbox = new VBox(false, 2);

        // Create mobile number input
        mobileEntry = new Entry { PlaceholderText = "Mobile Number" };
        requestOtpButton = new Button("Request OTP");
        
        // Create OTP input (initially hidden)
        otpContainer = new VBox(false, 2);
        otpEntry = new Entry { PlaceholderText = "Enter OTP", MaxLength = 4 };
        verifyOtpButton = new Button("Verify OTP");
        otpContainer.PackStart(otpEntry, false, false, 0);
        otpContainer.PackStart(verifyOtpButton, false, false, 0);
        otpContainer.Hide();

        alertLabel = new Label { Text = "Enter your mobile number", UseMarkup = true };
        statusBar = new Label("Status: Ready");
        spinner = new Spinner();

        requestOtpButton.Clicked += RequestOtpButton_Clicked;
        verifyOtpButton.Clicked += VerifyOtpButton_Clicked;

        // Pack everything
        vbox.PackStart(mobileEntry, false, false, 0);
        vbox.PackStart(requestOtpButton, false, false, 0);
        vbox.PackStart(otpContainer, false, false, 0);
        vbox.PackStart(alertLabel, false, false, 0);

        HBox statusBox = new HBox(false, 2);
        statusBox.PackStart(statusBar, true, true, 0);
        statusBox.PackStart(spinner, false, false, 0);

        vbox.PackStart(statusBox, false, false, 0);

        Add(vbox);
        ShowAll();
    }

    private async void RequestOtpButton_Clicked(object sender, EventArgs e)
    {
        string mobile = mobileEntry.Text.Trim();
        if (string.IsNullOrEmpty(mobile))
        {
            alertLabel.Markup = "<span foreground='red'>Please enter a mobile number</span>";
            return;
        }

        statusBar.Text = "Status: Requesting OTP...";
        spinner.Start();
        spinner.Show();

        try
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("mobile", mobile)
                });

                HttpResponseMessage response = await client.PostAsync("https://api.haio.ir/v1/user/otp/login", content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    alertLabel.Markup = "<span foreground='green'>OTP sent successfully. Please check your phone.</span>";
                    otpContainer.Show();
                    requestOtpButton.Sensitive = false;
                    mobileEntry.Sensitive = false;
                }
                else
                {
                    alertLabel.Markup = $"<span foreground='red'>Error: {response.ReasonPhrase}</span>";
                }
            }
        }
        catch (Exception ex)
        {
            alertLabel.Markup = $"<span foreground='red'>Error: {ex.Message}</span>";
        }
        finally
        {
            spinner.Stop();
            spinner.Hide();
            statusBar.Text = "Status: Waiting for OTP";
        }
    }

    private async void VerifyOtpButton_Clicked(object sender, EventArgs e)
    {
        string mobile = mobileEntry.Text.Trim();
        string otp = otpEntry.Text.Trim();

        if (string.IsNullOrEmpty(otp) || otp.Length != 4)
        {
            alertLabel.Markup = "<span foreground='red'>Please enter a valid 4-digit OTP</span>";
            return;
        }

        statusBar.Text = "Status: Verifying OTP...";
        spinner.Start();
        spinner.Show();

        try
        {
            using (HttpClient client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("mobile", mobile),
                    new KeyValuePair<string, string>("otp_code", otp)
                });

                HttpResponseMessage response = await client.PostAsync("https://api.haio.ir/v1/user/otp/login/verify", content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    JObject json = JObject.Parse(responseString);
                    
                    if (json["status"]?.Value<bool>() == true)
                    {
                        // Store the tokens
                        accessToken = json["params"]?["data"]?["access_token"]?.ToString();
                        tokenType = json["params"]?["data"]?["token_type"]?.ToString();

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            alertLabel.Markup = "<span foreground='green'>Authentication successful!</span>";
                            statusBar.Text = "Status: Authenticated";
                        }
                        else
                        {
                            alertLabel.Markup = "<span foreground='red'>Invalid response format from server.</span>";
                        }
                    }
                    else
                    {
                        alertLabel.Markup = $"<span foreground='red'>Error: {json["message"]}</span>";
                    }
                }
                else
                {
                    alertLabel.Markup = "<span foreground='red'>Invalid OTP. Please try again.</span>";
                    otpEntry.Text = "";
                }
            }
        }
        catch (Exception ex)
        {
            alertLabel.Markup = $"<span foreground='red'>Error: {ex.Message}</span>";
        }
        finally
        {
            spinner.Stop();
            spinner.Hide();
        }
    }

    // Method to get the stored tokens
    public (string AccessToken, string TokenType) GetStoredTokens()
    {
        return (accessToken, tokenType);
    }
}
