using Gtk;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

public class SplashScreen : Window
{
    private Image logoImage;
    private Label titleLabel;
    // private ProgressBar progressBar;
    private Box container;

    public SplashScreen() : base(WindowType.Popup)
    {
        Title = "KOLBEH";
        SetDefaultSize(320, 400);
        SetPosition(WindowPosition.Center);
        Decorated = false;  // Remove window decorations
        
        // Create container
        container = new Box(Orientation.Vertical, 0);
        // container.Spacing = 0;
        container.MarginStart = container.MarginEnd = container.MarginTop = container.MarginBottom = 0;

        // Load and display logo
        try 
        {
            var assembly = Assembly.GetExecutingAssembly();
            // Update to match the LogicalName in the csproj file
            using Stream stream = assembly.GetManifestResourceStream("GuacamoleLinuxApp.Resources.kolbeh.png");
            if (stream != null)
            {
                var pixbuf = new Gdk.Pixbuf(stream);
                logoImage = new Image(pixbuf);
                logoImage.SetSizeRequest(400, 400);
            }
            else
            {
                Console.WriteLine("Resource not found. Available resources:");
                foreach (var resourceName in assembly.GetManifestResourceNames())
                {
                    Console.WriteLine($"- {resourceName}");
                }
                throw new FileNotFoundException("Embedded resource kolbeh.png not found. Check namespace and build settings.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading logo: {ex.Message}");
            logoImage = new Image();  // Empty image as fallback
        }

        // Create title label
        titleLabel = new Label("KOLBEH");
        titleLabel.StyleContext.AddClass("splash-title");

        // Create progress bar
        // progressBar = new ProgressBar();
        // progressBar.Fraction = 0.0;

        // Pack widgets
        container.PackStart(logoImage, true, true, 0);
        // container.PackStart(titleLabel, false, false, 0);
        // container.PackStart(progressBar, false, false, 0);

        Add(container);

        // Add CSS styling
        var css = new CssProvider();
        css.LoadFromData(@"
            .splash-title {
                font-size: 24px;
                font-weight: bold;
            }
            window {
                background-color: white;
            }
        ");
        StyleContext.AddProvider(css, uint.MaxValue);
    }

    public async Task ShowSplashScreen()
    {
        ShowAll();

        // Simulate loading progress
        // for (double i = 0; i <= 1.0; i += 0.1)
        // {
        //     progressBar.Fraction = i;
        //     await Task.Delay(0);  // Delay for animation effect
        //     while (Application.EventsPending())
        //         Application.RunIteration();
        // }

        await Task.Delay(500);  // Final delay before closing
        Hide();
    }
}