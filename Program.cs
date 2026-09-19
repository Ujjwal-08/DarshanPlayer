using System;
using System.IO;
using DarshanPlayer.Services;
using Velopack;

namespace DarshanPlayer
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Velopack must run before anything else — handles install/update/uninstall hooks and
            // applies pending updates on startup. The fast callbacks run in a short-lived process
            // with no UI, and must finish quickly.
            VelopackApp.Build()
                .WithAfterInstallFastCallback(_ => AppIdentity.RegisterWithWindows())
                .WithAfterUpdateFastCallback(_ =>
                {
                    AppIdentity.RegisterWithWindows();
                    RemoveLegacyShortcuts();
                })
                .WithBeforeUninstallFastCallback(_ => AppIdentity.UnregisterFromWindows())
                .Run();

            // Before any window exists, so the taskbar button, pinned shortcut and jump list all
            // agree on which app this is.
            AppIdentity.ApplyAppUserModelId();

            var request = LaunchRequest.Parse(args);
            if (IsSingleInstanceEnabled())
            {
                if (SingleInstance.TryBecomePrimary())
                {
                    SingleInstance.StartServer();
                }
                else if (SingleInstance.SendToPrimary(request, TimeSpan.FromSeconds(5)))
                {
                    // The running window has it (or was just asked to come forward).
                    return;
                }
                // Otherwise the primary is not answering — hung or mid-exit — so start normally
                // rather than leave the user with nothing.
            }

            var app = new App();
            app.InitializeComponent();
            app.Run();
            SingleInstance.Shutdown();
        }

        /// <summary>
        /// Reads only the single-instance preference, before the full settings service exists.
        /// Defaults to on if the settings file is missing or unreadable.
        /// </summary>
        private static bool IsSingleInstanceEnabled()
        {
            try
            {
                var settings = new SettingsService();
                settings.Load();
                return settings.Current.SingleInstance;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Builds before 1.2.0 were packed without a title, so their shortcuts are named
        /// "DarshanPlayer". Once a correctly titled "Darshan Player" shortcut exists beside one,
        /// drop the old copy so updated users do not end up with two. Only ever removes a legacy
        /// shortcut that has a replacement next to it.
        /// </summary>
        private static void RemoveLegacyShortcuts()
        {
            try
            {
                foreach (var folder in new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                })
                {
                    var legacy = Path.Combine(folder, "DarshanPlayer.lnk");
                    var current = Path.Combine(folder, "Darshan Player.lnk");
                    if (File.Exists(legacy) && File.Exists(current))
                        File.Delete(legacy);
                }
            }
            catch
            {
                // A leftover shortcut is cosmetic; never fail an update over it.
            }
        }
    }
}
