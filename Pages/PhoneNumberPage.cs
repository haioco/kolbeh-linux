using Gtk;
using System;

public class PhoneNumberPage : BasePage
{
    private Entry phoneEntry;
    private Button continueButton;
    private Label statusLabel;
    private Spinner spinner;

    public PhoneNumberPage(MainWindow mainWindow) : base(mainWindow)
    {
        Console.Out.WriteLine($"PHONE NUMBER PAGE INITIALIZED");
        // Create centered container
        var centerBox = new VBox(false, 10);
        centerBox.Halign = Align.Center;
        centerBox.Valign = Align.Center;

        // Title
        var titleLabel = new Label("Enter Phone Number");
        titleLabel.StyleContext.AddClass("title");
        centerBox.PackStart(titleLabel, false, false, 0);

        // Phone input
        phoneEntry = new Entry { PlaceholderText = "Phone Number", MaxLength = 11 };
        phoneEntry.WidthRequest = 200;
        centerBox.PackStart(phoneEntry, false, false, 0);

        // Continue button
        continueButton = new Button("Continue");
        continueButton.Clicked += ContinueButton_Clicked;
        centerBox.PackStart(continueButton, false, false, 0);

        // Status label
        statusLabel = new Label("");
        statusLabel.UseMarkup = true;
        centerBox.PackStart(statusLabel, false, false, 0);

        // Spinner
        spinner = new Spinner();
        centerBox.PackStart(spinner, false, false, 0);

        PackStart(centerBox, true, true, 0);
    }

    private async void ContinueButton_Clicked(object sender, EventArgs e)
    {
        string phone = phoneEntry.Text.Trim();
        if (string.IsNullOrEmpty(phone))
        {
            statusLabel.Markup = "<span foreground='red'>Please enter your phone number</span>";
            return;
        }

        var (canRequest, remainingTime) = MainWindow.CheckPhoneCooldown(phone);
        if (!canRequest)
        {
            int minutes = (int)remainingTime.TotalMinutes;
            int seconds = (int)remainingTime.TotalSeconds % 60;
            statusLabel.Markup = $"<span foreground='red'>Please wait {minutes:D2}:{seconds:D2} before requesting another OTP</span>";
            return;
        }

        spinner.Start();
        continueButton.Sensitive = false;
        phoneEntry.Sensitive = false;

        try
        {
            bool success = await MainWindow.RequestOtp(phone);
            if (success)
            {
                statusLabel.Text = "";
                MainWindow.NavigateToOtpPage(phone);
            }
            else
            {
                statusLabel.Markup = "<span foreground='red'>Failed to send OTP. Please try again.</span>";
            }
        }
        finally
        {
            spinner.Stop();
            continueButton.Sensitive = true;
            phoneEntry.Sensitive = true;
        }
    }

    public void SetPhoneNumber(string phone)
    {
        phoneEntry.Text = phone;
    }

    public override void Show()
    {
        ShowAll();
        statusLabel.Text = "";
    }

    public override void Hide()
    {
        this.Visible = false;
    }
} 