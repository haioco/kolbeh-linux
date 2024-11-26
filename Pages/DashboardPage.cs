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

    public DashboardPage(MainWindow mainWindow) : base(mainWindow)
    {
        Console.Out.WriteLine($"DASHBOARD INITIALIZED");

        // Create main container
        contentBox = new Box(Orientation.Vertical, 10);
        contentBox.Homogeneous = false;

        // Add navbar
        var navbar = CreateNavBar();
        contentBox.PackStart(navbar, false, false, 0);

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
        var box = new Box(Orientation.Vertical, 5);
        box.MarginStart = box.MarginEnd = box.MarginTop = box.MarginBottom = 10;

        // Title with Status
        var titleBox = new Box(Orientation.Horizontal, 5);
        var titleLabel = new Label(vmData["title"].ToString());
        titleLabel.Halign = Align.Center;

        var statusCircle = new DrawingArea();
        statusCircle.SetSizeRequest(10, 10);
        statusCircle.Drawn += (o, args) =>
        {
            var cr = args.Cr;
            cr.Arc(5, 5, 5, 0, 2 * Math.PI);
            cr.SetSourceRGB(vmData["vm_status_title"].ToString() == "آنلاین" ? 0.18 : 0.91, vmData["vm_status_title"].ToString() == "آنلاین" ? 0.80 : 0.29, 0.29);
            cr.Fill();
        };

        titleBox.PackStart(titleLabel, true, true, 0);
        titleBox.PackStart(statusCircle, false, false, 0);

        // Load and display the OS icon
        var iconPath = "assets/Windows.png";
        var pixbuf = new Gdk.Pixbuf(iconPath);
        var scaledPixbuf = pixbuf.ScaleSimple(64, 64, Gdk.InterpType.Bilinear); // Scale the icon
        var osIcon = new Image(scaledPixbuf);

        // Specs
        var specsBox = new Box(Orientation.Vertical, 2);
        specsBox.Halign = Align.Center;
        specsBox.PackStart(new Label($"CPU: {vmData["vm_cpu"]} cores"), false, false, 0);
        specsBox.PackStart(new Label($"RAM: {vmData["vm_ram"]} GB"), false, false, 0);
        specsBox.PackStart(new Label($"Storage: {vmData["vm_storage"]} GB"), false, false, 0);
        specsBox.PackStart(new Label($"OS: {vmData["image"]["title"]}"), false, false, 0);
        specsBox.PackStart(new Label($"Location: {vmData["country"]["country_name"]}"), false, false, 0);
        // Add Connect button
        var connectButton = new Button("Connect");
        connectButton.Sensitive = vmData["vm_status_title"].ToString() == "آنلاین";
        connectButton.Clicked += async (sender, e) =>
        {
            await ConnectToVM(vmData["id"].ToString(), vmData["title"].ToString());
        };

        box.PackStart(titleBox, false, false, 0);
        box.PackStart(osIcon, false, false, 0); // Add OS icon below the title
        box.PackStart(specsBox, false, false, 0);
        box.PackStart(connectButton, false, false, 5);  // Add some padding

        frame.Add(box);
        frame.ShowAll();
        return frame;
    }

    private async Task ConnectToVM(string vmId, string vmName)
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
                            var vmWindow = new VMConnectionWindow(vmName, vmId);
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
                                var vmCard = CreateVMCard(vm);
                                vmFlowBox.Add(vmCard);
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
        foreach (var child in vmFlowBox.Children)
        {
            vmFlowBox.Remove(child);
            child.Destroy();
        }
    }

    private void ShowNoVMsMessage()
    {
        var messageLabel = new Label("No virtual machines found");
        messageLabel.StyleContext.AddClass("no-vms-message");
        vmFlowBox.Add(messageLabel);
    }

    private void ShowErrorMessage(string message)
    {
        var errorLabel = new Label(message);
        errorLabel.StyleContext.AddClass("error-message");
        vmFlowBox.Add(errorLabel);
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
                        Application.Invoke((sender, args) =>
                        {
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