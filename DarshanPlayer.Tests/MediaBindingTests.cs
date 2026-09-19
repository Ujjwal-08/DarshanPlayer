using System;
using System.IO;
using System.Linq;
using DarshanPlayer.Models;
using DarshanPlayer.Services;
using DarshanPlayer.ViewModels;
using Xunit;

namespace DarshanPlayer.Tests
{
    /// <summary>
    /// The "did the right value reach MediaPlayer?" tests the checklist has been carrying for
    /// crop, deinterlace, aspect, zoom, audio routing and delays. These were unwritable until
    /// <see cref="IMediaService"/> could be faked and <see cref="SettingsService"/> could be
    /// pointed away from the user's real settings file.
    /// </summary>
    public class MediaBindingTests : IDisposable
    {
        private readonly string _dir;
        private readonly FakeMediaService _media = new();
        private readonly SettingsService _settings;
        private readonly MainViewModel _vm;

        public MediaBindingTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "DarshanVmTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);

            // Never Load(): a fresh AppSettings keeps each test independent, and the temp directory
            // means SaveDebounced() can't touch the real configuration.
            _settings = new SettingsService(_dir);
            // Session restore reads a machine-wide path; off so tests don't inherit real state.
            _settings.Current.RestoreLastPlaylist = false;

            ServiceLocator.SettingsService = _settings;

            _vm = new MainViewModel(
                _media,
                new PlaylistService(),
                _settings,
                new LanguageManager(),
                new NotificationService());
        }

