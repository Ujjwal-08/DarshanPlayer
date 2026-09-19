using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DarshanPlayer.Models;

namespace DarshanPlayer.ViewModels
{
    /// <summary>
    /// One equalizer slider. Exists so the panel can bind a column per band without the parent
    /// view-model needing ten near-identical properties.
    /// </summary>
    public class EqualizerBandVM : INotifyPropertyChanged
    {
        public EqualizerBandVM(int index, int frequencyHz, float gain)
        {
            Index = index;
            FrequencyHz = frequencyHz;
            _gain = EqualizerProfile.ClampGain(gain);
        }

        public int Index { get; }
        public int FrequencyHz { get; }

        /// <summary>Axis label: "60" below 1kHz, "3k" above.</summary>
        public string FrequencyLabel => FrequencyHz >= 1000
            ? $"{FrequencyHz / 1000}k"
            : FrequencyHz.ToString();

        public static float MinGain => EqualizerProfile.MinGainDb;
        public static float MaxGain => EqualizerProfile.MaxGainDb;

        private float _gain;
        public float Gain
        {
            get => _gain;
            set
            {
                var clamped = EqualizerProfile.ClampGain(value);
                if (Math.Abs(clamped - _gain) < 0.001f) return;

                _gain = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GainLabel));
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string GainLabel => $"{_gain:+0.#;-0.#;0} dB";

        /// <summary>Raised only on user edits, so the parent can mark the profile as customised.</summary>
        public event EventHandler? ValueChanged;

        /// <summary>
        /// Update from a preset without reporting it as a user edit — otherwise selecting
        /// "Rock" would immediately flip the preset back to "Custom".
        /// </summary>
        public void SetWithoutNotify(float gain)
        {
            _gain = EqualizerProfile.ClampGain(gain);
            OnPropertyChanged(nameof(Gain));
            OnPropertyChanged(nameof(GainLabel));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
