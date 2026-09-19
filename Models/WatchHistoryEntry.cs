using System;

namespace DarshanPlayer.Models
{
    /// <summary>
    /// One remembered playback position. Replaces the old <c>Dictionary&lt;string, long&gt;</c>
    /// form, which stored only a position and so could neither apply the "don't offer to resume
    /// something you already finished" rule nor evict by genuine recency.
    /// </summary>
    public class WatchHistoryEntry
    {
        /// <summary>Saved playback position, in milliseconds.</summary>
        public long PositionMs { get; set; }

        /// <summary>
        /// Total media length in milliseconds, or 0 when it was never reported by the backend.
        /// Needed to decide whether <see cref="PositionMs"/> is "near the end".
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>When this entry was last written. Drives LRU eviction.</summary>
        public DateTime LastPlayedUtc { get; set; }
    }
}
