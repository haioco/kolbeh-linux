using Gtk;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class DashboardPage : BasePage
{
    private Label welcomeLabel;
    private Spinner spinner;
    private VBox contentBox;
    private ScrolledWindow scrolledWindow;
    private VBox vmListBox;  // Container for VM items

    public DashboardPage(MainWindow mainWindow) : base(mainWindow)
    {
        Console.Out.WriteLine($"DASHBOARD INITIALIZED");
        welcomeLabel = new Label("Your Virtual Machines");
        welcomeLabel.StyleContext.AddClass("title");
        
        spinner = new Spinner();
        
        // Create VBox for VM list
        vmListBox = new VBox(false, 10);
        
        scrolledWindow = new ScrolledWindow();
        scrolledWindow.HeightRequest = 400;
        scrolledWindow.WidthRequest = 600;
        scrolledWindow.Add(vmListBox);
        
        contentBox = new VBox(false, 10);
        contentBox.Halign = Align.Fill;
        contentBox.Valign = Align.Start;
        contentBox.PackStart(welcomeLabel, false, false, 0);
        contentBox.PackStart(spinner, false, false, 0);
        contentBox.PackStart(scrolledWindow, true, true, 0);

        PackStart(contentBox, true, true, 0);
    }

    private Widget CreateVMWidget(JObject vmData)
    {
        var frame = new Frame();
        var box = new VBox(false, 5);
        box.MarginStart = box.MarginEnd = box.MarginTop = box.MarginBottom = 10;

        // Title with Status
        var titleBox = new HBox(false, 5);
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

        box.PackStart(titleBox, false, false, 0);
        box.PackStart(specsLabel, false, false, 0);
        box.PackStart(detailsLabel, false, false, 0);

        frame.Add(box);
        frame.ShowAll();
        return frame;
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
                else
                {
                    ShowErrorMessage($"Failed to fetch VMs. Status code: {response.StatusCode}");
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
            Console.Out.WriteLine("Starting to fetch VMs...");
            try {
                await FetchVirtualMachines();
            } catch (Exception ex) {
                Console.Out.WriteLine($"Error in FetchVirtualMachines: {ex}");
            }
        });
    }

    public override void Hide()
    {
        this.Visible = false;
    }
} 