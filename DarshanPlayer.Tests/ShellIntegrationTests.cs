using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarshanPlayer.Services;
using Xunit;

namespace DarshanPlayer.Tests
{
    /// <summary>In-memory registry: keys are case-insensitive, like the real one.</summary>
    internal sealed class FakeRegistryStore : IRegistryStore
    {
        private readonly Dictionary<string, Dictionary<string, string>> _keys =
            new(StringComparer.OrdinalIgnoreCase);

        public int Writes { get; private set; }

        public string? GetValue(string keyPath, string name) =>
            _keys.TryGetValue(keyPath, out var values) && values.TryGetValue(name, out var data) ? data : null;

        public void SetValue(string keyPath, string name, string data)
        {
            if (!_keys.TryGetValue(keyPath, out var values))
                _keys[keyPath] = values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            values[name] = data;
            Writes++;
        }

        public void DeleteTree(string keyPath)
        {
            foreach (var k in _keys.Keys.Where(k =>
                         k.Equals(keyPath, StringComparison.OrdinalIgnoreCase) ||
                         k.StartsWith(keyPath + @"\", StringComparison.OrdinalIgnoreCase)).ToList())
                _keys.Remove(k);
        }

        public void DeleteValue(string keyPath, string name)
        {
            if (_keys.TryGetValue(keyPath, out var values)) values.Remove(name);
        }

        public bool KeyExists(string keyPath) => _keys.ContainsKey(keyPath);
        public int ValueCount => _keys.Values.Sum(v => v.Count);
    }

    public class ShellRegistrationTests
    {
        private const string Exe = @"C:\Users\test\AppData\Local\DarshanPlayer\current\DarshanPlayer.exe";
        private readonly ShellRegistration _reg = new(Exe);

        private string? Value(string key, string name) =>
            _reg.Values().FirstOrDefault(v => v.KeyPath == key && v.Name == name)?.Data;

        [Theory]
        [InlineData(".mp4", ShellRegistration.VideoProgId)]
        [InlineData(".mkv", ShellRegistration.VideoProgId)]
        [InlineData(".mp3", ShellRegistration.AudioProgId)]
        [InlineData(".flac", ShellRegistration.AudioProgId)]
        public void EveryExtensionIsOfferedInOpenWith_UnderTheRightProgId(string ext, string progId)
        {
            Assert.Equal("", Value($@"Software\Classes\{ext}\OpenWithProgids", progId));
            Assert.Equal(progId, Value(@"Software\DarshanPlayer\Capabilities\FileAssociations", ext));
        }

        [Fact]
        public void AllSupportedExtensionsAreRegistered()
        {
            foreach (var ext in MediaFormats.AllExtensions)
                Assert.Contains(_reg.Values(), v => v.KeyPath == $@"Software\Classes\{ext}\OpenWithProgids");
        }

        [Theory]
        [InlineData(".jpg")]
        [InlineData(".png")]
        [InlineData(".gif")]
        public void ImagesAreNotClaimed(string ext)
        {
            // The old Inno installer registered images, which the player cannot open.
            Assert.DoesNotContain(_reg.Values(), v => v.KeyPath.Contains($@"\{ext}\", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void OpenCommandQuotesExeAndFile()
        {
            Assert.Equal($"\"{Exe}\" \"%1\"",
                Value($@"Software\Classes\{ShellRegistration.VideoProgId}\shell\open\command", ""));
        }

        [Fact]
        public void EnqueueVerbPassesTheEnqueueSwitch()
        {
            Assert.Equal($"\"{Exe}\" --enqueue \"%1\"",
                Value(@"Software\Classes\SystemFileAssociations\.mp4\shell\DarshanPlayer.Enqueue\command", ""));
        }

        [Fact]
        public void RegistersForWindowsDefaultApps()
        {
            Assert.Equal(@"Software\DarshanPlayer\Capabilities",
                Value(@"Software\RegisteredApplications", ShellRegistration.RegisteredAppName));
            Assert.Equal("Darshan Player", Value(@"Software\DarshanPlayer\Capabilities", "ApplicationName"));
        }

        [Fact]
        public void RegistersAppPathsAndFolderVerb()
        {
            Assert.Equal(Exe, Value(@"Software\Microsoft\Windows\CurrentVersion\App Paths\DarshanPlayer.exe", ""));
            Assert.Equal("Play with Darshan Player", Value(@"Software\Classes\Directory\shell\DarshanPlayer.Play", ""));
        }

        [Fact]
        public void EverythingIsPerUser_NothingTouchesMachineWideHives()
        {
            // All paths are HKCU-relative; none should reach for HKLM-style roots.
            Assert.All(_reg.Values(), v => Assert.StartsWith("Software\\", v.KeyPath));
        }

        [Fact]
        public void Apply_WritesThenIsRegistered()
        {
            var store = new FakeRegistryStore();
            Assert.False(_reg.IsRegistered(store));

            Assert.True(_reg.Apply(store));
            Assert.True(_reg.IsRegistered(store));
        }

        [Fact]
        public void Apply_IsIdempotent_SoItIsSafeOnEveryLaunch()
        {
            var store = new FakeRegistryStore();
            _reg.Apply(store);
            var writesAfterFirst = store.Writes;

            Assert.False(_reg.Apply(store));
            Assert.Equal(writesAfterFirst, store.Writes);
        }

        [Fact]
        public void Apply_RepairsAssociationsWhenTheExeMoves()
        {
            var store = new FakeRegistryStore();
            new ShellRegistration(@"D:\old\DarshanPlayer.exe").Apply(store);

            Assert.False(_reg.IsRegistered(store));
            Assert.True(_reg.Apply(store));
            Assert.True(_reg.IsRegistered(store));
        }

        [Fact]
        public void Remove_ClearsEverythingItWrote()
        {
            var store = new FakeRegistryStore();
            _reg.Apply(store);

            ShellRegistration.Remove(store);

            Assert.Equal(0, store.ValueCount);
        }

        [Fact]
        public void Remove_LeavesOtherPlayersRegistrationsIntact()
        {
            var store = new FakeRegistryStore();
            store.SetValue(@"Software\Classes\.mp4\OpenWithProgids", "VLC.mp4", "");
            store.SetValue(@"Software\RegisteredApplications", "VLC", @"Software\Clients\Media\VLC\Capabilities");
            _reg.Apply(store);

            ShellRegistration.Remove(store);

            Assert.Equal("", store.GetValue(@"Software\Classes\.mp4\OpenWithProgids", "VLC.mp4"));
            Assert.NotNull(store.GetValue(@"Software\RegisteredApplications", "VLC"));
            Assert.Null(store.GetValue(@"Software\Classes\.mp4\OpenWithProgids", ShellRegistration.VideoProgId));
        }

        [Fact]
        public void Constructor_RejectsBlankExePath()
        {
            Assert.Throws<ArgumentException>(() => new ShellRegistration(" "));
        }

        [Fact]
        public void DefaultAppsUriTargetsThisApp()
        {
            Assert.Equal("ms-settings:defaultapps?registeredAppUser=DarshanPlayer",
                ShellRegistration.DefaultAppsSettingsUri);
        }
    }

    public class LaunchRequestTests
    {
        [Fact]
        public void Parse_CollectsPaths()
        {
            var r = LaunchRequest.Parse(new[] { @"C:\a.mp4", @"C:\b.mkv" });
            Assert.Equal(new[] { @"C:\a.mp4", @"C:\b.mkv" }, r.Paths);
            Assert.False(r.Enqueue);
        }

        [Fact]
        public void Parse_RecognisesEnqueue()
        {
            var r = LaunchRequest.Parse(new[] { "--enqueue", @"C:\a.mp4" });
            Assert.True(r.Enqueue);
            Assert.Equal(new[] { @"C:\a.mp4" }, r.Paths);
        }

        [Fact]
        public void Parse_IgnoresUnknownSwitchesSuchAsVelopacks()
        {
            var r = LaunchRequest.Parse(new[] { "--veloapp-firstrun", @"C:\a.mp4" });
            Assert.Equal(new[] { @"C:\a.mp4" }, r.Paths);
        }

        [Fact]
        public void Parse_HandlesNullAndBlank()
        {
            Assert.False(LaunchRequest.Parse(null).HasPaths);
            Assert.False(LaunchRequest.Parse(new[] { "", "  " }).HasPaths);
        }

        [Fact]
        public void Serialize_RoundTrips()
        {
            var original = new LaunchRequest(new[] { @"C:\My Videos\a b.mp4", @"D:\x.mkv" }, true);
            var copy = LaunchRequest.Deserialize(original.Serialize());

            Assert.Equal(original.Paths, copy.Paths);
            Assert.True(copy.Enqueue);
        }

        [Fact]
        public void Serialize_RoundTripsARequestWithNoPaths()
        {
            // A plain second launch from the Start Menu: just bring the window forward.
            var copy = LaunchRequest.Deserialize(LaunchRequest.Empty.Serialize());
            Assert.False(copy.HasPaths);
            Assert.False(copy.Enqueue);
        }
    }

    public class OpenBatchPolicyTests
    {
        private DateTime _now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        private OpenBatchPolicy Make() => new(() => _now, TimeSpan.FromSeconds(2));
        private static LaunchRequest Open => new(new[] { @"C:\a.mp4" }, false);

        [Fact]
        public void FirstOpenPlays()
        {
            Assert.False(Make().ShouldEnqueue(Open));
        }

        [Fact]
        public void MultiSelectBurst_PlaysFirstAndQueuesTheRest()
        {
            var policy = Make();
            Assert.False(policy.ShouldEnqueue(Open));
            _now = _now.AddMilliseconds(150);
            Assert.True(policy.ShouldEnqueue(Open));
            _now = _now.AddMilliseconds(150);
            Assert.True(policy.ShouldEnqueue(Open));
        }

        [Fact]
        public void SeparateOpensLaterOnReplacePlayback()
        {
            var policy = Make();
            policy.ShouldEnqueue(Open);
            _now = _now.AddSeconds(30);
            Assert.False(policy.ShouldEnqueue(Open));
        }

        [Fact]
        public void ExplicitEnqueueAlwaysQueues()
        {
            Assert.True(Make().ShouldEnqueue(new LaunchRequest(new[] { @"C:\a.mp4" }, true)));
        }
    }

    public class MediaFormatsTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "DarshanFormats", Guid.NewGuid().ToString("N"));

        public MediaFormatsTests() => Directory.CreateDirectory(_dir);
        public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

        private string Touch(string name)
        {
            var p = Path.Combine(_dir, name);
            File.WriteAllText(p, "x");
            return p;
        }

        [Theory]
        [InlineData(@"C:\a.MP4", true)]
        [InlineData(@"C:\a.m2ts", true)]
        [InlineData(@"C:\a.flac", true)]
        [InlineData(@"C:\a.jpg", false)]
        [InlineData(@"C:\a.txt", false)]
        [InlineData("", false)]
        public void IsMedia(string path, bool expected) => Assert.Equal(expected, MediaFormats.IsMedia(path));

        [Fact]
        public void VideoAndAudioListsDoNotOverlap()
        {
            Assert.Empty(MediaFormats.VideoExtensions.Intersect(MediaFormats.AudioExtensions, StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void ExpandsAFolderToItsMediaInNameOrder()
        {
            Touch("b.mkv"); Touch("a.mp4"); Touch("notes.txt"); Touch("cover.jpg");

            var files = MediaFormats.ExpandToMediaFiles(new[] { _dir });

            Assert.Equal(new[] { "a.mp4", "b.mkv" }, files.Select(Path.GetFileName));
        }

        [Fact]
        public void DropsUnsupportedFilesAndDuplicates()
        {
            var video = Touch("clip.mp4");
            var text = Touch("readme.txt");

            var files = MediaFormats.ExpandToMediaFiles(new[] { video, text, video.ToUpperInvariant() });

            Assert.Single(files);
        }

        [Fact]
        public void StripsQuotesTheShellMayLeaveOnPaths()
        {
            var video = Touch("clip.mp4");
            Assert.Single(MediaFormats.ExpandToMediaFiles(new[] { $"\"{video}\"" }));
        }
    }

    public class AppIdentityTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "DarshanIdentity", Guid.NewGuid().ToString("N"));
        public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

        [Fact]
        public void InstalledLayoutIsRecognised()
        {
            var current = Path.Combine(_root, "current");
            Directory.CreateDirectory(current);
            File.WriteAllText(Path.Combine(_root, "Update.exe"), "");

            Assert.True(AppIdentity.IsInstalledCopy(Path.Combine(current, "DarshanPlayer.exe")));
        }

        [Fact]
        public void DevBuildIsNotAnInstalledCopy()
        {
            Assert.False(AppIdentity.IsInstalledCopy(@"C:\src\DarshanPlayer\bin\Debug\net10.0\win-x64\DarshanPlayer.exe"));
        }

        [Fact]
        public void CurrentFolderWithoutUpdateExeIsNotAnInstalledCopy()
        {
            // e.g. a portable zip someone happened to extract into a folder called "current".
            var current = Path.Combine(_root, "current");
            Directory.CreateDirectory(current);
            Assert.False(AppIdentity.IsInstalledCopy(Path.Combine(current, "DarshanPlayer.exe")));
        }

        [Fact]
        public void AumidMatchesVelopackShortcuts()
        {
            Assert.Equal("velopack.DarshanPlayer", AppIdentity.AppUserModelId);
        }
    }

    public class JumpListServiceTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "DarshanJump", Guid.NewGuid().ToString("N"));
        public JumpListServiceTests() => Directory.CreateDirectory(_dir);
        public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

        [Fact]
        public void SkipsFilesThatNoLongerExist()
        {
            var real = Path.Combine(_dir, "movie.mp4");
            File.WriteAllText(real, "x");

            var tasks = JumpListService.BuildTasks(new[] { real, @"Z:\gone.mp4" }, @"C:\app\DarshanPlayer.exe");

            var task = Assert.Single(tasks);
            Assert.Equal("movie", task.Title);
            Assert.Equal($"\"{real}\"", task.Arguments);
        }

        [Fact]
        public void CapsTheListLength()
        {
            var files = Enumerable.Range(0, 25).Select(i =>
            {
                var p = Path.Combine(_dir, $"f{i}.mp4");
                File.WriteAllText(p, "x");
                return p;
            });

            Assert.Equal(JumpListService.MaxRecentItems,
                JumpListService.BuildTasks(files, @"C:\app\DarshanPlayer.exe").Count);
        }
    }
}
