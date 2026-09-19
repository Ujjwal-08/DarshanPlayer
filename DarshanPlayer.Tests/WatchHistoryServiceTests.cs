using System;
using System.Collections.Generic;
using System.Linq;
using DarshanPlayer.Models;
using DarshanPlayer.Services;
using Xunit;

namespace DarshanPlayer.Tests
{
    public class WatchHistoryServiceTests
    {
        private static (WatchHistoryService svc, Dictionary<string, WatchHistoryEntry> store) Make()
        {
            var store = new Dictionary<string, WatchHistoryEntry>();
            return (new WatchHistoryService(store), store);
        }

        // ── Recording ────────────────────────────────────────────────────────

        [Fact]
        public void Record_StoresPositionAndDuration()
        {
            var (svc, store) = Make();
            svc.Record(@"C:\movies\a.mkv", positionMs: 30_000, durationMs: 600_000);

            var entry = Assert.Single(store).Value;
            Assert.Equal(30_000, entry.PositionMs);
            Assert.Equal(600_000, entry.DurationMs);
        }

        [Fact]
        public void Record_IsCaseInsensitiveOnPath()
        {
            var (svc, store) = Make();
            svc.Record(@"C:\Movies\A.mkv", 30_000, 600_000);
            svc.Record(@"c:\movies\a.mkv", 45_000, 600_000);

            // Same file, so one slot — not two entries resuming from different points.
            Assert.Single(store);
            Assert.True(svc.TryGetResumePosition(@"C:\MOVIES\A.MKV", out var pos));
            Assert.Equal(45_000, pos);
        }

        [Fact]
        public void Record_DoesNotLoseKnownDuration_WhenLaterReportedAsZero()
        {
            var (svc, _) = Make();
            svc.Record(@"C:\a.mkv", 10_000, 600_000);
            svc.Record(@"C:\a.mkv", 20_000, 0);

            // Duration must survive, otherwise the 95% rule silently stops applying.
            Assert.True(svc.TryGetResumePosition(@"C:\a.mkv", out _));
            svc.Record(@"C:\a.mkv", 599_000, 0);
            Assert.False(svc.TryGetResumePosition(@"C:\a.mkv", out _));
        }

        [Fact]
        public void Record_IgnoresBlankPaths()
        {
            var (svc, store) = Make();
            svc.Record("   ", 30_000, 600_000);
            svc.Record("", 30_000, 600_000);
            Assert.Empty(store);
        }

        // ── The 95% rule ─────────────────────────────────────────────────────

        [Fact]
        public void TryGetResumePosition_ReturnsPosition_WhenComfortablyMidFile()
        {
            var (svc, _) = Make();
            svc.Record(@"C:\a.mkv", 300_000, 600_000); // 50%

            Assert.True(svc.TryGetResumePosition(@"C:\a.mkv", out var pos));
            Assert.Equal(300_000, pos);
        }

        [Fact]
        public void TryGetResumePosition_RefusesPastNinetyFivePercent()
        {
            var (svc, _) = Make();
            svc.Record(@"C:\a.mkv", 580_000, 600_000); // ~96.7%

            Assert.False(svc.TryGetResumePosition(@"C:\a.mkv", out var pos));
            Assert.Equal(0, pos);
        }

        [Fact]
        public void TryGetResumePosition_RefusesExactlyAtThreshold()
        {
            var (svc, _) = Make();
            svc.Record(@"C:\a.mkv", 570_000, 600_000); // exactly 95%
            Assert.False(svc.TryGetResumePosition(@"C:\a.mkv", out _));
        }

        [Fact]
        public void TryGetResumePosition_AllowsJustUnderThreshold()
        {
            var (svc, _) = Make();
            svc.Record(@"C:\a.mkv", 569_000, 600_000); // 94.8%
            Assert.True(svc.TryGetResumePosition(@"C:\a.mkv", out _));
        }

        [Fact]
        public void TryGetResumePosition_RefusesOpeningSeconds()
        {
            var (svc, _) = Make();
            svc.Record(@"C:\a.mkv", WatchHistoryService.MinimumResumePositionMs - 1, 600_000);
            Assert.False(svc.TryGetResumePosition(@"C:\a.mkv", out _));
        }

        [Fact]
        public void TryGetResumePosition_ResumesWhenDurationUnknown()
        {
            var (svc, _) = Make();
            svc.Record(@"C:\a.mkv", 300_000, durationMs: 0);

            // Can't apply the 95% rule without a duration, so prefer offering the resume.
            Assert.True(svc.TryGetResumePosition(@"C:\a.mkv", out var pos));
            Assert.Equal(300_000, pos);
        }

