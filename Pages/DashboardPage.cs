using Gtk;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WebKit;
using System.IO;

public class DashboardPage : BasePage
{
    private Spinner spinner;
    private Box contentBox;
    private FlowBox vmFlowBox;  // Container for VM items
    private Button refreshButton; // Refresh button

    public DashboardPage(MainWindow mainWindow) : base(mainWindow)
    {
        Console.Out.WriteLine($"DASHBOARD INITIALIZED");

        // Create main container
        contentBox = new Box(Orientation.Vertical, 10);
        contentBox.Homogeneous = false;

        // Add navbar
        var navbar = CreateNavBar();
        contentBox.PackStart(navbar, false, false, 0);

        // Create refresh button
        refreshButton = new Button();
        var refreshIcon = new Image(Stock.Refresh, IconSize.Button);
        refreshButton.Image = refreshIcon;
        refreshButton.Clicked += async (sender, e) => await FetchVirtualMachines();
        contentBox.PackStart(refreshButton, false, false, 0);

        // Create content area
        var contentArea = new Box(Orientation.Vertical, 10);
        contentArea.Homogeneous = false;
        contentArea.MarginStart = contentArea.MarginEnd = contentArea.MarginTop = contentArea.MarginBottom = 20;

        spinner = new Spinner();

        vmFlowBox = new FlowBox();
        vmFlowBox.Homogeneous = false;
        vmFlowBox.SelectionMode = SelectionMode.None;

        // Add scrolling support
        var scrolledWindow = new ScrolledWindow();
        scrolledWindow.Add(vmFlowBox);
        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

        contentArea.PackStart(spinner, false, false, 0);
        contentArea.PackStart(scrolledWindow, true, true, 0);

        contentBox.PackStart(contentArea, true, true, 0);

        PackStart(contentBox, true, true, 0);
    }

    private Widget CreateVMCard(JObject vmData)
    {
        var frame = new Frame();
        frame.StyleContext.AddClass("vm-card");
        
        var box = new Box(Orientation.Vertical, 10);
        box.MarginStart = box.MarginEnd = box.MarginTop = box.MarginBottom = 20;

        // Status indicator with animation
        var statusBox = new Box(Orientation.Horizontal, 5);
        statusBox.Halign = Align.End;
        
        var statusDot = new DrawingArea();
        statusDot.SetSizeRequest(12, 12);
        
        bool isOnline = vmData["vm_status_title"].ToString() == "آنلاین";
        if (isOnline)
        {
            // Create opacity as class-level field for this instance
            double dotOpacity = 1.0;
            bool increasing = false;
            
            uint timeoutId = GLib.Timeout.Add(50, () => {
                dotOpacity += increasing ? 0.05 : -0.05;
                if (dotOpacity >= 1.0) {
                    dotOpacity = 1.0;
                    increasing = false;
                } else if (dotOpacity <= 0.3) {
                    dotOpacity = 0.3;
                    increasing = true;
                }
                statusDot.QueueDraw();
                return true;
            });

            statusDot.Destroyed += (s, e) => {
                if (timeoutId != 0) {
                    GLib.Source.Remove(timeoutId);
                }
            };

            // Use the local dotOpacity variable in the Drawn handler
            statusDot.Drawn += (o, args) => {
                var cr = args.Cr;
                cr.SetSourceRGBA(0.27, 0.74, 0.50, dotOpacity); // Green with animation
                cr.Arc(6, 6, 5, 0, 2 * Math.PI);
                cr.Fill();
            };
        }
        else
        {
            statusDot.Drawn += (o, args) => {
                var cr = args.Cr;
                cr.SetSourceRGB(0.91, 0.29, 0.29); // Red
                cr.Arc(6, 6, 5, 0, 2 * Math.PI);
                cr.Fill();
            };
        }

        var statusLabel = new Label(isOnline ? "Online" : "Offline");
        statusLabel.StyleContext.AddClass(isOnline ? "status-online" : "status-offline");
        
        statusBox.PackStart(statusLabel, false, false, 0);
        statusBox.PackStart(statusDot, false, false, 0);

        // VM Title with icon
        var titleBox = new Box(Orientation.Horizontal, 10);
        titleBox.Halign = Align.Start;

        var titleLabel = new Label(vmData["title"].ToString());
        titleLabel.StyleContext.AddClass("vm-title");
        titleLabel.Halign = Align.Start;

        titleBox.PackStart(titleLabel, true, true, 0);
        titleBox.PackStart(statusBox, false, false, 0);

        // Specs with icons
        var specsBox = new Box(Orientation.Vertical, 5);
        specsBox.StyleContext.AddClass("specs-box");
        
        var specs = new[] {
            ("CPU", $"{vmData["vm_cpu"]} cores", "cpu"),
            ("RAM", $"{vmData["vm_ram"]} GB", "memory"),
            ("Storage", $"{vmData["vm_storage"]} GB", "drive"),
            ("OS", vmData["image"]["title"].ToString(), "computer"),
            ("Location", vmData["country"]["country_name"].ToString(), "network")
        };

        foreach (var (label, value, icon) in specs)
        {
            var specBox = new Box(Orientation.Horizontal, 5);
            var iconLabel = new Label($"<span color='#45BD80'>\u2022</span>");
            iconLabel.UseMarkup = true;
            
            var specLabel = new Label($"{label}: {value}");
            specLabel.StyleContext.AddClass("spec-text");
            specLabel.Halign = Align.Start;
            
            specBox.PackStart(iconLabel, false, false, 0);
            specBox.PackStart(specLabel, true, true, 0);
            specsBox.PackStart(specBox, false, false, 0);
        }

        // Connect button with loading state
        var connectButton = new Button();
        connectButton.StyleContext.AddClass("connect-button");
        
        var buttonBox = new Box(Orientation.Horizontal, 5);
        var buttonLabel = new Label("Connect");
        var spinner = new Spinner();
        
        buttonBox.PackStart(buttonLabel, true, true, 0);
        buttonBox.PackStart(spinner, false, false, 0);
        connectButton.Add(buttonBox);

        connectButton.Sensitive = isOnline;
        if (!isOnline) {
            connectButton.TooltipText = "VM is currently offline";
        }

        connectButton.Clicked += async (sender, e) => {
            buttonLabel.Visible = false;
            spinner.Start();
            spinner.Visible = true;
            connectButton.Sensitive = false;

            try
            {
                await ConnectToVM(vmData["id"].ToString(), vmData["title"].ToString().Trim(), vmData["vm_sequence_id"].ToString());
            }
            finally
            {
                buttonLabel.Visible = true;
                spinner.Stop();
                spinner.Visible = false;
                connectButton.Sensitive = true;
            }
        };

        // Assembly
        box.PackStart(titleBox, false, false, 0);
        box.PackStart(new Separator(Orientation.Horizontal), false, false, 5);
        box.PackStart(specsBox, false, false, 0);
        box.PackStart(connectButton, false, false, 10);

        frame.Add(box);
        frame.ShowAll();
        return frame;
    }

