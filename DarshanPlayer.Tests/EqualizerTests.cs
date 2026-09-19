using System;
using System.Linq;
using DarshanPlayer.Models;
using DarshanPlayer.Services;
using DarshanPlayer.ViewModels;
using Xunit;

namespace DarshanPlayer.Tests
{
    public class EqualizerProfileTests
    {
        [Fact]
        public void NewProfile_IsFlat()
        {
            Assert.True(new EqualizerProfile().IsFlat);
        }

        [Fact]
        public void ProfileWithAnyBandSet_IsNotFlat()
        {
            var p = new EqualizerProfile();
            p.Bands[4] = 3f;
            Assert.False(p.IsFlat);
        }

        [Fact]
        public void ProfileWithPreampSet_IsNotFlat()
        {
            Assert.False(new EqualizerProfile { PreAmp = 2f }.IsFlat);
        }

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(25f, 20f)]     // above LibVLC's accepted range
        [InlineData(-25f, -20f)]   // below it
        [InlineData(12.5f, 12.5f)]
        public void ClampGain_KeepsValuesInLibVlcRange(float input, float expected)
        {
            Assert.Equal(expected, EqualizerProfile.ClampGain(input));
        }

        [Fact]
        public void Normalized_ClampsEveryBandAndPreamp()
        {
            var p = new EqualizerProfile
            {
                PreAmp = 99f,
                Bands = new[] { 50f, -50f, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f },
            };

            var n = p.Normalized();
            Assert.Equal(20f, n.PreAmp);
            Assert.Equal(20f, n.Bands[0]);
            Assert.Equal(-20f, n.Bands[1]);
        }

        [Fact]
        public void Normalized_PadsShortBandArrayFromMalformedSettings()
        {
            // settings.json edited by hand, or written by an older build.
            var p = new EqualizerProfile { Bands = new[] { 3f, 4f } };
            var n = p.Normalized();

            Assert.Equal(EqualizerProfile.BandCount, n.Bands.Length);
            Assert.Equal(3f, n.Bands[0]);
            Assert.Equal(0f, n.Bands[9]);
        }

        [Fact]
        public void Normalized_SurvivesNullBands()
        {
            var n = new EqualizerProfile { Bands = null! }.Normalized();
            Assert.Equal(EqualizerProfile.BandCount, n.Bands.Length);
            Assert.True(n.IsFlat);
        }

        [Fact]
        public void Clone_DoesNotShareBandArray()
        {
            var p = EqualizerPresets.Get("Rock");
            var c = p.Clone();
            c.Bands[0] = -15f;

            Assert.NotEqual(c.Bands[0], p.Bands[0]);
        }

