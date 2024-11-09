using System;
using Gtk;

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

            var win = new MainWindow();
            // win.SetSizeRequest(800, 600); // Set a minimum size for the window
            // win.Resizable = true; // Allow the window to be resizable
            app.AddWindow(win);

            win.Show();
            Application.Run();
        }
    }
}
