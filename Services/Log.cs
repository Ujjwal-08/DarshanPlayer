using System;
using System.Diagnostics;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace DarshanPlayer.Services
{
    /// <summary>
    /// Application-wide structured logging (checklist A21).
    ///
    /// Writes to <c>%LocalAppData%\DarshanPlayer\logs\darshan-YYYYMMDD.log</c>, rolling daily and at
    /// 10MB, keeping a week of files. Every method is failure-tolerant: logging must never be able
    /// to take down playback, so a broken log sink degrades to <see cref="Debug"/> output.
    ///
    /// Call <see cref="Init"/> once at startup. Calls made before that are not lost — they fall
    /// through to <see cref="Debug"/>.
    /// </summary>
    public static class Log
    {
        private static Logger? _logger;
        private static readonly object InitLock = new();

        // Level is switchable at runtime so verbose tracing can be enabled from settings without
        // rebuilding — the Input/Window traces are what made the fullscreen focus bug findable.
        private static readonly LoggingLevelSwitch LevelSwitch = new(LogEventLevel.Information);

        public static string LogDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DarshanPlayer",
            "logs");

        /// <summary>Path of the current day's log file, for "open log folder" UI and bug reports.</summary>
        public static string CurrentLogFile =>
            Path.Combine(LogDirectory, $"darshan-{DateTime.Now:yyyyMMdd}.log");

        public static bool IsInitialized => _logger != null;

        /// <summary>
        /// Configure the file sink. Safe to call twice; the second call is ignored.
        /// </summary>
        /// <param name="verbose">Include Debug-level entries. Off by default to keep logs readable.</param>
        public static void Init(bool verbose = false)
        {
            lock (InitLock)
            {
                if (_logger != null) return;

                try
                {
                    Directory.CreateDirectory(LogDirectory);
                    SetVerbose(verbose);

                    _logger = new LoggerConfiguration()
                        .MinimumLevel.ControlledBy(LevelSwitch)
                        .WriteTo.File(
                            Path.Combine(LogDirectory, "darshan-.log"),
                            rollingInterval: RollingInterval.Day,
                            rollOnFileSizeLimit: true,
                            fileSizeLimitBytes: 10 * 1024 * 1024,
                            retainedFileCountLimit: 7,
                            // shared: another instance of the app may be writing the same file.
                            shared: true,
                            outputTemplate:
                                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{Category}] {Message:lj}{NewLine}{Exception}")
                        .CreateLogger();

                    Info("App", "─── logging started ─── {File}", CurrentLogFile);
                }
                catch (Exception ex)
                {
                    // A read-only or full disk must not stop the player from running.
                    Debug.WriteLine($"[Log] Init failed, continuing without file logging: {ex.Message}");
                    _logger = null;
                }
            }
        }

        /// <summary>
        /// Turn Debug-level tracing on or off. Takes effect immediately, including for a logger
        /// that has already been created.
        /// </summary>
        public static void SetVerbose(bool on) =>
            LevelSwitch.MinimumLevel = on ? LogEventLevel.Debug : LogEventLevel.Information;

        /// <summary>Flush and release the sink. Call from App.OnExit.</summary>
        public static void Shutdown()
        {
            lock (InitLock)
            {
                try
                {
                    if (_logger == null) return;
                    Info("App", "─── logging stopped ───");
                    _logger.Dispose();
                }
                catch (Exception ex) { Debug.WriteLine($"[Log] Shutdown failed: {ex.Message}"); }
                finally { _logger = null; }
            }
        }

        // ── Call sites ────────────────────────────────────────────────────
        // "category" groups entries by subsystem (Playback, Window, Subtitle, Settings, …) so a
        // log can be filtered down to the area being investigated.

        public static void Debug_(string category, string template, params object?[] values) =>
            Write(LogEventLevel.Debug, category, null, template, values);

        public static void Info(string category, string template, params object?[] values) =>
            Write(LogEventLevel.Information, category, null, template, values);

        public static void Warn(string category, string template, params object?[] values) =>
            Write(LogEventLevel.Warning, category, null, template, values);

        public static void Error(string category, Exception? ex, string template, params object?[] values) =>
            Write(LogEventLevel.Error, category, ex, template, values);

        public static void Fatal(string category, Exception? ex, string template, params object?[] values) =>
            Write(LogEventLevel.Fatal, category, ex, template, values);

        private static void Write(LogEventLevel level, string category, Exception? ex,
                                  string template, object?[] values)
        {
            try
            {
                var logger = _logger;
                if (logger == null)
                {
                    // Pre-Init (or the sink failed): still surface it to the debugger.
                    Debug.WriteLine($"[{level}] [{category}] {template} {string.Join(", ", values)} {ex}");
                    return;
                }

                logger.ForContext("Category", category).Write(level, ex, template, values);
            }
            catch (Exception inner)
            {
                Debug.WriteLine($"[Log] write failed: {inner.Message}");
            }
        }
    }
}
