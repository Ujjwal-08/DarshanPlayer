using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using DarshanPlayer.Services;
using System.Windows.Threading;

namespace DarshanPlayer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Log.Init();
            Log.Info("App", "Starting {Version} on {OS} ({Bitness}-bit), args: {ArgCount}",
                typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown",
                Environment.OSVersion.VersionString,
                Environment.Is64BitProcess ? 64 : 32,
                e.Args.Length);

            // Global crash protection – show error dialog instead of silent exit
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            if (e.Args.Length > 0)
            {
                Properties["Args"] = e.Args;
            }

            // Settings first (needed by other services)
            var settings = new SettingsService();
            settings.Load();
            ServiceLocator.SettingsService = settings;
            Log.SetVerbose(settings.Current.VerboseLogging);

            // Self-repair Windows integration for installed copies: covers users who installed a
            // build without the registration hooks, and anything a Windows update reset. Idempotent,
            // and skipped for dev builds and portable copies so they never hijack associations.
            if (AppIdentity.IsInstalledCopy())
                _ = Task.Run(() => AppIdentity.RegisterWithWindows());

            // Notification service (toasts). Created early so settings save-failures can surface.
            var notifications = new NotificationService();
            ServiceLocator.Notifications = notifications;
            settings.SaveFailed += (_, ex) => notifications.ShowError($"Could not save settings: {ex.Message}");

            // Language manager
            var lang = new LanguageManager();
            ServiceLocator.LanguageManager = lang;

            // Media service — pass subtitle style settings so freetype is initialized with them
            // A failure here means no playback at all, so it gets loud diagnostics rather than a
            // silent crash: LibVLC init problems are almost always a missing or mismatched libvlc
            // folder next to the executable.
            try
            {
                ServiceLocator.MediaService = new LibVlcMediaService(settings.Current);
                Log.Info("Startup", "LibVLC initialized; libvlc dir present: {HasLibVlc}",
                    Directory.Exists(Path.Combine(AppContext.BaseDirectory, "libvlc")));
            }
            catch (Exception ex)
            {
                Log.Fatal("Startup", ex,
                    "LibVLC failed to initialize. BaseDirectory={BaseDir}, libvlc present={HasLibVlc}",
                    AppContext.BaseDirectory,
                    Directory.Exists(Path.Combine(AppContext.BaseDirectory, "libvlc")));

                MessageBox.Show(
                    "Darshan Player could not start its media engine (LibVLC)." +
                    Environment.NewLine + Environment.NewLine + ex.Message +
                    Environment.NewLine + Environment.NewLine + "Details were written to:" +
                    Environment.NewLine + Log.CurrentLogFile,
                    "Darshan Player - Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            // Playlist service
            var playlist = new PlaylistService();
            playlist.RepeatMode = settings.Current.RepeatMode;
            playlist.IsShuffle = settings.Current.IsShuffle;
            ServiceLocator.PlaylistService = playlist;

            // Update check — fire-and-forget; never blocks startup or crashes the app
            var updateService = new UpdateService();
            ServiceLocator.UpdateService = updateService;
            _ = Task.Run(async () =>
            {
                var newVersion = await updateService.CheckAndDownloadAsync();
                if (newVersion == null) return;
                Dispatcher.Invoke(() =>
                    notifications.ShowInfo($"Update v{newVersion} downloaded — restart to install"));
            });
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash("DispatcherUnhandledException", e.Exception);
            e.Handled = true; // Prevent crash
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe application will try to continue.",
                "Darshan Player – Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LogCrash("CurrentDomain_UnhandledException", ex);
            MessageBox.Show(
                $"A fatal error occurred:\n\n{ex?.Message ?? e.ExceptionObject?.ToString()}\n\nThe application may need to restart.",
                "Darshan Player – Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>Append a full crash report (with stack trace) to %LocalAppData%\DarshanPlayer\crash.log
        /// so hard-to-reproduce crashes can be diagnosed after the fact.</summary>
        private static void LogCrash(string source, Exception? ex)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DarshanPlayer");
                Directory.CreateDirectory(dir);
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}\n{ex}\n\n";
                File.AppendAllText(Path.Combine(dir, "crash.log"), line);
                Log.Fatal("Crash", ex, "Unhandled exception from {Source}", source);
            }
            catch { /* logging must never throw */ }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Flush any pending debounced settings writes (volume/rate/position from the last few hundred ms).
            // Without this, the user's last setting change can be lost on a fast quit.
            try { ServiceLocator.SettingsService?.FlushPendingSave(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[App] FlushPendingSave failed: {ex.Message}"); }

            // Persist the playlist so the next launch can pick up where the user left off.
            if (ServiceLocator.SettingsService?.Current.RestoreLastPlaylist != false)
                ServiceLocator.PlaylistService?.SaveSession();

            ServiceLocator.MediaService?.Dispose();
            Log.Info("App", "Exiting with code {Code}", e.ApplicationExitCode);
            Log.Shutdown();
            base.OnExit(e);
        }
    }
}