    private async Task ConnectToVM(string vmId, string vmName, string vmNumber)
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
                            // Create VM connection window with the VM's name
                            var vmWindow = new VDIConnectionWindow(vmName, vmId, vmNumber);
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
                                var vmCard = CreateVMCard(vm);
                                vmFlowBox.Add(vmCard);
                            }
                        }
                        else
                        {
                            ShowMessage("No virtual machines found", "info");
                        }
                    }
                }
                else
                {
                    ShowMessage("Failed to fetch VMs. Please try again.", "error");
                }
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Error: {ex.Message}", "error");
        }
        finally
        {
            spinner.Stop();
            ShowAll();
        }
    }

    private void ClearVMList()
    {
        foreach (var child in vmFlowBox.Children)
        {
            vmFlowBox.Remove(child);
            child.Destroy();
        }
    }

    private void ShowMessage(string message, string type)
    {
        var messageBox = new Box(Orientation.Vertical, 5);
        messageBox.Halign = Align.Center;
        messageBox.Valign = Align.Center;

        var icon = new Image();
        switch (type)
        {
            case "error":
                icon = new Image(Stock.DialogError, IconSize.Dialog);
                break;
            case "info":
                icon = new Image(Stock.DialogInfo, IconSize.Dialog);
                break;
        }

        var label = new Label(message);
        label.StyleContext.AddClass($"message-{type}");
        
        messageBox.PackStart(icon, false, false, 10);
        messageBox.PackStart(label, false, false, 5);
        
        vmFlowBox.Add(messageBox);
    }

    public override void Show()
    {
        ShowAll();
        Console.Out.WriteLine("Dashboard Show() called");

        // Execute immediately on the UI thread
        Application.Invoke(async (sender, args) =>
        {
            Console.Out.WriteLine("Starting to fetch data...");
            try
            {
                await Task.WhenAll(
                    FetchVirtualMachines(),
                    UpdateUserInfo()
                );
            }
            catch (Exception ex)
            {
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
        balanceLabel.StyleContext.AddClass("balance-text");
        var pointBalanceLabel = new Label("Points: Loading...");
        pointBalanceLabel.StyleContext.AddClass("balance-text");
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
                        Application.Invoke((sender, args) =>
                        {
                            nameLabel.Text = $"{userInfo["first_name"]} {userInfo["last_name"]}";
                            var balance = long.Parse(userInfo["balance"].ToString());
                            var points = long.Parse(userInfo["point_balance"].ToString());
                            balanceLabel.Text = $"Balance: {balance:N0}";
                            pointBalanceLabel.Text = $"Points: {points:N0}";
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
            DialogFlags.Modal,
            MessageType.Question,
            ButtonsType.None,
            "Are you sure you want to logout?"
        );

        dialog.Title = "Logout?";
        dialog.AddButton("Cancel", ResponseType.Cancel);
        dialog.AddButton("Yes", ResponseType.Yes);

        dialog.Response += (o, args) =>
        {
            dialog.Destroy();
            if (args.ResponseId == ResponseType.Yes)
            {
                MainWindow.Logout();
            }
        };

        dialog.Show();
    }
}
