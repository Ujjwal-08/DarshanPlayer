using DarshanPlayer.Models;
using Xunit;

namespace DarshanPlayer.Tests
{
    public class VideoAdjustmentTests
    {
        [Theory]
        [InlineData(5.0f, 2.0f)]   // above max
        [InlineData(-1.0f, 0.0f)]  // below min
        [InlineData(1.5f, 1.5f)]   // in range
        public void ClampBrightness_BoundsToRange(float input, float expected)
            => Assert.Equal(expected, VideoAdjustment.ClampBrightness(input));

        [Theory]
        [InlineData(5.0f, 3.0f)]
        [InlineData(-2.0f, 0.0f)]
        public void ClampSaturation_BoundsToRange(float input, float expected)
            => Assert.Equal(expected, VideoAdjustment.ClampSaturation(input));

        [Fact]
        public void ClampGamma_NeverGoesBelowLowerBound()
            => Assert.Equal(0.01f, VideoAdjustment.ClampGamma(0f));

        [Theory]
        [InlineData(200f, 180f)]
        [InlineData(-200f, -180f)]
        [InlineData(45f, 45f)]
        public void ClampHue_BoundsToRange(float input, float expected)
            => Assert.Equal(expected, VideoAdjustment.ClampHue(input));

        [Fact]
        public void IsNeutral_TrueForDefaults()
            => Assert.True(VideoAdjustment.IsNeutral(
                VideoAdjustment.DefaultBrightness,
                VideoAdjustment.DefaultContrast,
                VideoAdjustment.DefaultSaturation,
                VideoAdjustment.DefaultGamma,
                VideoAdjustment.DefaultHue));

        [Fact]
        public void IsNeutral_FalseWhenAnyValueChanged()
            => Assert.False(VideoAdjustment.IsNeutral(1.2f, 1f, 1f, 1f, 0f));
    }

    public class AppSettingsDefaultsTests
    {
        [Fact]
        public void Defaults_HaveNeutralVideoAdjustments()
        {
            var s = new AppSettings();
            Assert.Equal(1.0f, s.Brightness);
            Assert.Equal(1.0f, s.Contrast);
            Assert.Equal(1.0f, s.Saturation);
            Assert.Equal(1.0f, s.Gamma);
            Assert.Equal(0.0f, s.Hue);
        }

        [Fact]
        public void Defaults_HaveSaneSubtitleStyle()
        {
            var s = new AppSettings();
            Assert.Equal(0, s.SubtitleFontSize);          // 0 = auto
            Assert.Equal(0xFFFFFF, s.SubtitleColorRgb);   // white
            Assert.Equal("", s.SubtitleFontFamily);
        }
    }
}
