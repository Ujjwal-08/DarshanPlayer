using System;
using System.Collections.Generic;
using System.Linq;
using DarshanPlayer.Models;

namespace DarshanPlayer.Services
{
    /// <summary>
    /// Owns per-file resume positions: when to record one, when it is worth offering back to the
    /// user, and which entries to drop once the store grows past <see cref="MaxEntries"/>.
    ///
    /// Deliberately has no I/O of its own — it mutates the dictionary handed to it (which lives on
    /// <see cref="AppSettings"/>), so persistence stays the settings layer's job and every rule
    /// here is directly unit-testable.
    /// </summary>
    public class WatchHistoryService
    {
        /// <summary>Hard cap on remembered files. Oldest-played entries are evicted first.</summary>
        public const int MaxEntries = 100;

        /// <summary>
        /// Positions at or beyond this fraction of the duration count as "finished" and are not
        /// offered for resume — nobody wants to be asked to resume the closing credits.
        /// </summary>
        public const double ResumeThresholdFraction = 0.95;

        /// <summary>
        /// Below this, resuming is more annoying than just restarting.
        /// </summary>
        public const long MinimumResumePositionMs = 5_000;

        private readonly Dictionary<string, WatchHistoryEntry> _entries;

        public WatchHistoryService(Dictionary<string, WatchHistoryEntry> entries)
        {
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        public int Count => _entries.Count;

        /// <summary>
        /// Record (or refresh) the position for <paramref name="path"/>, then evict down to
        /// <see cref="MaxEntries"/> if needed.
        /// </summary>
        /// <param name="nowUtc">Injectable clock; tests use it to build deterministic recency.</param>
        public void Record(string path, long positionMs, long durationMs, DateTime? nowUtc = null)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (positionMs < 0) positionMs = 0;

            var key = Normalize(path);
            _entries[key] = new WatchHistoryEntry
            {
                PositionMs = positionMs,
                // Never let a later 0-length report erase a duration we already knew.
                DurationMs = durationMs > 0
                    ? durationMs
                    : (_entries.TryGetValue(key, out var prior) ? prior.DurationMs : 0),
                LastPlayedUtc = nowUtc ?? DateTime.UtcNow,
            };

            Evict();
        }

        /// <summary>
        /// True when there is a position worth resuming from. False for unknown files, positions
        /// in the first few seconds, and anything effectively finished.
        /// </summary>
        public bool TryGetResumePosition(string path, out long positionMs)
        {
            positionMs = 0;
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (!_entries.TryGetValue(Normalize(path), out var entry)) return false;
            if (entry.PositionMs < MinimumResumePositionMs) return false;

            // Duration unknown (older entry, or backend never reported it): fall back to offering
            // the resume rather than silently dropping it.
            if (entry.DurationMs > 0 &&
                entry.PositionMs >= entry.DurationMs * ResumeThresholdFraction)
            {
                return false;
            }

            positionMs = entry.PositionMs;
            return true;
        }

        /// <summary>Forget one file, e.g. once it has been played to the end.</summary>
        public void Forget(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            _entries.Remove(Normalize(path));
        }

        /// <summary>
        /// Fold a legacy <c>path -&gt; positionMs</c> map into the richer store. Existing entries
        /// win, so migration is idempotent and safe to run on every load. Migrated entries get
        /// <see cref="DateTime.MinValue"/> recency, making them the first to be evicted.
        /// </summary>
        /// <returns>Number of entries actually migrated.</returns>
        public int MigrateLegacy(Dictionary<string, long>? legacy)
        {
            if (legacy == null || legacy.Count == 0) return 0;

            int migrated = 0;
            foreach (var (path, positionMs) in legacy)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                var key = Normalize(path);
                if (_entries.ContainsKey(key)) continue;

                _entries[key] = new WatchHistoryEntry
                {
                    PositionMs = positionMs,
                    DurationMs = 0, // unknown in the legacy format
                    LastPlayedUtc = DateTime.MinValue,
                };
                migrated++;
            }

            Evict();
            return migrated;
        }

        private void Evict()
        {
            if (_entries.Count <= MaxEntries) return;

            // Genuine LRU: drop the least-recently-played entries, not whatever the dictionary
            // happens to enumerate first (the previous behaviour, which was effectively random).
            foreach (var key in _entries
                         .OrderBy(kv => kv.Value.LastPlayedUtc)
                         .Take(_entries.Count - MaxEntries)
                         .Select(kv => kv.Key)
                         .ToList())
            {
                _entries.Remove(key);
            }
        }

        // Windows paths are case-insensitive, and the backing dictionary comes from JSON with a
        // default ordinal comparer, so fold the key here instead. Without this the same file
        // opened via a differently cased path would occupy two slots and resume from the wrong one.
        private static string Normalize(string path) => path.Trim().ToLowerInvariant();
    }
}
