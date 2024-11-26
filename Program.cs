using System;
using Gtk;
using System.Threading.Tasks;

namespace GuacamoleLinuxApp
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {

            Application.Init();

            var app = new Application("org.GuacamoleLinuxApp.GuacamoleLinuxApp", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            if (MainWindow.DEBUG) {
                var win = new MainWindow();
                app.AddWindow(win);
                win.Show();                
            } else {
            // Show splash screen
            var splashScreen = new SplashScreen();
            splashScreen.ShowAll();

            // Delay for splash screen
            Task.Delay(2500).ContinueWith(t =>
            {
                Application.Invoke((sender, e) =>
                {
                    splashScreen.Hide();
                    var win = new MainWindow();
                    app.AddWindow(win);
                    win.Show();
                });
            });
            }
            Application.Run();
        }
    }
}