        public void Dispose()
        {
            _settings.FlushPendingSave();
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        // ── Aspect ratio (12.2) ───────────────────────────────────────────

        [Fact]
        public void AspectRatio_SetsMediaPlayerAspectRatioString()
        {
            _vm.SelectedAspectRatio = "16:9";
            Assert.Equal("16:9", _media.LastAspectRatio);
        }

        [Fact]
        public void AspectRatio_Default_ClearsTheRatio()
        {
            _vm.SelectedAspectRatio = "4:3";
            _vm.SelectedAspectRatio = "Default";
            Assert.Null(_media.LastAspectRatio);
        }

        [Fact]
        public void AspectRatio_Fill_DoesNotPushARatioItself()
        {
            // "Fill" needs the host's live size, which only the view can supply; it must not
            // silently fall through to null the way it used to.
            _vm.SelectedAspectRatio = "16:9";
            var before = _media.AspectRatiosSet.Count;

            _vm.SelectedAspectRatio = "Fill";

            Assert.Equal(before, _media.AspectRatiosSet.Count);
        }

        [Fact]
        public void AspectRatio_Fill_RaisesTheFillRequest()
        {
            var raised = 0;
            _vm.AspectRatioNeedsFill += (_, _) => raised++;

            _vm.SelectedAspectRatio = "Fill";

            Assert.Equal(1, raised);
        }

        [Fact]
        public void CycleAspectRatio_WrapsThroughEveryOption()
        {
            var seen = new System.Collections.Generic.List<string>();
            for (int i = 0; i < MainViewModel.AspectRatios.Count; i++)
            {
                _vm.CycleAspectRatio();
                seen.Add(_vm.SelectedAspectRatio);
            }

            Assert.Equal(MainViewModel.AspectRatios.OrderBy(x => x), seen.OrderBy(x => x));
            Assert.Equal("Default", _vm.SelectedAspectRatio); // back to the start
        }

        // ── Crop (12.3) ───────────────────────────────────────────────────

        [Fact]
        public void Crop_SetsMediaPlayerCropGeometry()
        {
            _vm.SelectedCrop = "16:9";
            Assert.Equal("16:9", _media.CropGeometry);
        }

        [Fact]
        public void Crop_Off_ClearsGeometryRatherThanSendingTheWordOff()
        {
            _vm.SelectedCrop = "4:3";
            _vm.SelectedCrop = "Off";

            // LibVLC expects null to disable cropping; "Off" would be parsed as a ratio.
            Assert.Null(_media.CropGeometry);
        }

        [Fact]
        public void Crop_IsPersisted()
        {
            _vm.SelectedCrop = "2.35:1";
            Assert.Equal("2.35:1", _settings.Current.CropGeometry);
        }

        [Fact]
        public void CropModes_StartWithOffAndContainNoDuplicates()
        {
            Assert.Equal("Off", MainViewModel.CropModes[0]);
            Assert.Equal(MainViewModel.CropModes.Count, MainViewModel.CropModes.Distinct().Count());
        }

        // ── Deinterlace (12.6) ────────────────────────────────────────────

        [Fact]
        public void Deinterlace_SetsMode()
        {
            _vm.SelectedDeinterlace = "Yadif2x";
            Assert.Equal("Yadif2x", _media.DeinterlaceMode);
        }

        [Fact]
        public void Deinterlace_Off_IsPassedThroughForTheServiceToNormalise()
        {
            _vm.SelectedDeinterlace = "Bob";
            _vm.SelectedDeinterlace = "Off";
            Assert.Equal("Off", _media.DeinterlaceMode);
        }

        [Fact]
        public void Deinterlace_IsPersisted()
        {
            _vm.SelectedDeinterlace = "Yadif";
            Assert.Equal("Yadif", _settings.Current.DeinterlaceMode);
        }

        [Fact]
        public void DeinterlaceModes_CoverTheDocumentedSet()
        {
            Assert.Equal(
                new[] { "Off", "Discard", "Blend", "Mean", "Bob", "Linear", "X", "Yadif", "Yadif2x" },
                MainViewModel.DeinterlaceModes);
        }

        // ── Audio normalization (11.4) ────────────────────────────────────

        [Fact]
        public void AudioNormalization_ReachesTheMediaService()
        {
            _vm.AudioNormalization = true;
            Assert.True(_media.AudioNormalization);
        }

        [Fact]
        public void AudioNormalization_IsPersisted()
        {
            _vm.AudioNormalization = true;
            Assert.True(_settings.Current.AudioNormalization);
        }

        // ── Audio channel (11.5) ──────────────────────────────────────────

        [Theory]
        [InlineData(AudioChannelMode.Stereo)]
        [InlineData(AudioChannelMode.Mono)]
        [InlineData(AudioChannelMode.Left)]
        [InlineData(AudioChannelMode.Right)]
        [InlineData(AudioChannelMode.ReverseStereo)]
        public void AudioChannel_SetCorrectly_ForEachMode(AudioChannelMode mode)
        {
            _vm.AudioChannel = mode;
            Assert.Equal(mode, _media.AudioChannel);
            Assert.Equal(mode, _settings.Current.AudioChannel);
        }

        [Fact]
        public void AudioChannelModes_MatchLibVlcChannelNumbering()
        {
            // These values are passed straight to AudioOutputChannel, so the numbering is load-bearing.
            Assert.Equal(1, (int)AudioChannelMode.Stereo);
            Assert.Equal(2, (int)AudioChannelMode.ReverseStereo);
            Assert.Equal(3, (int)AudioChannelMode.Left);
            Assert.Equal(4, (int)AudioChannelMode.Right);
            Assert.Equal(5, (int)AudioChannelMode.Mono);
        }

        // ── Audio delay (11.2) ────────────────────────────────────────────

        [Fact]
        public void AudioDelay_AppliedToMediaPlayer()
        {
            _vm.AudioDelay = 250;
            Assert.Equal(250, _media.AudioDelay);
        }

        [Fact]
        public void AudioDelay_IsClampedToTheSliderRange()
        {
            _vm.AudioDelay = 99_999;
            Assert.Equal(2000, _vm.AudioDelay);

            _vm.AudioDelay = -99_999;
            Assert.Equal(-2000, _vm.AudioDelay);
        }

        [Theory]
        [InlineData(0, "0ms")]
        [InlineData(250, "+250ms")]
        [InlineData(-250, "-250ms")]
        public void AudioDelayLabel_ShowsSign(long delay, string expected)
        {
            _vm.AudioDelay = delay;
            Assert.Equal(expected, _vm.AudioDelayLabel);
        }

        // ── Subtitle delay (10.4) ─────────────────────────────────────────

        [Fact]
        public void SubtitleDelay_AppliedToMediaPlayer()
        {
            _vm.SubtitleDelay = -100;
            Assert.Equal(-100, _media.SubtitleDelay);
        }

        [Fact]
        public void AdjustSubtitleDelay_AccumulatesTheOffset()
        {
            _vm.AdjustSubtitleDelayCommand.Execute("-100");
            _vm.AdjustSubtitleDelayCommand.Execute("-100");
            _vm.AdjustSubtitleDelayCommand.Execute("100");

            Assert.Equal(-100, _media.SubtitleDelay);
        }

        // ── Zoom (12.4) ───────────────────────────────────────────────────

        [Fact]
        public void Zoom_ReachesTheMediaService()
        {
            _vm.Zoom = 2.0f;
            Assert.Equal(2.0f, _media.VideoScale);
        }

        [Fact]
        public void Zoom_ClampsAtBounds()
        {
            _vm.Zoom = 99f;
            Assert.Equal(4.0f, _vm.Zoom);

            _vm.Zoom = 0.01f;
            Assert.Equal(0.25f, _vm.Zoom);
        }

        // ── Equalizer wiring (11.3) ───────────────────────────────────────

        [Fact]
        public void Equalizer_Apply_SetsAllBandValues()
        {
            _vm.EqualizerEnabled = true;
            _vm.SelectedEqualizerPreset = "Rock";

            var pushed = _media.LastEqualizer;
            Assert.NotNull(pushed);
            Assert.Equal(EqualizerPresets.Get("Rock").Bands, pushed!.Bands);
        }

        [Fact]
        public void Equalizer_Disabled_PushesNullSoTheFilterDetaches()
        {
            _vm.EqualizerEnabled = true;
            _vm.SelectedEqualizerPreset = "Rock";
            _vm.EqualizerEnabled = false;

            Assert.Null(_media.LastEqualizer);
        }

        [Fact]
        public void EditingABand_SwitchesToCustomAndPersists()
        {
            _vm.EqualizerEnabled = true;
            _vm.SelectedEqualizerPreset = "Rock";

            _vm.EqualizerBands[0].Gain = -12f;

            Assert.Equal("Custom", _vm.SelectedEqualizerPreset);
            Assert.Equal(-12f, _settings.Current.EqualizerCustom.Bands[0]);
        }

        [Fact]
        public void SelectingAPreset_DoesNotCountAsAUserEdit()
        {
            _vm.EqualizerEnabled = true;
            _vm.SelectedEqualizerPreset = "Pop";

            // Syncing the sliders must not flip the selection straight back to "Custom".
            Assert.Equal("Pop", _vm.SelectedEqualizerPreset);
        }

        [Fact]
        public void ResetEqualizer_ReturnsToFlat()
        {
            _vm.EqualizerEnabled = true;
            _vm.SelectedEqualizerPreset = "Bass Boost";

            _vm.ResetEqualizer();

            Assert.Equal(EqualizerPresets.FlatName, _vm.SelectedEqualizerPreset);
            Assert.True(_vm.EqualizerBands.All(b => Math.Abs(b.Gain) < 0.01f));
        }

        [Fact]
        public void EqualizerBands_AreBuiltOnceWithTheRightFrequencies()
        {
            Assert.Equal(EqualizerProfile.BandCount, _vm.EqualizerBands.Count);
            Assert.Equal(
                EqualizerProfile.BandFrequencies,
                _vm.EqualizerBands.Select(b => b.FrequencyHz).ToArray());
        }

        // ── Chapters (9.4) ────────────────────────────────────────────────

        [Fact]
        public void RefreshChapters_PublishesWhatTheServiceReports()
        {
            _media.SeedChapters(
                new ChapterInfo { Index = 0, Title = "Intro", StartMs = 0 },
                new ChapterInfo { Index = 1, Title = "Act I", StartMs = 60_000 });

            _vm.RefreshChapters();

            Assert.Equal(2, _vm.Chapters.Count);
            Assert.True(_vm.HasChapters);
            Assert.Equal("Intro", _vm.Chapters[0].Title);
        }

        [Fact]
        public void HasChapters_IsFalseForMediaWithout()
        {
            _vm.RefreshChapters();
            Assert.False(_vm.HasChapters);
            Assert.Empty(_vm.Chapters);
        }

        [Fact]
        public void NextChapter_WrapsAround()
        {
            _media.SeedChapters(
                new ChapterInfo { Index = 0 }, new ChapterInfo { Index = 1 }, new ChapterInfo { Index = 2 });
            _vm.RefreshChapters();

            _media.CurrentChapter = 2;
            _vm.NextChapter();

            Assert.Equal(0, _media.CurrentChapter);
        }

        [Fact]
        public void PreviousChapter_WrapsBackwards()
        {
            _media.SeedChapters(new ChapterInfo { Index = 0 }, new ChapterInfo { Index = 1 });
            _vm.RefreshChapters();

            _media.CurrentChapter = 0;
            _vm.PreviousChapter();

            Assert.Equal(1, _media.CurrentChapter);
        }

        [Fact]
        public void ChapterNavigation_IsANoOpWithoutChapters()
        {
            _vm.RefreshChapters();

            _vm.NextChapter();
            _vm.PreviousChapter();

            Assert.Equal(0, _media.CurrentChapter);
        }

        [Fact]
        public void SelectedChapter_SeeksToThatChapter()
        {
            _media.SeedChapters(new ChapterInfo { Index = 0 }, new ChapterInfo { Index = 1 });
            _vm.RefreshChapters();

            _vm.SelectedChapter = _vm.Chapters[1];

            Assert.Equal(1, _media.CurrentChapter);
        }
    }
}
