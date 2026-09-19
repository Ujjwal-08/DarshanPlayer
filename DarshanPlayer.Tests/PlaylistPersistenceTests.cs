using System;
using System.IO;
using System.Linq;
using DarshanPlayer.Services;
using Xunit;

namespace DarshanPlayer.Tests
{
    /// <summary>
    /// Covers M3U round-tripping and the session save/restore added for Phase 13.3.
    /// Uses a real temp directory because the service talks to <see cref="File"/> directly;
    /// swapping in an IFileSystem abstraction is tracked separately in the checklist.
    /// </summary>
    public class PlaylistPersistenceTests : IDisposable
    {
        private readonly string _dir;

        public PlaylistPersistenceTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "DarshanPlayerTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string MakeFile(string name)
        {
            var path = Path.Combine(_dir, name);
            File.WriteAllText(path, "not really media");
            return path;
        }

        [Fact]
        public void SaveM3U_WritesExtM3UHeaderAndOneLinePerItem()
        {
            var svc = new PlaylistService();
            svc.Add(@"C:\a.mp4");
            svc.Add(@"C:\b.mkv");

            var target = Path.Combine(_dir, "out.m3u8");
            svc.SaveM3U(target);

            var lines = File.ReadAllLines(target);
            Assert.Equal("#EXTM3U", lines[0]);
            Assert.Equal(@"C:\a.mp4", lines[1]);
            Assert.Equal(@"C:\b.mkv", lines[2]);
            Assert.Equal(3, lines.Length);
        }

        [Fact]
        public void LoadM3U_ParsesEntriesAndSkipsCommentsAndBlanks()
        {
            var target = Path.Combine(_dir, "in.m3u8");
            File.WriteAllLines(target, new[]
            {
                "#EXTM3U",
                "",
                @"C:\a.mp4",
                "#EXTINF:123,Some Title",
                @"  C:\b.mkv  ",
            });

            var svc = new PlaylistService();
            svc.LoadM3U(target);

            Assert.Equal(2, svc.Items.Count);
            Assert.Equal(@"C:\a.mp4", svc.Items[0].FilePath);
            Assert.Equal(@"C:\b.mkv", svc.Items[1].FilePath);
        }

        [Fact]
        public void SaveThenLoad_RoundTripsPlaylistOrder()
        {
            var svc = new PlaylistService();
            svc.Add(@"C:\1.mp4");
            svc.Add(@"C:\2.mp4");
            svc.Add(@"C:\3.mp4");

            var target = Path.Combine(_dir, "rt.m3u8");
            svc.SaveM3U(target);

            var restored = new PlaylistService();
            restored.LoadM3U(target);

            Assert.Equal(
                svc.Items.Select(i => i.FilePath),
                restored.Items.Select(i => i.FilePath));
        }

        [Fact]
        public void LoadM3U_WithSkipMissing_DropsEntriesThatNoLongerExist()
        {
            var live = MakeFile("live.mp4");
            var target = Path.Combine(_dir, "mixed.m3u8");
            File.WriteAllLines(target, new[]
            {
                "#EXTM3U",
                live,
                @"D:\removable\gone.mkv",
            });

            var svc = new PlaylistService();
            svc.LoadM3U(target, skipMissing: true);

            var item = Assert.Single(svc.Items);
            Assert.Equal(live, item.FilePath);
        }

        [Fact]
        public void LoadM3U_WithoutSkipMissing_KeepsEveryEntry()
        {
            var target = Path.Combine(_dir, "all.m3u8");
            File.WriteAllLines(target, new[] { "#EXTM3U", @"D:\gone1.mkv", @"D:\gone2.mkv" });

            var svc = new PlaylistService();
            svc.LoadM3U(target, skipMissing: false);

            Assert.Equal(2, svc.Items.Count);
        }

        [Fact]
        public void LoadM3U_DoesNotDuplicateEntriesAlreadyPresent()
        {
            var target = Path.Combine(_dir, "dup.m3u8");
            File.WriteAllLines(target, new[] { "#EXTM3U", @"C:\a.mp4", @"c:\A.MP4" });

            var svc = new PlaylistService();
            svc.LoadM3U(target);

            Assert.Single(svc.Items);
        }

        [Fact]
        public void RestoreSession_ReturnsZeroWhenNoSessionFileExists()
        {
            var svc = new PlaylistService();
            // SessionPlaylistPath points at the real %AppData% location; when absent this must be a
            // quiet no-op rather than throwing during construction of the view model.
            if (File.Exists(PlaylistService.SessionPlaylistPath)) return;

            Assert.Equal(0, svc.RestoreSession());
            Assert.Empty(svc.Items);
        }

        [Fact]
        public void SessionPlaylistPath_LivesUnderAppDataDarshanPlayer()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Assert.StartsWith(Path.Combine(appData, "DarshanPlayer"), PlaylistService.SessionPlaylistPath);
            Assert.EndsWith(".m3u8", PlaylistService.SessionPlaylistPath);
        }
    }
}
