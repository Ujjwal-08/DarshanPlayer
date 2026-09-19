using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace DarshanPlayer.Services
{
    /// <summary>
    /// How Windows identifies this app, and the install-time integration shared by Velopack's
    /// hooks, the startup self-repair and the "Set as default player" command.
    /// </summary>
    public static class AppIdentity
    {
        /// <summary>
        /// Must equal the System.AppUserModel.ID Velopack stamps on the Start Menu and Desktop
        /// shortcuts. When the process and a pinned shortcut disagree, Windows shows the running
        /// window as a second, separate taskbar button and cannot attach the jump list.
        /// </summary>
        public const string AppUserModelId = "velopack.DarshanPlayer";

        /// <summary>Call before any window is created.</summary>
        public static void ApplyAppUserModelId()
        {
            try { SetCurrentProcessExplicitAppUserModelID(AppUserModelId); }
            catch (Exception ex) { Debug.WriteLine($"[AppIdentity] AUMID not applied: {ex.Message}"); }
        }

        /// <summary>The running executable's full path.</summary>
        public static string ExePath =>
            Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "DarshanPlayer.exe");

        /// <summary>
        /// True only for a Velopack install (<c>%LocalAppData%\DarshanPlayer\current\DarshanPlayer.exe</c>,
        /// with Update.exe beside <c>current</c>). Automatic registration is limited to this case so a
        /// developer's bin\Debug build, or a portable copy on a USB stick, never silently repoints the
        /// machine's file associations at itself.
        /// </summary>
        public static bool IsInstalledCopy(string? exePath = null)
        {
            try
            {
                var dir = Path.GetDirectoryName(exePath ?? ExePath);
                if (dir == null || !string.Equals(Path.GetFileName(dir), "current", StringComparison.OrdinalIgnoreCase))
                    return false;
                var root = Path.GetDirectoryName(dir);
                return root != null && File.Exists(Path.Combine(root, "Update.exe"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Register with Windows. Safe to repeat; only notifies Explorer when something changed.</summary>
        public static bool RegisterWithWindows(IRegistryStore? store = null)
        {
            try
            {
                var changed = new ShellRegistration(ExePath).Apply(store ?? new CurrentUserRegistryStore());
                if (changed) ShellRegistration.NotifyShell();
                Log.Info("Shell", "Windows registration {Result} for {Exe}", changed ? "updated" : "already current", ExePath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Shell", ex, "Windows registration failed");
                return false;
            }
        }

        /// <summary>Undo <see cref="RegisterWithWindows"/>. Used by the uninstall hook.</summary>
        public static void UnregisterFromWindows(IRegistryStore? store = null)
        {
            try
            {
                ShellRegistration.Remove(store ?? new CurrentUserRegistryStore());
                ShellRegistration.NotifyShell();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppIdentity] Unregister failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Open Windows' Default apps page for Darshan Player. The user makes the choice there;
        /// registering first guarantees the page has something to show, including for portable copies.
        /// </summary>
        public static void OpenDefaultAppsSettings()
        {
            RegisterWithWindows();
            try
            {
                Process.Start(new ProcessStartInfo(ShellRegistration.DefaultAppsSettingsUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Warn("Shell", "Could not open Default apps settings: {Reason}", ex.Message);
                try { Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true }); }
                catch { /* nothing more we can do */ }
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SetCurrentProcessExplicitAppUserModelID(string appId);
    }
}