        [Fact]
        public void BandFrequencies_MatchBandCount()
        {
            Assert.Equal(EqualizerProfile.BandCount, EqualizerProfile.BandFrequencies.Length);
        }
    }

    public class EqualizerPresetsTests
    {
        [Fact]
        public void FlatIsFirstPreset()
        {
            Assert.Equal(EqualizerPresets.FlatName, EqualizerPresets.Names[0]);
        }

        [Fact]
        public void EveryPresetHasTenBandsInRange()
        {
            foreach (var name in EqualizerPresets.Names)
            {
                var p = EqualizerPresets.Get(name);
                Assert.Equal(EqualizerProfile.BandCount, p.Bands.Length);
                Assert.All(p.Bands, b =>
                    Assert.InRange(b, EqualizerProfile.MinGainDb, EqualizerProfile.MaxGainDb));
            }
        }

        [Fact]
        public void FlatPresetIsActuallyFlat()
        {
            Assert.True(EqualizerPresets.Flat().IsFlat);
        }

        [Fact]
        public void NonFlatPresetsAreNotFlat()
        {
            foreach (var name in EqualizerPresets.Names.Where(n => n != EqualizerPresets.FlatName))
                Assert.False(EqualizerPresets.Get(name).IsFlat, $"{name} should shape the sound");
        }

        [Fact]
        public void BassBoostLiftsLowsAndLeavesHighsAlone()
        {
            var p = EqualizerPresets.Get("Bass Boost");
            Assert.True(p.Bands[0] > 0, "60Hz should be lifted");
            Assert.Equal(0f, p.Bands[9]);
        }

        [Fact]
        public void Get_IsCaseInsensitive()
        {
            Assert.Equal("Rock", EqualizerPresets.Get("rock").Name);
        }

        [Fact]
        public void Get_ThrowsOnUnknownPreset()
        {
            Assert.Throws<ArgumentException>(() => EqualizerPresets.Get("Dubstep"));
        }

        [Fact]
        public void Exists_ReportsKnownAndUnknown()
        {
            Assert.True(EqualizerPresets.Exists("Pop"));
            Assert.False(EqualizerPresets.Exists("Nope"));
            Assert.False(EqualizerPresets.Exists(""));
            Assert.False(EqualizerPresets.Exists(null!));
        }
    }

    public class EqualizerBandVMTests
    {
        [Fact]
        public void FrequencyLabel_AbbreviatesKilohertz()
        {
            Assert.Equal("60", new EqualizerBandVM(0, 60, 0).FrequencyLabel);
            Assert.Equal("3k", new EqualizerBandVM(5, 3000, 0).FrequencyLabel);
            Assert.Equal("16k", new EqualizerBandVM(9, 16000, 0).FrequencyLabel);
        }

        [Fact]
        public void SettingGain_RaisesValueChanged()
        {
            var vm = new EqualizerBandVM(0, 60, 0);
            var fired = 0;
            vm.ValueChanged += (_, _) => fired++;

            vm.Gain = 5f;

            Assert.Equal(1, fired);
            Assert.Equal(5f, vm.Gain);
        }

        [Fact]
        public void SettingSameGain_DoesNotRaiseValueChanged()
        {
            var vm = new EqualizerBandVM(0, 60, 5f);
            var fired = 0;
            vm.ValueChanged += (_, _) => fired++;

            vm.Gain = 5f;

            Assert.Equal(0, fired);
        }

        [Fact]
        public void SetWithoutNotify_DoesNotReportAUserEdit()
        {
            // Selecting a preset must not flip the selection straight back to "Custom".
            var vm = new EqualizerBandVM(0, 60, 0);
            var fired = 0;
            vm.ValueChanged += (_, _) => fired++;

            vm.SetWithoutNotify(8f);

            Assert.Equal(0, fired);
            Assert.Equal(8f, vm.Gain);
        }

        [Fact]
        public void Gain_IsClampedOnConstructionAndAssignment()
        {
            Assert.Equal(20f, new EqualizerBandVM(0, 60, 99f).Gain);

            var vm = new EqualizerBandVM(0, 60, 0);
            vm.Gain = -99f;
            Assert.Equal(-20f, vm.Gain);
        }

        [Theory]
        [InlineData(0f, "0 dB")]
        [InlineData(3f, "+3 dB")]
        [InlineData(-4.5f, "-4.5 dB")]
        public void GainLabel_ShowsSign(float gain, string expected)
        {
            Assert.Equal(expected, new EqualizerBandVM(0, 60, gain).GainLabel);
        }
    }

    public class ChapterInfoTests
    {
        [Fact]
        public void DisplayLabel_UsesOneBasedNumberingAndOffset()
        {
            var c = new ChapterInfo { Index = 0, Title = "Opening", StartMs = 0 };
            Assert.Equal("1. Opening (0:00)", c.DisplayLabel);
        }

        [Fact]
        public void DisplayLabel_FallsBackWhenChapterIsUntitled()
        {
            var c = new ChapterInfo { Index = 2, Title = "", StartMs = 65_000 };
            Assert.Equal("3. Chapter 3 (1:05)", c.DisplayLabel);
        }

        [Fact]
        public void DisplayLabel_SwitchesToHoursForLongOffsets()
        {
            var c = new ChapterInfo { Index = 0, Title = "Act II", StartMs = 3_725_000 };
            Assert.Equal("1. Act II (1:02:05)", c.DisplayLabel);
        }

        [Fact]
        public void FormatOffset_TreatsNegativeAsZero()
        {
            Assert.Equal("0:00", ChapterInfo.FormatOffset(-5));
        }
    }
}
