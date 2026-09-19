using System;

namespace DarshanPlayer.Models
{
    /// <summary>One chapter marker from the current media.</summary>
    public class ChapterInfo
    {
        public int Index { get; set; }
        public string Title { get; set; } = string.Empty;

        /// <summary>Offset from the start of the media, in milliseconds.</summary>
        public long StartMs { get; set; }

        /// <summary>Chapter length in milliseconds.</summary>
        public long DurationMs { get; set; }

        /// <summary>Label for the chapter list: "1. Opening (0:00)".</summary>
        public string DisplayLabel =>
            $"{Index + 1}. {(string.IsNullOrWhiteSpace(Title) ? $"Chapter {Index + 1}" : Title)}" +
            $" ({FormatOffset(StartMs)})";

        public static string FormatOffset(long ms)
        {
            var t = TimeSpan.FromMilliseconds(Math.Max(0, ms));
            return t.TotalHours >= 1
                ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
                : $"{t.Minutes}:{t.Seconds:D2}";
        }
    }

    /// <summary>Downmix applied to the audio output.</summary>
    public enum AudioChannelMode
    {
        Stereo = 1,
        ReverseStereo = 2,
        Left = 3,
        Right = 4,
        Mono = 5,
    }
}