        [Fact]
        public void TryGetResumePosition_ReturnsFalseForUnknownFile()
        {
            var (svc, _) = Make();
            Assert.False(svc.TryGetResumePosition(@"C:\never-seen.mkv", out _));
        }

        [Fact]
        public void Forget_RemovesEntry()
        {
            var (svc, _) = Make();
            svc.Record(@"C:\a.mkv", 300_000, 600_000);
            svc.Forget(@"C:\A.MKV");
            Assert.False(svc.TryGetResumePosition(@"C:\a.mkv", out _));
        }

        // ── LRU eviction ─────────────────────────────────────────────────────

        [Fact]
        public void Record_CapsStoreAtMaxEntries()
        {
            var (svc, store) = Make();
            var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (int i = 0; i < WatchHistoryService.MaxEntries + 25; i++)
                svc.Record($@"C:\f{i}.mkv", 30_000, 600_000, t0.AddMinutes(i));

            Assert.Equal(WatchHistoryService.MaxEntries, store.Count);
        }

        [Fact]
        public void Record_EvictsLeastRecentlyPlayedFirst()
        {
            var (svc, _) = Make();
            var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            for (int i = 0; i < WatchHistoryService.MaxEntries; i++)
                svc.Record($@"C:\f{i}.mkv", 30_000, 600_000, t0.AddMinutes(i));

            // f0 is the oldest; touching it makes it the newest, so f1 becomes the eviction victim.
            svc.Record(@"C:\f0.mkv", 45_000, 600_000, t0.AddHours(10));
            svc.Record(@"C:\new.mkv", 30_000, 600_000, t0.AddHours(11));

            Assert.True(svc.TryGetResumePosition(@"C:\f0.mkv", out _));
            Assert.False(svc.TryGetResumePosition(@"C:\f1.mkv", out _));
            Assert.True(svc.TryGetResumePosition(@"C:\new.mkv", out _));
        }

        // ── Legacy migration ─────────────────────────────────────────────────

        [Fact]
        public void MigrateLegacy_ImportsOldPositions()
        {
            var (svc, _) = Make();
            var legacy = new Dictionary<string, long>
            {
                [@"C:\old1.mkv"] = 120_000,
                [@"C:\old2.mkv"] = 240_000,
            };

            Assert.Equal(2, svc.MigrateLegacy(legacy));
            Assert.True(svc.TryGetResumePosition(@"C:\old1.mkv", out var pos));
            Assert.Equal(120_000, pos);
        }

        [Fact]
        public void MigrateLegacy_DoesNotOverwriteNewerEntries()
        {
            var (svc, _) = Make();
            svc.Record(@"C:\a.mkv", 300_000, 600_000);
            svc.MigrateLegacy(new Dictionary<string, long> { [@"C:\a.mkv"] = 999 });

            Assert.True(svc.TryGetResumePosition(@"C:\a.mkv", out var pos));
            Assert.Equal(300_000, pos);
        }

        [Fact]
        public void MigrateLegacy_IsIdempotent()
        {
            var (svc, store) = Make();
            var legacy = new Dictionary<string, long> { [@"C:\a.mkv"] = 120_000 };

            Assert.Equal(1, svc.MigrateLegacy(legacy));
            Assert.Equal(0, svc.MigrateLegacy(legacy));
            Assert.Single(store);
        }

        [Fact]
        public void MigrateLegacy_MigratedEntriesAreEvictedBeforeRealOnes()
        {
            var (svc, _) = Make();
            var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var legacy = Enumerable.Range(0, 60)
                .ToDictionary(i => $@"C:\legacy{i}.mkv", _ => 120_000L);
            svc.MigrateLegacy(legacy);

            for (int i = 0; i < 60; i++)
                svc.Record($@"C:\fresh{i}.mkv", 30_000, 600_000, t0.AddMinutes(i));

            // Freshly recorded entries all survive; the undated legacy ones absorb the eviction.
            for (int i = 0; i < 60; i++)
                Assert.True(svc.TryGetResumePosition($@"C:\fresh{i}.mkv", out _));
        }

        [Fact]
        public void MigrateLegacy_HandlesNullAndEmpty()
        {
            var (svc, _) = Make();
            Assert.Equal(0, svc.MigrateLegacy(null));
            Assert.Equal(0, svc.MigrateLegacy(new Dictionary<string, long>()));
        }
    }
}
