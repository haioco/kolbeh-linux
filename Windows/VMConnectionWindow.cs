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
    private Clipboard clipboard;

    public VMConnectionWindow(string vmName, string vmId) : base($"{vmName} - {vmId}")
    {
        this.vmId = vmId;
        this.clipboard = Clipboard.Get(Gdk.Selection.Clipboard);
        
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
        // settings.EnableJavascriptClipboard = true;  // Enable clipboard access
        settings.JavascriptCanAccessClipboard = true;

        // Add key event handling for copy/paste
        this.KeyPressEvent += Window_KeyPressEvent;

        // Add clipboard monitoring
        clipboard.OwnerChange += Clipboard_OwnerChange;

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

    private void Window_KeyPressEvent(object o, KeyPressEventArgs args)
    {
        // Handle Ctrl+C, Ctrl+V, Ctrl+X
        if ((args.Event.State & Gdk.ModifierType.ControlMask) != 0)
        {
            switch (args.Event.Key)
            {
                case Gdk.Key.c:
                case Gdk.Key.C:
                    HandleCopy();
                    break;
                case Gdk.Key.v:
                case Gdk.Key.V:
                    HandlePaste();
                    break;
                case Gdk.Key.x:
                case Gdk.Key.X:
                    HandleCut();
                    break;
            }
        }
    }

    private void HandleCopy()
    {
        webView.RunJavascript(@"
            (function() {
                const selectedText = window.getSelection().toString();
                if (selectedText) {
                    window.copyToNative(selectedText);
                }
            })();
        ");
    }

    private void HandlePaste()
    {
        clipboard.RequestText((clipboard, text) =>
        {
            if (!string.IsNullOrEmpty(text))
            {
                // Escape the text for JavaScript
                text = text.Replace("'", "\\'").Replace("\n", "\\n");
                
                webView.RunJavascript($@"
                    (function() {{
                        const activeElement = document.activeElement;
                        if (activeElement && (activeElement.isContentEditable || 
                            activeElement.tagName === 'INPUT' || 
                            activeElement.tagName === 'TEXTAREA')) {{
                            
                            // Create a new input event
                            const event = new InputEvent('input', {{
                                bubbles: true,
                                cancelable: true,
                                inputType: 'insertText',
                                data: '{text}'
                            }});
                            
                            // Set the value and dispatch the event
                            activeElement.value = activeElement.value + '{text}';
                            activeElement.dispatchEvent(event);
                        }}
                    }})();
                ");
            }
        });
    }

    private void HandleCut()
    {
        webView.RunJavascript(@"
            (function() {
                const selectedText = window.getSelection().toString();
                if (selectedText) {
                    window.copyToNative(selectedText);
                    document.execCommand('delete');
                }
            })();
        ");
    }

    private void Clipboard_OwnerChange(object o, OwnerChangeArgs args)
    {
        // Handle clipboard content changes
        if (clipboard.WaitIsTextAvailable())
        {
            clipboard.RequestText((clipboard, text) =>
            {
                if (!string.IsNullOrEmpty(text))
                {
                    // Store the clipboard content for use in the VDI session
                    webView.RunJavascript($@"
                        window.lastClipboardContent = '{text.Replace("'", "\'")}';
                    ");
                }
            });
        }
    }

    public void Connect(string url)
    {
        try
        {
            // Add clipboard bridge to the page
            var clipboardBridge = @"
                window.copyToNative = function(text) {
                    // This function will be called from the JavaScript side
                    // to copy text to the native clipboard
                    console.log('Copying to native clipboard: ' + text);
                };
            ";

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
                        webView.RunJavascript(clipboardBridge);  // Add clipboard bridge
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
