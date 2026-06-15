using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using Timer = System.Threading.Timer;
using DarshanPlayer.Models;

namespace DarshanPlayer.Services
{
    /// <summary>
    /// Persists <see cref="AppSettings"/> to <c>%AppData%\DarshanPlayer\settings.json</c>.
    ///
    /// Writes are atomic (temp file + replace, with a .bak left behind) and debounced via
    /// <see cref="SaveDebounced"/> so hot paths (TimeChanged, slider drags) don't write 60×/sec.
    /// Failures are surfaced via <see cref="SaveFailed"/> instead of swallowed silently.
    /// </summary>
    public class SettingsService
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DarshanPlayer");
        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");
        private static readonly string TempPath = SettingsPath + ".tmp";
        private static readonly string BackupPath = SettingsPath + ".bak";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            // Forward-compat: silently accept unknown keys when an older build reads a newer file.
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        // Debounce: coalesce rapid SaveDebounced() calls into a single flush after 500 ms idle.
        private const int DebounceMs = 500;
        private readonly object _ioLock = new();
        private Timer? _debounceTimer;

        public AppSettings Current { get; private set; } = new();

        /// <summary>Raised on any I/O exception during <see cref="Save"/>. Wire this to a logger/toast.</summary>
        public event EventHandler<Exception>? SaveFailed;

        public void Load()
        {
            try
            {
                // Prefer the live file; if it's corrupt or missing, try the .bak from the last atomic write.
                if (TryLoadFrom(SettingsPath)) return;
                if (TryLoadFrom(BackupPath))
                {
                    Debug.WriteLine("[SettingsService] Loaded from .bak after main file unreadable.");
                    return;
                }
                Current = new AppSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsService] Load failed entirely: {ex.Message}");
                Current = new AppSettings();
            }
        }

        private bool TryLoadFrom(string path)
        {
            if (!File.Exists(path)) return false;
            try
            {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts);
                if (loaded != null)
                {
                    Current = loaded;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsService] Could not parse {path}: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Synchronous, atomic write. Call from app shutdown, language change, or other
        /// explicit user-driven moments. For hot paths use <see cref="SaveDebounced"/>.
        /// </summary>
        public void Save()
        {
            lock (_ioLock)
            {
                try
                {
                    Directory.CreateDirectory(SettingsDir);
                    var json = JsonSerializer.Serialize(Current, JsonOpts);
                    File.WriteAllText(TempPath, json);

                    if (File.Exists(SettingsPath))
                    {
                        // File.Replace is atomic on NTFS and leaves a .bak behind for recovery.
                        File.Replace(TempPath, SettingsPath, BackupPath, ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Move(TempPath, SettingsPath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SettingsService] Save failed: {ex.Message}");
                    SaveFailed?.Invoke(this, ex);
                    // Best-effort cleanup of an orphaned temp file.
                    try { if (File.Exists(TempPath)) File.Delete(TempPath); } catch { /* ignore */ }
                }
            }
        }

        /// <summary>
        /// Schedule a save after <see cref="DebounceMs"/> ms of idle time. Repeated calls reset the timer
        /// so a burst of edits results in exactly one disk write.
        /// </summary>
        public void SaveDebounced()
        {
            lock (_ioLock)
            {
                _debounceTimer ??= new Timer(_ => Save(), null, Timeout.Infinite, Timeout.Infinite);
                _debounceTimer.Change(DebounceMs, Timeout.Infinite);
            }
        }

        /// <summary>Force any pending debounced write to disk now. Call from app shutdown.</summary>
        public void FlushPendingSave()
        {
            bool needsFlush;
            lock (_ioLock)
            {
                needsFlush = _debounceTimer != null;
                _debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
            if (needsFlush) Save();
        }

        public void AddRecentFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            // Case-insensitive removal so the same file with different casing doesn't duplicate.
            Current.RecentFiles.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            Current.RecentFiles.Insert(0, path);
            if (Current.RecentFiles.Count > 20)
                Current.RecentFiles.RemoveRange(20, Current.RecentFiles.Count - 20);
            SaveDebounced();
        }

        public void AddRecentFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            Current.RecentFolders.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            Current.RecentFolders.Insert(0, path);
            if (Current.RecentFolders.Count > 10)
                Current.RecentFolders.RemoveRange(10, Current.RecentFolders.Count - 10);
            SaveDebounced();
        }
    }
}
