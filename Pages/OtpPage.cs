using Gtk;
using System;
using System.Threading.Tasks;

public class OtpPage : BasePage
{
    private Entry otpEntry;
    private Button verifyButton;
    private Button backButton;
    private Label statusLabel;
    private Spinner spinner;
    private string phoneNumber;
    private Label phoneLabel;
    private Button resendButton;
    private uint timerHandle;
    private int remainingSeconds;
    private bool isFirstResend = true;
    private const int INITIAL_COOLDOWN_SECONDS = 120; // 2 minutes
    private const int SUBSEQUENT_COOLDOWN_SECONDS = 600; // 10 minutes

    public OtpPage(MainWindow mainWindow) : base(mainWindow)
    {
        Console.Out.WriteLine($"OTP PAGE INITIALIZED:\nPhone_number:{phoneNumber}");
        // Create centered container
        var centerBox = new Box(Orientation.Vertical, 10);
        centerBox.Homogeneous = false;
        centerBox.Halign = Align.Center;
        centerBox.Valign = Align.Center;

        // Title
        var titleLabel = new Label("Enter OTP Code");
        titleLabel.StyleContext.AddClass("title");
        centerBox.PackStart(titleLabel, false, false, 0);

        // Phone number display
        phoneLabel = new Label("");
        phoneLabel.UseMarkup = true;
        centerBox.PackStart(phoneLabel, false, false, 0);

        // Input container
        var inputBox = new Box(Orientation.Horizontal, 10);
        inputBox.Homogeneous = false;
        inputBox.Halign = Align.Center;

        // Back button
        backButton = new Button("Back");
        backButton.Clicked += BackButton_Clicked;
        inputBox.PackStart(backButton, false, false, 0);

        // OTP input
        otpEntry = new Entry { PlaceholderText = "Enter 6-digit OTP", MaxLength = 6 };
        otpEntry.WidthRequest = 200;
        inputBox.PackStart(otpEntry, false, false, 0);

        // Verify button
        verifyButton = new Button("Verify");
        verifyButton.Clicked += VerifyButton_Clicked;
        inputBox.PackStart(verifyButton, false, false, 0);

        centerBox.PackStart(inputBox, false, false, 0);

        // Status label
        statusLabel = new Label("");
        statusLabel.UseMarkup = true;
        centerBox.PackStart(statusLabel, false, false, 0);

        // Spinner
        spinner = new Spinner();
        centerBox.PackStart(spinner, false, false, 0);

        // "Resend OTP" button
        resendButton = new Button("Resend OTP");
        resendButton.Clicked += ResendButton_Clicked;
        centerBox.PackStart(resendButton, false, false, 0);

        PackStart(centerBox, true, true, 0);
    }

    private void BackButton_Clicked(object sender, EventArgs e)
    {
        StopCooldownTimer();
        MainWindow.NavigateToPhone(phoneNumber);
    }

    private async void ResendButton_Clicked(object sender, EventArgs e)
    {
        if (isFirstResend)
        {
            StartCooldown(SUBSEQUENT_COOLDOWN_SECONDS);
            isFirstResend = false;
        }
        else
        {
            StartCooldown(SUBSEQUENT_COOLDOWN_SECONDS);
        }

        spinner.Start();
        statusLabel.Text = "Requesting OTP...";

        bool success = await MainWindow.RequestOtp(phoneNumber);
        
        if (success)
        {
            statusLabel.Markup = "<span foreground='green'>OTP requested successfully</span>";
            otpEntry.Text = "";
        }
        else
        {
            statusLabel.Markup = "<span foreground='red'>Failed to request OTP. Please try again.</span>";
        }
        spinner.Stop();
    }

    private void StartCooldown(int cooldownSeconds)
    {
        remainingSeconds = cooldownSeconds;
        UpdateCooldownUI();
        
        // Stop any existing timer
        StopCooldownTimer();

        // Start new timer
        timerHandle = GLib.Timeout.Add(1000, () =>
        {
            remainingSeconds--;
            UpdateCooldownUI();

            if (remainingSeconds <= 0)
            {
                EndCooldown();
                return false; // Stop timer
            }
            return true; // Continue timer
        });
    }

    private void EndCooldown()
    {
        resendButton.Sensitive = true;
        resendButton.Label = "Resend OTP";
        timerHandle = 0;
    }

    private void StopCooldownTimer()
    {
        if (timerHandle != 0)
        {
            GLib.Source.Remove(timerHandle);
            timerHandle = 0;
        }
    }

    private void UpdateCooldownUI()
    {
        int minutes = remainingSeconds / 60;
        int seconds = remainingSeconds % 60;
        resendButton.Label = $"Resend in {minutes:D2}:{seconds:D2}";
        resendButton.Sensitive = false;
    }

    public void SetPhoneNumber(string phone)
    {
        phoneNumber = phone;
        phoneLabel.Markup = $"<span>Code sent to: {phone}</span>";
        StartCooldown(INITIAL_COOLDOWN_SECONDS); // Start initial cooldown when page is shown
    }

    private async void VerifyButton_Clicked(object sender, EventArgs e)
    {
        string otp = otpEntry.Text.Trim();
        Console.Out.WriteLine($"VERIFICATION PROCESS INITIALIZED:\nPhone_number:{phoneNumber}\nOTP:{otp}");
        if (string.IsNullOrEmpty(otp) || otp.Length != 6)
        {
            statusLabel.Markup = "<span foreground='red'>Please enter a valid 6-digit OTP</span>";
            return;
        }

        spinner.Start();
        verifyButton.Sensitive = false;
        backButton.Sensitive = false;
        otpEntry.Sensitive = false;

        try
        {
            bool success = await MainWindow.VerifyOtp(phoneNumber, otp);
            if (success)
            {
                StopCooldownTimer();
                Application.Invoke((sender, args) => {
                    MainWindow.NavigateToDashboard();
                });
            }
            else
            {
                statusLabel.Markup = "<span foreground='red'>Invalid OTP. Please try again.</span>";
                otpEntry.Text = "";
            }
        }
        catch (Exception ex)
        {
            Console.Out.WriteLine($"Error during verification: {ex}");
            statusLabel.Markup = "<span foreground='red'>An error occurred during verification.</span>";
        }
        finally
        {
            spinner.Stop();
            verifyButton.Sensitive = true;
            backButton.Sensitive = true;
            otpEntry.Sensitive = true;
        }
    }

    public override void Show()
    {
        ShowAll();
        otpEntry.Text = "";
        statusLabel.Text = "";
    }

    public override void Hide()
    {
        StopCooldownTimer();
        this.Visible = false;
    }
}