using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Gtk;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using WebKit; // Add this for WebView
using System.Diagnostics;
using System.Threading;


public class MainWindow : Window
{
    private Entry usernameEntry;
    private Entry passwordEntry;
    private Label alertLabel;
    private Label statusBar;
    private Spinner spinner;

    private Image connectionStatusImage;


    public MainWindow() : base("Guacamole Connections")
{
    SetDefaultSize(800, 600);
    SetPosition(WindowPosition.Center);

    VBox vbox = new VBox(false, 2);

    usernameEntry = new Entry { PlaceholderText = "Username" };
    passwordEntry = new Entry { PlaceholderText = "Password", Visibility = false };
    Button loginButton = new Button("Login");
    alertLabel = new Label { Text = "Alerts will be displayed here", UseMarkup = true };
    statusBar = new Label("Status: Disconnected");
    spinner = new Spinner();
    connectionStatusImage = new Image();

    loginButton.Clicked += LoginButton_Clicked;

    vbox.PackStart(usernameEntry, false, false, 0);
    vbox.PackStart(passwordEntry, false, false, 0);
    vbox.PackStart(loginButton, false, false, 0);
    vbox.PackStart(alertLabel, false, false, 0);

    HBox statusBox = new HBox(false, 2);
    statusBox.PackStart(connectionStatusImage, false, false, 0);
    statusBox.PackStart(statusBar, true, true, 0);
    statusBox.PackStart(spinner, false, false, 0);

    vbox.PackStart(statusBox, false, false, 0);

    Add(vbox);
    ShowAll();
}


    private async void LoginButton_Clicked(object sender, EventArgs e)
    {
        string guacamoleUrl = "https://ir2.vdi.haiocloud.com/api/tokens";
        string username = usernameEntry.Text;
        string password = passwordEntry.Text;
        int maxRetries = 3;
        int retryCount = 0;
        bool success = false;

        statusBar.Text = "Status: Connecting...";
        spinner.Start();
        spinner.Show();

        while (retryCount < maxRetries && !success)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("username", username),
                        new KeyValuePair<string, string>("password", password)
                    });

                    HttpResponseMessage response = await client.PostAsync(guacamoleUrl, content);
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        JObject json = JObject.Parse(responseString);
                        string authToken = json["authToken"].ToString();

                        string connectionUrl = $"https://ir2.vdi.haiocloud.com/#/?token={authToken}";
                        OpenConnectionWindow("Guacamole Connection", connectionUrl);
                        success = true;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        statusBar.Text = "Status: Disconnected";
                        spinner.Stop();
                        spinner.Hide();
                        alertLabel.Markup = "<span foreground='red'>Error: Authentication failed.</span>";
                        break;
                    }
                    else
                    {
                        statusBar.Text = "Status: Error";
                        spinner.Stop();
                        spinner.Hide();
                        alertLabel.Markup = $"<span foreground='red'>Error: {response.ReasonPhrase}</span>";
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                statusBar.Text = "Status: Network Error";
                spinner.Stop();
                spinner.Hide();
                alertLabel.Markup = $"<span foreground='red'>Network Error: {httpEx.Message}</span>";
            }
            catch (Exception ex)
            {
                statusBar.Text = "Status: Error";
                spinner.Stop();
                spinner.Hide();
                alertLabel.Markup = $"<span foreground='red'>Error: {ex.Message}</span>";
            }

            if (!success)
            {
                retryCount++;
                if (retryCount < maxRetries)
                {
                    await Task.Delay(2000); // Wait for 2 seconds before retrying
                }
            }
        }

        if (!success)
        {
            alertLabel.Markup = "<span foreground='red'>Error: Could not establish connection. Please try again.</span>";
        }
    }
    
   private void EnableCopyPaste(WebView webView)
    {
        webView.KeyPressEvent += (o, args) =>
        {
            if ((args.Event.State & Gdk.ModifierType.ControlMask) != 0)
            {
                switch (args.Event.Key)
                {
                    case Gdk.Key.c:
                        webView.ExecuteEditingCommand("Copy");
                        break;
                    case Gdk.Key.v:
                        webView.ExecuteEditingCommand("Paste");
                        break;
                    case Gdk.Key.x:
                        webView.ExecuteEditingCommand("Cut");
                        break;
                }
            }
        };
    }
    private void OpenConnectionWindow(string connectionName, string url)
{
    Window connectionWindow = new Window(connectionName);
    connectionWindow.SetDefaultSize(800, 600);
    connectionWindow.SetPosition(WindowPosition.Center);
    connectionWindow.Resizable = true; // Allow the window to be resizable

    VBox vbox = new VBox(false, 2);

    WebView webView = new WebView();
    statusBar = new Label("Status: Loading...");
    spinner = new Spinner();
    spinner.Start();


    // Set up the WebView
    webView.LoadChanged += (sender, args) =>
    {
        if (webView.IsLoading)
        {
            statusBar.Text = "Status: Loading...";
            spinner.Start();
            spinner.Show();
        }
        else
        {
            statusBar.Text = "Status: Connected";
            spinner.Stop();
            spinner.Hide();

            // Inject JavaScript to handle window resize and reload the page
            string script = @"
                window.onresize = function() {
                    clearTimeout(window.reloadTimeout);
                    window.reloadTimeout = setTimeout(function() {
                        location.reload();
                    }, 500);
                };
            ";
            webView.RunJavascript(script);
        }
    };

    // Load the URL
    webView.LoadUri(url);

    // Enable copy-paste
    EnableCopyPaste(webView);
    
    // Add the WebView and status bar to the window
    HBox hbox = new HBox(false, 2);
    hbox.PackStart(statusBar, true, true, 0);
    hbox.PackStart(spinner, false, false, 0);

    vbox.PackStart(webView, true, true, 0); // Ensure the WebView expands
    vbox.PackStart(hbox, false, false, 0); // Add the status bar at the bottom

    connectionWindow.Add(vbox);
    connectionWindow.ShowAll();

    // Close the main window
    this.Destroy();
}





}
