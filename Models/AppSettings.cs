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
        // Per-file resume positions. Key = full file path; value = saved time in ms.
        public Dictionary<string, long> WatchHistory { get; set; } = new();
        public bool EnableHardwareAcceleration { get; set; } = true;

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
        public int SubtitleFontSize { get; set; } = 0;          // 0 = auto (VLC default); otherwise 14–48 px
        public string SubtitleFontFamily { get; set; } = "";    // "" = VLC default font
        public int SubtitleColorRgb { get; set; } = 0xFFFFFF;   // 24-bit RGB, default white
        public int SubtitleOutlineThickness { get; set; } = 4;  // 0 – 10
        public int SubtitleBackgroundOpacity { get; set; } = 0; // 0 – 255 (0 = transparent)

        // ─── Video Zoom (Phase 12.4) ──────────────────────────────────────
        public float VideoScale { get; set; } = 0f;   // 0 = auto-fit (LibVLC fits video to window)
    }
}
