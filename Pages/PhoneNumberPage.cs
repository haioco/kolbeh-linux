using Gtk;
using System;
using System.Linq; // Add this for the All() method

public class PhoneNumberPage : BasePage
{
    private Entry phoneEntry;
    private Button continueButton;
    private Label statusLabel;
    private Spinner spinner;
    private Button debugButton;

    public PhoneNumberPage(MainWindow mainWindow) : base(mainWindow)
    {
        Console.Out.WriteLine($"PHONE NUMBER PAGE INITIALIZED");
        // Create centered container
        var centerBox = new Box(Orientation.Vertical, 10);
        centerBox.Homogeneous = false;
        centerBox.Halign = Align.Center;
        centerBox.Valign = Align.Center;

        // Title
        var titleLabel = new Label("Enter Phone Number");
        titleLabel.StyleContext.AddClass("title");
        centerBox.PackStart(titleLabel, false, false, 0);

        // Phone input
        phoneEntry = new Entry { PlaceholderText = "Phone Number", MaxLength = 11 };
        phoneEntry.WidthRequest = 200;
        phoneEntry.KeyPressEvent += PhoneEntry_KeyPressEvent;
        centerBox.PackStart(phoneEntry, false, false, 0);

        // Continue button
        continueButton = new Button("Continue");
        continueButton.Clicked += ContinueButton_Clicked;
        centerBox.PackStart(continueButton, false, false, 0);

        // Debug button (only if DEBUG is true)
        if (MainWindow.DEBUG)
        {
            debugButton = new Button("Debug Login");
            debugButton.StyleContext.AddClass("debug-button");
            debugButton.Clicked += DebugButton_Clicked;
            centerBox.PackStart(debugButton, false, false, 0);
        }

        // Status label
        statusLabel = new Label("");
        statusLabel.UseMarkup = true;
        centerBox.PackStart(statusLabel, false, false, 0);

        // Spinner
        spinner = new Spinner();
        centerBox.PackStart(spinner, false, false, 0);

        PackStart(centerBox, true, true, 0);
    }

    private void PhoneEntry_KeyPressEvent(object o, KeyPressEventArgs args)
    {
        // Only allow numbers and control keys (backspace, delete, etc.)
        bool isNumber = char.IsDigit((char)args.Event.KeyValue);
        bool isControl = args.Event.KeyValue == (uint)Gdk.Key.BackSpace 
                        || args.Event.KeyValue == (uint)Gdk.Key.Delete
                        || args.Event.KeyValue == (uint)Gdk.Key.Left
                        || args.Event.KeyValue == (uint)Gdk.Key.Right;

        if (!isNumber && !isControl)
        {
            args.RetVal = true; // Block the key press
        }
    }

    private async void ContinueButton_Clicked(object sender, EventArgs e)
    {
        string phone = phoneEntry.Text.Trim();
        if (string.IsNullOrEmpty(phone) || !phone.All(char.IsDigit) || phone.Length != 11)
        {
            statusLabel.Markup = "<span foreground='red'>Please enter a valid 11-digit phone number</span>";
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

        MainWindow.NavigateToOtpPage(phone);

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

    private void DebugButton_Clicked(object sender, EventArgs e)
    {
        var dialog = new Dialog(
            "Debug Login",
            MainWindow,
            DialogFlags.Modal | DialogFlags.DestroyWithParent,
            "Cancel", ResponseType.Cancel,
            "Login", ResponseType.Accept
        );

        // Create entry for access token
        var contentBox = dialog.ContentArea;
        contentBox.MarginStart = contentBox.MarginEnd = contentBox.MarginTop = contentBox.MarginBottom = 10;
        contentBox.Spacing = 10;

        var label = new Label("Enter Access Token:");
        contentBox.Add(label);

        var tokenEntry = new Entry();
        tokenEntry.WidthRequest = 300;
        tokenEntry.Text = ""; // Default empty
        contentBox.Add(tokenEntry);

        dialog.ShowAll();

        dialog.Response += (o, args) =>
        {
            if (args.ResponseId == ResponseType.Accept && !string.IsNullOrEmpty(tokenEntry.Text))
            {
                MainWindow.SetDebugTokens(tokenEntry.Text);
                MainWindow.NavigateToDashboard();
            }
            dialog.Destroy();
        };
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