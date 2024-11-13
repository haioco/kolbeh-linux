using Gtk;

public abstract class BasePage : VBox
{
    protected MainWindow MainWindow { get; }

    protected BasePage(MainWindow mainWindow)
    {
        MainWindow = mainWindow;
        SetMargins();
    }

    private void SetMargins()
    {
        MarginStart = 20;
        MarginEnd = 20;
        MarginTop = 20;
        MarginBottom = 20;
        Spacing = 10;
    }

    public abstract void Show();
    public abstract void Hide();
} 