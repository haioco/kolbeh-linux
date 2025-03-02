using System;
using Gtk;
using System.Threading.Tasks;

namespace GuacamoleLinuxApp
{
    class Program
    {
        private static Application app;

        [STAThread]
        public static void Main(string[] args)
        {
            Application.Init();

            app = new Application("org.GuacamoleLinuxApp.GuacamoleLinuxApp", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            // Handle application shutdown
            app.Shutdown += (sender, e) => {
                Application.Quit();
                Environment.Exit(0);
            };

            if (MainWindow.DEBUG) {
                var win = new MainWindow();
                app.AddWindow(win);
                win.Show();
            } else {
                var splashScreen = new SplashScreen();
                splashScreen.ShowAll();

                Task.Delay(2500).ContinueWith(t => {
                    Application.Invoke((sender, e) => {
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
