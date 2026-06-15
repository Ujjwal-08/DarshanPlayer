using System;

namespace DarshanPlayer.Models
{
    /// <summary>
    /// Pure clamping/range helpers for the LibVLC "adjust" video filter (Phase 12.1).
    /// Kept free of any WPF/LibVLC dependency so it can be unit-tested directly and reused
    /// by both the ViewModel setters and the media service.
    /// </summary>
    public static class VideoAdjustment
    {
        // Neutral (no-op) values — what "Reset All" restores.
        public const float DefaultBrightness = 1.0f;
        public const float DefaultContrast = 1.0f;
        public const float DefaultSaturation = 1.0f;
        public const float DefaultGamma = 1.0f;
        public const float DefaultHue = 0.0f;

        public static float ClampBrightness(float v) => Math.Clamp(v, 0.0f, 2.0f);
        public static float ClampContrast(float v) => Math.Clamp(v, 0.0f, 2.0f);
        public static float ClampSaturation(float v) => Math.Clamp(v, 0.0f, 3.0f);
        public static float ClampGamma(float v) => Math.Clamp(v, 0.01f, 10.0f);
        public static float ClampHue(float v) => Math.Clamp(v, -180.0f, 180.0f);

        /// <summary>True when every value equals its neutral default (filter has no visible effect).</summary>
        public static bool IsNeutral(float brightness, float contrast, float saturation, float gamma, float hue)
            => brightness == DefaultBrightness
               && contrast == DefaultContrast
               && saturation == DefaultSaturation
               && gamma == DefaultGamma
               && hue == DefaultHue;
    }
}
