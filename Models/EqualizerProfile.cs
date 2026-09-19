using System;
using System.Linq;

namespace DarshanPlayer.Models
{
    /// <summary>
    /// A 10-band equalizer setting. Band centres match LibVLC's fixed set
    /// (60Hz, 170, 310, 600, 1k, 3k, 6k, 12k, 14k, 16k), so the index of
    /// <see cref="Bands"/> maps straight onto LibVLC's band index.
    /// </summary>
    public class EqualizerProfile
    {
        public const int BandCount = 10;

        /// <summary>Gain limits LibVLC accepts, in dB.</summary>
        public const float MinGainDb = -20f;
        public const float MaxGainDb = 20f;

        /// <summary>Band centre frequencies, for axis labels in the UI.</summary>
        public static readonly int[] BandFrequencies =
            { 60, 170, 310, 600, 1000, 3000, 6000, 12000, 14000, 16000 };

        public string Name { get; set; } = "Custom";

        /// <summary>Overall gain applied before the bands, in dB.</summary>
        public float PreAmp { get; set; }

        /// <summary>Per-band gain in dB, always <see cref="BandCount"/> entries.</summary>
        public float[] Bands { get; set; } = new float[BandCount];

        /// <summary>True when every band and the preamp sit at 0 — i.e. the EQ does nothing.</summary>
        public bool IsFlat =>
            Math.Abs(PreAmp) < 0.01f && Bands.All(b => Math.Abs(b) < 0.01f);

        public static float ClampGain(float db) => Math.Clamp(db, MinGainDb, MaxGainDb);

        /// <summary>
        /// Returns a copy with exactly <see cref="BandCount"/> bands, every value clamped into
        /// LibVLC's accepted range. Guards against malformed values loaded from settings.json,
        /// which would otherwise be rejected by the native call.
        /// </summary>
        public EqualizerProfile Normalized()
        {
            var bands = new float[BandCount];
            if (Bands != null)
            {
                for (int i = 0; i < BandCount && i < Bands.Length; i++)
                    bands[i] = ClampGain(Bands[i]);
            }

            return new EqualizerProfile
            {
                Name = string.IsNullOrWhiteSpace(Name) ? "Custom" : Name,
                PreAmp = ClampGain(PreAmp),
                Bands = bands,
            };
        }

        public EqualizerProfile Clone() => new()
        {
            Name = Name,
            PreAmp = PreAmp,
            Bands = (float[])Bands.Clone(),
        };
    }
}
