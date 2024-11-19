using Gtk;
using WebKit;
using System;
using System.IO;

public class VMConnectionWindow : Window
{
    private WebView webView;
    private string vmId;
    private string sessionDir;

    public VMConnectionWindow(string title, string vmId) : base(title)
    {
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
            webView.LoadUri(url);
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
