using Gtk;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WebKit;
using System.IO;

public class DashboardPage : BasePage
{
    private Label welcomeLabel;
    private Spinner spinner;
    private Box contentBox;
    private ScrolledWindow scrolledWindow;
    private Box vmListBox;  // Container for VM items
    private Separator separator;

    public DashboardPage(MainWindow mainWindow) : base(mainWindow)
    {
        Console.Out.WriteLine($"DASHBOARD INITIALIZED");
        
        // Create main container
        contentBox = new Box(Orientation.Vertical, 10);
        contentBox.Homogeneous = false;
        
        // Add navbar
        var navbar = CreateNavBar();
        contentBox.PackStart(navbar, false, false, 0);
        
        // Add separator
        separator = new Separator(Orientation.Horizontal);
        contentBox.PackStart(separator, false, false, 0);
        
        // Create content area
        var contentArea = new Box(Orientation.Vertical,10);
        contentArea.Homogeneous = false;
        contentArea.MarginStart = contentArea.MarginEnd = contentArea.MarginTop = contentArea.MarginBottom = 20;
        
        welcomeLabel = new Label("Your Virtual Machines");
        welcomeLabel.StyleContext.AddClass("title");
        
        spinner = new Spinner();
        
        vmListBox = new Box(Orientation.Vertical,10);
        vmListBox.Homogeneous = false;
        
        scrolledWindow = new ScrolledWindow();
        scrolledWindow.HeightRequest = 400;
        scrolledWindow.WidthRequest = 600;
        scrolledWindow.Add(vmListBox);
        
        contentArea.PackStart(welcomeLabel, false, false, 0);
        contentArea.PackStart(spinner, false, false, 0);
        contentArea.PackStart(scrolledWindow, true, true, 0);
        
        contentBox.PackStart(contentArea, true, true, 0);

        PackStart(contentBox, true, true, 0);
    }

    private Widget CreateVMWidget(JObject vmData)
    {
        var frame = new Frame();
        var box = new Box(Orientation.Vertical, 5);
        box.MarginStart = box.MarginEnd = box.MarginTop = box.MarginBottom = 10;

        // Title with Status
        var titleBox = new Box(Orientation.Horizontal, 5);
        var titleLabel = new Label($"<b>{vmData["title"]}</b>");
        titleLabel.UseMarkup = true;
        titleLabel.Halign = Align.Start;
        
        var statusLabel = new Label($"({vmData["vm_status_title"]})");
        statusLabel.StyleContext.AddClass(vmData["vm_status_title"].ToString().ToLower() == "running" ? "status-running" : "status-stopped");
        
        titleBox.PackStart(titleLabel, true, true, 0);
        titleBox.PackStart(statusLabel, false, false, 0);

        // Specs
        var specsLabel = new Label(
            $"CPU: {vmData["vm_cpu"]} cores | " +
            $"RAM: {vmData["vm_ram"]} GB | " +
            $"Storage: {vmData["vm_storage"]} GB"
        );
        specsLabel.Halign = Align.Start;

        // Image and Location
        var detailsLabel = new Label(
            $"OS: {vmData["image"]["title"]} | " +
            $"Location: {vmData["country"]["country_name"]}"
        );
        detailsLabel.Halign = Align.Start;

        // Add Connect button
        var connectButton = new Button("Connect");
        connectButton.Clicked += async (sender, e) => {
            await ConnectToVM(vmData["id"].ToString());
        };
        
        // Button container for alignment
        var buttonBox = new Box(Orientation.Horizontal, 0);
        buttonBox.PackEnd(connectButton, false, false, 0);

        box.PackStart(titleBox, false, false, 0);
        box.PackStart(specsLabel, false, false, 0);
        box.PackStart(detailsLabel, false, false, 0);
        box.PackStart(buttonBox, false, false, 5);  // Add some padding

        frame.Add(box);
        frame.ShowAll();
        return frame;
    }

