using Gtk;
using WebKit;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;

public class VMConnectionWindow : Window
{
    private WebView webView;
    private string vmId;
    private string sessionDir;

    public VMConnectionWindow(string vmName, string vmId) : base($"{vmName} - {vmId}")
    {
        // Title = $"{vmName} - {vmId}";
        this.vmId = vmId;
        
        SetDefaultSize(1024, 768);
        SetPosition(WindowPosition.Center);

        // Create unique session directory for this VM instance
        sessionDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"kolbeh-vm-{vmId}-{DateTime.Now.Ticks}");
        Directory.CreateDirectory(sessionDir);

        // Create a new WebContext for isolation
        var context = new WebContext();
        
        // Create WebView with isolated context
        webView = new WebView(context);

        // Configure WebView settings for optimal VM connection
        var settings = webView.Settings;
        settings.EnableJavascript = true;
        settings.EnableWebgl = true;
        settings.EnableMediaStream = true;
        settings.EnableSmoothScrolling = true;
        settings.EnableWriteConsoleMessagesToStdout = true;

        // Handle navigation to ensure session isolation
        webView.LoadChanged += (sender, e) => {
            if (e.LoadEvent == LoadEvent.Finished)
            {
                // Clear sensitive data after page load
                try
                {
                    webView.RunJavascript(@"
                        localStorage.clear();
                        sessionStorage.clear();
                        document.cookie = '';
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error clearing browser data: {ex.Message}");
                }
            }
        };

        Add(webView);
        ShowAll();
    }

    public void Connect(string url)
    {
        try
        {

        var auto_resize_window = 
            @"
            window.onresize = function() {
                clearTimeout(window.reloadTimeout);
                window.reloadTimeout = setTimeout(function() {
                    location.reload();
                    }, 500);
                };
            ";
        var auto_redirect=
            @"
            (function lookForButton() {
                const button = Array.from(document.querySelectorAll('button'))
                    .find(el =>
                        el.getAttribute('ng-repeat') === 'action in notification.actions' &&
                        el.getAttribute('ng-click') === 'action.callback()' &&
                        el.getAttribute('ng-class') === 'action.className' &&
                        el.classList.contains('home') &&
                        el.textContent.trim() === 'Home'
                    );

                if (button) {
                    button.click();
                    console.log('Button found and clicked successfully!');
                } else {
                    console.log('Button not found. Retrying...');
                    setTimeout(lookForButton, 100); // Retry after 100ms
                }
                })();
            ";

        // Load the initial URL to set up session data and connection parameters
        webView.LoadUri(url);
        var redirect = true;
        webView.LoadChanged += (sender, e) => {
            if (e.LoadEvent == LoadEvent.Finished)
                {
                    if (redirect) {
                        webView.RunJavascript(auto_redirect);
                    }
                    webView.RunJavascript(auto_resize_window);
                    redirect = false;
                }
            };

            // webView.LoadUri("https://ir2.vdi.haiocloud.com/");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading URL: {ex.Message}");
        }
    }

    protected override bool OnDeleteEvent(Gdk.Event ev)
    {
        try
        {
            // Clear browser data
            webView.RunJavascript(@"
                localStorage.clear();
                sessionStorage.clear();
                document.cookie = '';
            ");

            // Clean up session directory
            if (Directory.Exists(sessionDir))
            {
                Directory.Delete(sessionDir, true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up VM session: {ex.Message}");
        }

        return base.OnDeleteEvent(ev);
    }

    public new void Present()
    {
        base.Present();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Clean up session directory on disposal
                if (Directory.Exists(sessionDir))
                {
                    Directory.Delete(sessionDir, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing VM window: {ex.Message}");
            }
        }
        base.Dispose(disposing);
    }
}
