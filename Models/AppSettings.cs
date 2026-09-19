using System.Collections.Generic;

namespace DarshanPlayer.Models
{
    public class AppSettings
    {
        public int Volume { get; set; } = 80;
        public string Language { get; set; } = "en";
        public bool AlwaysOnTop { get; set; } = false;
        public double WindowWidth { get; set; } = 1100;
        public double WindowHeight { get; set; } = 700;
        public double? WindowLeft { get; set; }
        public double? WindowTop { get; set; }
        public bool StartMaximized { get; set; } = false;
        public List<string> RecentFiles { get; set; } = new();
        public List<string> RecentFolders { get; set; } = new();
        public RepeatMode RepeatMode { get; set; } = RepeatMode.None;
        public bool IsShuffle { get; set; } = false;
        public float PlaybackRate { get; set; } = 1.0f;
        public string LastPlayedFile { get; set; } = string.Empty;
        public long LastPlaybackPosition { get; set; } = 0;
        // Legacy per-file resume positions (path -> ms). Kept so existing settings.json files
        // still load; WatchHistoryService folds these into WatchHistoryEntries on startup.
        public Dictionary<string, long> WatchHistory { get; set; } = new();

        // Per-file resume state. Key = lower-cased full file path.
        public Dictionary<string, WatchHistoryEntry> WatchHistoryEntries { get; set; } = new();
        public bool EnableHardwareAcceleration { get; set; } = true;

        /// <summary>
        /// Open files launched from Explorer in the already-running window rather than a new one.
        /// </summary>
        public bool SingleInstance { get; set; } = true;

        /// <summary>Write Debug-level traces (input, window transitions) to the log file.</summary>
        public bool VerboseLogging { get; set; } = false;

        // Re-open the previous session's playlist on launch (Phase 13.3).
        public bool RestoreLastPlaylist { get; set; } = true;

        // ─── Video Adjustments (Phase 12.1) ──────────────────────────────
        // Applied live via the LibVLC "adjust" video filter. Defaults are the
        // neutral/no-op values for each control.
        public float Brightness { get; set; } = 1.0f;   // 0.0 – 2.0  (1 = unchanged)
        public float Contrast { get; set; } = 1.0f;     // 0.0 – 2.0  (1 = unchanged)
        public float Saturation { get; set; } = 1.0f;   // 0.0 – 3.0  (1 = unchanged)
        public float Gamma { get; set; } = 1.0f;        // 0.01 – 10.0 (1 = unchanged)
        public float Hue { get; set; } = 0.0f;          // -180 – 180 degrees (0 = unchanged)

        // ─── Subtitle Appearance (Phase 10.3) ────────────────────────────
        // Applied as freetype-* media options when a file is opened, so changes
        // take effect on the next opened media.
        public int SubtitleFontSize { get; set; } = 0;          // 0 = auto (VLC default); otherwise 14–60 px

        // Number of frames the ",", "." keys / frame-step buttons jump (configurable in Settings).
        public int FrameStepCount { get; set; } = 20;
        public string SubtitleFontFamily { get; set; } = "";    // "" = VLC default font
        public int SubtitleColorRgb { get; set; } = 0xFFFFFF;   // 24-bit RGB, default white
        public int SubtitleOutlineThickness { get; set; } = 4;  // 0 – 10
        public int SubtitleBackgroundOpacity { get; set; } = 0; // 0 – 255 (0 = transparent)

        // ─── Video Zoom (Phase 12.4) ──────────────────────────────────────
        public float VideoScale { get; set; } = 0f;   // 0 = auto-fit (LibVLC fits video to window)

        // ─── Video geometry (Phases 12.2, 12.3, 12.5, 12.6) ───────────────
        public string AspectRatio { get; set; } = "Default";
        public string CropGeometry { get; set; } = "Off";
        public string DeinterlaceMode { get; set; } = "Off";

        // ─── Audio (Phases 11.3, 11.4, 11.5) ──────────────────────────────
        public bool EqualizerEnabled { get; set; } = false;
        public string EqualizerPreset { get; set; } = "Flat";
        /// <summary>Last hand-tuned curve, restored when the preset is set to "Custom".</summary>
        public EqualizerProfile EqualizerCustom { get; set; } = new();
        public bool AudioNormalization { get; set; } = false;
        public AudioChannelMode AudioChannel { get; set; } = AudioChannelMode.Stereo;

    }
}