    private async Task ConnectToVM(string vmId)
    {
        try
        {
            var (accessToken, refreshToken) = MainWindow.GetStoredTokens();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                var response = await client.GetAsync($"https://api.haio.ir/v1/cloud/desktop/{vmId}/login");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);
                    
                    if (json["status"]?.Value<bool>() == true)
                    {
                        var vdiUrl = json["params"]?["vdi_url"]?.ToString();
                        if (!string.IsNullOrEmpty(vdiUrl))
                        {
                            // Create VM connection window with ephemeral WebView
                            var vmWindow = new VMConnectionWindow($"VM Connection - {vmId}", vmId);
                            vmWindow.Connect(vdiUrl);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new MessageDialog(
                MainWindow,
                DialogFlags.Modal,
                MessageType.Error,
                ButtonsType.Ok,
                $"Failed to connect to VM: {ex.Message}"
            );
            errorDialog.Run();
            errorDialog.Destroy();
        }
    }

    private async Task FetchVirtualMachines()
    {
        Console.Out.WriteLine($"USING ACCESS TOKEN TO FETCH VMs");
        spinner.Start();
        ClearVMList();

        try
        {
            var (accessToken, tokenType) = MainWindow.GetStoredTokens();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                var response = await client.GetAsync("https://api.haio.ir/v1/cloud/desktop");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);
                    
                    if (json["status"]?.Value<bool>() == true)
                    {
                        var vms = json["params"]?["data"] as JArray;
                        if (vms != null && vms.Count > 0)
                        {
                            foreach (JObject vm in vms)
                            {
                                var vmWidget = CreateVMWidget(vm);
                                vmListBox.PackStart(vmWidget, false, false, 0);
                            }
                        }
                        else
                        {
                            ShowNoVMsMessage();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage($"Error: {ex.Message}");
        }
        finally
        {
            spinner.Stop();
            ShowAll();
        }
    }

    private void ClearVMList()
    {
        foreach (var child in vmListBox.Children)
        {
            vmListBox.Remove(child);
            child.Destroy();
        }
    }

    private void ShowNoVMsMessage()
    {
        var messageLabel = new Label("No virtual machines found");
        messageLabel.StyleContext.AddClass("no-vms-message");
        vmListBox.PackStart(messageLabel, true, true, 0);
    }

    private void ShowErrorMessage(string message)
    {
        var errorLabel = new Label(message);
        errorLabel.StyleContext.AddClass("error-message");
        vmListBox.PackStart(errorLabel, true, true, 0);
    }

    public override void Show()
    {
        ShowAll();
        Console.Out.WriteLine("Dashboard Show() called");
        
        // Execute immediately on the UI thread
        Application.Invoke(async (sender, args) => {
            Console.Out.WriteLine("Starting to fetch data...");
            try {
                await Task.WhenAll(
                    FetchVirtualMachines(),
                    UpdateUserInfo()
                );
            } catch (Exception ex) {
                Console.Out.WriteLine($"Error fetching data: {ex}");
            }
        });
    }

    public override void Hide()
    {
        this.Visible = false;
    }

    private Box CreateNavBar()
    {
        var navbar = new Box(Orientation.Horizontal, 10);
        navbar.Homogeneous = false;
        navbar.MarginStart = navbar.MarginEnd = navbar.MarginTop = navbar.MarginBottom = 10;
        
        // User Info Card (Left)
        var userInfoCard = new EventBox();
        var userInfoBox = new Box(Orientation.Vertical, 10);
        userInfoBox.Homogeneous = false;
        userInfoBox.MarginStart = userInfoBox.MarginEnd = userInfoBox.MarginTop = userInfoBox.MarginBottom = 10;
        
        // Set background color using CSS
        userInfoCard.StyleContext.AddClass("user-info-card");
        
        var nameLabel = new Label("Loading...");
        nameLabel.StyleContext.AddClass("user-info-text");
        
        userInfoBox.PackStart(nameLabel, false, false, 0);
        userInfoCard.Add(userInfoBox);
        
        // Balance Info (Middle)
        var balanceBox = new Box(Orientation.Vertical, 5);
        balanceBox.Halign = Align.Center;
        var balanceLabel = new Label("Balance: Loading...");
        var pointBalanceLabel = new Label("Points: Loading...");
        balanceBox.PackStart(balanceLabel, false, false, 0);
        balanceBox.PackStart(pointBalanceLabel, false, false, 0);
        
        // Logout Button (Right)
        var logoutButton = new Button("Logout");
        logoutButton.Clicked += LogoutButton_Clicked;
        
        navbar.PackStart(userInfoCard, false, false, 0);
        navbar.PackStart(balanceBox, true, true, 0);
        navbar.PackEnd(logoutButton, false, false, 0);

        // Store references to update later
        this.nameLabel = nameLabel;
        this.balanceLabel = balanceLabel;
        this.pointBalanceLabel = pointBalanceLabel;

        return navbar;
    }

    private Label nameLabel;
    private Label balanceLabel;
    private Label pointBalanceLabel;

    private async Task UpdateUserInfo()
    {
        try
        {
            Console.Out.WriteLine($"FETCHING USER INFO");
            var (accessToken, refreshToken) = MainWindow.GetStoredTokens();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                var response = await client.GetAsync("https://api.haio.ir/v1/user/info");
                Console.Out.WriteLine($"USER INFO RESPONSE: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);
                    
                    if (json["status"]?.Value<bool>() == true)
                    {
                        var userInfo = json["params"] as JObject;
                        Application.Invoke((sender, args) => {
                            nameLabel.Text = $"{userInfo["first_name"]} {userInfo["last_name"]}";
                            balanceLabel.Text = $"Balance: {userInfo["balance"]}";
                            pointBalanceLabel.Text = $"Points: {userInfo["point_balance"]}";
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching user info: {ex}");
        }
    }

    private void LogoutButton_Clicked(object sender, EventArgs e)
    {
        var dialog = new MessageDialog(
            MainWindow,
            DialogFlags.Modal | DialogFlags.DestroyWithParent,
            MessageType.Question,
            ButtonsType.None,
            "Are you sure you want to logout?"
        );
        
        dialog.Title = "Logout?";
        dialog.AddButton("Cancel", ResponseType.Cancel);
        dialog.AddButton("Yes", ResponseType.Yes);

        dialog.Response += (o, args) => {
            dialog.Destroy();
            if (args.ResponseId == ResponseType.Yes)
            {
                MainWindow.Logout();
            }
        };

        dialog.Show();
    }
} 