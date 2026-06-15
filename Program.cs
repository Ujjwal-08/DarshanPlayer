using System;
using Velopack;

namespace DarshanPlayer
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Velopack must run before anything else — handles install/uninstall hooks
            // and applies pending updates on startup.
            VelopackApp.Build().Run();

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
