using System;
using System.Collections.Generic;
using System.Linq;
using DarshanPlayer.Models;

namespace DarshanPlayer.Services
{
    /// <summary>
    /// The built-in equalizer curves. Kept as pure data with no LibVLC dependency so the shapes
    /// can be unit-tested and reused anywhere.
    /// </summary>
    public static class EqualizerPresets
    {
        public const string FlatName = "Flat";

        // Band order matches EqualizerProfile.BandFrequencies:
        // 60, 170, 310, 600, 1k, 3k, 6k, 12k, 14k, 16k
        private static readonly Dictionary<string, float[]> Curves = new(StringComparer.OrdinalIgnoreCase)
        {
            [FlatName]      = new[] {  0f,   0f,   0f,   0f,  0f,  0f,  0f,  0f,  0f,  0f },
            ["Bass Boost"]  = new[] {  8f,   6f,   4f,   2f,  0f,  0f,  0f,  0f,  0f,  0f },
            ["Vocal Boost"] = new[] { -2f,  -1f,   0f,   3f,  5f,  5f,  3f,  1f,  0f,  0f },
            ["Classical"]   = new[] {  4f,   3f,   2f,   0f,  0f,  0f, -1f, -2f, -3f, -3f },
            ["Dance"]       = new[] {  7f,   5f,   2f,   0f,  1f,  3f,  4f,  4f,  3f,  2f },
            ["Pop"]         = new[] { -1f,   1f,   3f,   4f,  4f,  2f,  0f, -1f, -1f, -1f },
            ["Rock"]        = new[] {  6f,   4f,  -1f,  -2f, -1f,  2f,  4f,  5f,  5f,  5f },
            ["Podcast"]     = new[] { -6f,  -4f,  -1f,   3f,  5f,  5f,  4f,  1f, -1f, -3f },
        };

        /// <summary>Preset names in display order, "Flat" first.</summary>
        public static IReadOnlyList<string> Names { get; } = Curves.Keys.ToList();

        public static bool Exists(string name) =>
            !string.IsNullOrWhiteSpace(name) && Curves.ContainsKey(name);

        /// <summary>
        /// Build a profile for a named preset. Throws for unknown names so a typo surfaces
        /// immediately rather than silently producing a flat curve.
        /// </summary>
        public static EqualizerProfile Get(string name)
        {
            if (!Curves.TryGetValue(name, out var bands))
                throw new ArgumentException($"Unknown equalizer preset '{name}'.", nameof(name));

            return new EqualizerProfile
            {
                Name = Names.First(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)),
                PreAmp = 0f,
                Bands = (float[])bands.Clone(),
            };
        }

        public static EqualizerProfile Flat() => Get(FlatName);
    }
}
