using Gtk;

public class DashboardPage : BasePage
{
    public DashboardPage(MainWindow mainWindow) : base(mainWindow)
    {
        var welcomeLabel = new Label("Welcome to Dashboard");
        welcomeLabel.StyleContext.AddClass("title");
        
        var centerBox = new VBox(false, 10);
        centerBox.Halign = Align.Center;
        centerBox.Valign = Align.Center;
        centerBox.PackStart(welcomeLabel, false, false, 0);

        PackStart(centerBox, true, true, 0);
    }

    public override void Show()
    {
        ShowAll();
    }

    public override void Hide()
    {
        this.Visible = false;
    }
} 