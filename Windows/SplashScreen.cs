using Gtk;
using System;
using System.Threading.Tasks;

public class SplashScreen : Window
{
    private Image logoImage;
    private Label titleLabel;
    private ProgressBar progressBar;
    private VBox container;

    public SplashScreen() : base(WindowType.Popup)
    {
        Title = "KOLBEH";
        SetDefaultSize(320, 400);
        SetPosition(WindowPosition.Center);
        Decorated = false;  // Remove window decorations
        
        // Create container
        container = new VBox();
        container.Spacing = 20;
        container.MarginStart = container.MarginEnd = container.MarginTop = container.MarginBottom = 0;

        // Load and display logo
        try 
        {
            logoImage = new Image("kolbeh.png");
            logoImage.SetSizeRequest(320, 400);  // Set size of the image to fill the window
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
        progressBar = new ProgressBar();
        progressBar.Fraction = 0.0;

        // Pack widgets
        container.PackStart(logoImage, true, true, 0);
        container.PackStart(titleLabel, false, false, 0);
        container.PackStart(progressBar, false, false, 0);

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
        for (double i = 0; i <= 1.0; i += 0.1)
        {
            progressBar.Fraction = i;
            await Task.Delay(100);  // Delay for animation effect
            while (Application.EventsPending())
                Application.RunIteration();
        }

        await Task.Delay(500);  // Final delay before closing
        Hide();
    }
} 