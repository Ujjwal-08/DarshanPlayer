using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DarshanPlayer.Services
{
    /// <summary>A registry value, addressed relative to HKEY_CURRENT_USER. Empty name = (Default).</summary>
    public sealed record RegistryValue(string KeyPath, string Name, string Data);

    /// <summary>Minimal registry surface, so registration can be tested without the real registry.</summary>
    public interface IRegistryStore
    {
        string? GetValue(string keyPath, string name);
        void SetValue(string keyPath, string name, string data);
        void DeleteTree(string keyPath);
        void DeleteValue(string keyPath, string name);
    }

    /// <summary>
    /// Registers Darshan Player with Windows everywhere a user can choose a media player, without
    /// ever choosing on their behalf — Windows 10/11 deliberately do not allow apps to make
    /// themselves the default, so the job is to be offered in every place the user looks:
    ///
    ///   • Explorer "Open with" for every supported extension           (OpenWithProgids)
    ///   • "Choose another app" list, with the proper name and icon      (Applications\DarshanPlayer.exe)
    ///   • Settings ▸ Apps ▸ Default apps, as a registered application   (Capabilities + RegisteredApplications)
    ///   • "Add to Darshan Player playlist" on media files               (SystemFileAssociations)
    ///   • "Play with Darshan Player" on folders                         (Directory\shell)
    ///   • Win+R / Start search by exe name                              (App Paths)
    ///
    /// Everything lives under HKCU, matching Velopack's per-user install: no UAC prompt, and one
    /// user's choice never changes another account. Existing file-type keys such as <c>.mp4</c> are
    /// only ever added to, never replaced, so other players' registrations survive install and
    /// uninstall.
    /// </summary>
    public sealed class ShellRegistration
    {
        public const string AppName = "Darshan Player";
        public const string RegisteredAppName = "DarshanPlayer";
        public const string VideoProgId = "DarshanPlayer.Video";
        public const string AudioProgId = "DarshanPlayer.Audio";
        public const string EnqueueArgument = "--enqueue";

        private const string Classes = @"Software\Classes";
        private const string CapabilitiesKey = @"Software\DarshanPlayer\Capabilities";
        private const string RegisteredApplicationsKey = @"Software\RegisteredApplications";
        private const string AppPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths\DarshanPlayer.exe";
        private const string ApplicationsKey = Classes + @"\Applications\DarshanPlayer.exe";
        private const string FolderVerbKey = Classes + @"\Directory\shell\DarshanPlayer.Play";
        private const string EnqueueVerbName = "DarshanPlayer.Enqueue";

        private readonly string _exePath;

        public ShellRegistration(string exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentException("Executable path is required.", nameof(exePath));
            _exePath = exePath;
        }

        private string Icon => $"\"{_exePath}\",0";
        private string OpenCommand => $"\"{_exePath}\" \"%1\"";
        private string EnqueueCommand => $"\"{_exePath}\" {EnqueueArgument} \"%1\"";

        /// <summary>Every value written on install, in a deterministic order.</summary>
        public IReadOnlyList<RegistryValue> Values()
        {
            var v = new List<RegistryValue>();
            void Set(string key, string name, string data) => v.Add(new RegistryValue(key, name, data));

            // ── ProgIDs: how Darshan-opened files look and launch ─────────────
            foreach (var (progId, typeName) in new[] { (VideoProgId, "Video file"), (AudioProgId, "Audio file") })
            {
                var key = $@"{Classes}\{progId}";
                Set(key, "", typeName);
                Set(key, "FriendlyTypeName", typeName);
                Set($@"{key}\DefaultIcon", "", Icon);
                Set($@"{key}\shell", "", "open");
                Set($@"{key}\shell\open", "FriendlyAppName", AppName);
                // "Player" lifts Explorer's 15-item cap, so a large selection still offers the verb.
                Set($@"{key}\shell\open", "MultiSelectModel", "Player");
                Set($@"{key}\shell\open\command", "", OpenCommand);
            }

            foreach (var ext in MediaFormats.AllExtensions)
            {
                var progId = MediaFormats.AudioExtensions.Contains(ext) ? AudioProgId : VideoProgId;

                // ── "Open with" candidate. A value, not a key: other apps share this key. ──
                Set($@"{Classes}\{ext}\OpenWithProgids", progId, "");

                // ── "Add to playlist" on the classic context menu, whatever the default app is ──
                var verb = $@"{Classes}\SystemFileAssociations\{ext}\shell\{EnqueueVerbName}";
                Set(verb, "", "Add to Darshan Player playlist");
                Set(verb, "Icon", Icon);
                Set(verb, "MultiSelectModel", "Player");
                Set($@"{verb}\command", "", EnqueueCommand);
            }

            // ── "Choose another app": proper name, icon and the types we accept ──
            Set(ApplicationsKey, "FriendlyAppName", AppName);
            Set($@"{ApplicationsKey}\DefaultIcon", "", Icon);
            Set($@"{ApplicationsKey}\shell\open\command", "", OpenCommand);
            foreach (var ext in MediaFormats.AllExtensions)
                Set($@"{ApplicationsKey}\SupportedTypes", ext, "");

            // ── Settings ▸ Default apps ──────────────────────────────────────
            Set(CapabilitiesKey, "ApplicationName", AppName);
            Set(CapabilitiesKey, "ApplicationDescription", "A fast, free media player for Windows.");
            Set(CapabilitiesKey, "ApplicationIcon", Icon);
            foreach (var ext in MediaFormats.AllExtensions)
            {
                var progId = MediaFormats.AudioExtensions.Contains(ext) ? AudioProgId : VideoProgId;
                Set($@"{CapabilitiesKey}\FileAssociations", ext, progId);
            }
            Set(RegisteredApplicationsKey, RegisteredAppName, CapabilitiesKey);

            // ── Folders: play everything inside ─────────────────────────────
            Set(FolderVerbKey, "", "Play with Darshan Player");
            Set(FolderVerbKey, "Icon", Icon);
            Set($@"{FolderVerbKey}\command", "", OpenCommand);

            // ── Win+R "DarshanPlayer" ────────────────────────────────────────
            Set(AppPathsKey, "", _exePath);
            Set(AppPathsKey, "Path", Path.GetDirectoryName(_exePath) ?? "");

            return v;
        }

        /// <summary>Keys owned outright by Darshan Player; removed whole on uninstall.</summary>
        public static IReadOnlyList<string> OwnedKeys()
        {
            var keys = new List<string>
            {
                $@"{Classes}\{VideoProgId}",
                $@"{Classes}\{AudioProgId}",
                ApplicationsKey,
                @"Software\DarshanPlayer",
                FolderVerbKey,
                AppPathsKey,
            };
            keys.AddRange(MediaFormats.AllExtensions.Select(ext =>
                $@"{Classes}\SystemFileAssociations\{ext}\shell\{EnqueueVerbName}"));
            return keys;
        }

        /// <summary>Values Darshan Player adds to keys it shares with other apps; only these are removed.</summary>
        public static IReadOnlyList<(string KeyPath, string Name)> SharedValues()
        {
            var values = MediaFormats.AllExtensions
                .SelectMany(ext => new[]
                {
                    ($@"{Classes}\{ext}\OpenWithProgids", VideoProgId),
                    ($@"{Classes}\{ext}\OpenWithProgids", AudioProgId),
                })
                .ToList();
            values.Add((RegisteredApplicationsKey, RegisteredAppName));
            return values;
        }

        /// <summary>
        /// Write any value that is missing or stale. Idempotent, so it is safe on every launch.
        /// </summary>
        /// <returns>True when anything changed — the caller should then notify the shell.</returns>
        public bool Apply(IRegistryStore store)
        {
            bool changed = false;
            foreach (var value in Values())
            {
                if (store.GetValue(value.KeyPath, value.Name) == value.Data) continue;
                store.SetValue(value.KeyPath, value.Name, value.Data);
                changed = true;
            }
            return changed;
        }

        /// <summary>True when every value is present and points at this executable.</summary>
        public bool IsRegistered(IRegistryStore store) =>
            Values().All(v => store.GetValue(v.KeyPath, v.Name) == v.Data);

        /// <summary>Remove everything <see cref="Apply"/> wrote, leaving other apps' entries intact.</summary>
        public static void Remove(IRegistryStore store)
        {
            foreach (var key in OwnedKeys())
                store.DeleteTree(key);
            foreach (var (key, name) in SharedValues())
                store.DeleteValue(key, name);
        }

        // ── Windows glue ─────────────────────────────────────────────────

        /// <summary>
        /// Opens Settings on Darshan Player's own Default-apps page (Windows 11), or the general
        /// Default apps page where that deep link is not supported. Windows requires the user to
        /// make the final choice, which is exactly what this leaves them to do.
        /// </summary>
        public const string DefaultAppsSettingsUri =
            "ms-settings:defaultapps?registeredAppUser=" + RegisteredAppName;

        /// <summary>Tell Explorer file associations changed, so menus and icons refresh immediately.</summary>
        public static void NotifyShell()
        {
            const int SHCNE_ASSOCCHANGED = 0x08000000;
            const uint SHCNF_IDLIST = 0x0000;
            try { SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero); }
            catch (Exception ex) { Log.Warn("Shell", "SHChangeNotify failed: {Reason}", ex.Message); }
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);
    }

    /// <summary>The real per-user registry.</summary>
    public sealed class CurrentUserRegistryStore : IRegistryStore
    {
        public string? GetValue(string keyPath, string name)
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            return key?.GetValue(name) as string;
        }

        public void SetValue(string keyPath, string name, string data)
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
            key.SetValue(name, data, RegistryValueKind.String);
        }

        public void DeleteTree(string keyPath) =>
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);

        public void DeleteValue(string keyPath, string name)
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
    }
}
