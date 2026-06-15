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

            // Notification service (toasts). Created early so settings save-failures can surface.
            var notifications = new NotificationService();
            ServiceLocator.Notifications = notifications;
            settings.SaveFailed += (_, ex) => notifications.ShowError($"Could not save settings: {ex.Message}");

            // Language manager
            var lang = new LanguageManager();
            ServiceLocator.LanguageManager = lang;

            // Media service — pass subtitle style settings so freetype is initialized with them
            ServiceLocator.MediaService = new LibVlcMediaService(settings.Current);

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
            MessageBox.Show(
                $"A fatal error occurred:\n\n{ex?.Message ?? e.ExceptionObject?.ToString()}\n\nThe application may need to restart.",
                "Darshan Player – Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Flush any pending debounced settings writes (volume/rate/position from the last few hundred ms).
            // Without this, the user's last setting change can be lost on a fast quit.
            try { ServiceLocator.SettingsService?.FlushPendingSave(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[App] FlushPendingSave failed: {ex.Message}"); }

            ServiceLocator.MediaService?.Dispose();
            base.OnExit(e);
        }
    }
}
