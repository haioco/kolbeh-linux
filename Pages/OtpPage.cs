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
    private Label cooldownLabel;
    private uint timerHandle;
    private const int COOLDOWN_SECONDS = 600; // 10 minutes
    private int remainingSeconds;
    private bool isInCooldown = false;

    public OtpPage(MainWindow mainWindow) : base(mainWindow)
    {
        // Create centered container
        var centerBox = new VBox(false, 10);
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

        // OTP input
        otpEntry = new Entry { PlaceholderText = "Enter 4-digit OTP", MaxLength = 4 };
        otpEntry.WidthRequest = 200;
        centerBox.PackStart(otpEntry, false, false, 0);

        // Buttons container
        var buttonBox = new HBox(true, 10);
        buttonBox.Halign = Align.Center;

        // Back button
        backButton = new Button("Back");
        backButton.Clicked += BackButton_Clicked;
        buttonBox.PackStart(backButton, false, false, 0);

        // Verify button
        verifyButton = new Button("Verify OTP");
        verifyButton.Clicked += VerifyButton_Clicked;
        buttonBox.PackStart(verifyButton, false, false, 0);

        centerBox.PackStart(buttonBox, false, false, 0);

        // Status label
        statusLabel = new Label("");
        statusLabel.UseMarkup = true;
        centerBox.PackStart(statusLabel, false, false, 0);

        // Spinner
        spinner = new Spinner();
        centerBox.PackStart(spinner, false, false, 0);

        // Cooldown label
        cooldownLabel = new Label("");
        cooldownLabel.UseMarkup = true;
        centerBox.PackStart(cooldownLabel, false, false, 0);

        // "Resend OTP" button
        resendButton = new Button("Resend OTP");
        resendButton.Clicked += ResendButton_Clicked;
        centerBox.PackStart(resendButton, false, false, 0);

        PackStart(centerBox, true, true, 0);
    }

    private void BackButton_Clicked(object sender, EventArgs e)
    {
        StopCooldownTimer();
        MainWindow.NavigateBackToPhone(phoneNumber);
    }

    private async void ResendButton_Clicked(object sender, EventArgs e)
    {
        if (isInCooldown) return; // Extra safety check

        spinner.Start();
        statusLabel.Text = "Resending OTP...";

        bool success = await MainWindow.RequestOtp(phoneNumber);
        
        if (success)
        {
            statusLabel.Markup = "<span foreground='green'>OTP resent successfully</span>";
            otpEntry.Text = "";
            StartCooldown();
        }
        else
        {
            MainWindow.NavigateBackToPhone(phoneNumber);
        }
        spinner.Stop();
    }

    private void StartCooldown()
    {
        isInCooldown = true;
        remainingSeconds = COOLDOWN_SECONDS;
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
        isInCooldown = false;
        cooldownLabel.Text = "";
        resendButton.Visible = true;
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
        cooldownLabel.Markup = $"<span foreground='blue'>Resend available in {minutes:D2}:{seconds:D2}</span>";
        resendButton.Visible = !isInCooldown;
    }

    public void SetPhoneNumber(string phone)
    {
        phoneNumber = phone;
        phoneLabel.Markup = $"<span>Code sent to: {phone}</span>";
        StartCooldown(); // Start cooldown when page is shown
    }

    private async void VerifyButton_Clicked(object sender, EventArgs e)
    {
        string otp = otpEntry.Text.Trim();
        if (string.IsNullOrEmpty(otp) || otp.Length != 4)
        {
            statusLabel.Markup = "<span foreground='red'>Please enter a valid 4-digit OTP</span>";
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
                MainWindow.NavigateToDashboard();
            }
            else
            {
                statusLabel.Markup = "<span foreground='red'>Invalid OTP. Please try again.</span>";
                otpEntry.Text = "";
            }
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
        UpdateCooldownUI(); // Ensure resend button visibility is correct
        otpEntry.Text = "";
        statusLabel.Text = "";
    }

    public override void Hide()
    {
        StopCooldownTimer();
        this.Visible = false;
    }
} 