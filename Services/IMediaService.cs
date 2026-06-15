using DarshanPlayer.Models;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;

namespace DarshanPlayer.Services
{
    public interface IMediaService : IDisposable
    {
        LibVLC LibVlc { get; }
        MediaPlayer MediaPlayer { get; }

        // State
        bool IsPlaying { get; }
        long Duration { get; }
        long CurrentTime { get; }
        float Position { get; set; }
        int Volume { get; set; }
        bool IsMuted { get; set; }
        float Rate { get; set; }

        // Tracks
        IReadOnlyList<MediaTrackInfo> AudioTracks { get; }
        IReadOnlyList<MediaTrackInfo> SubtitleTracks { get; }
        int CurrentAudioTrack { get; set; }
        int CurrentSubtitleTrack { get; set; }

        // Playback
        void Play();
        void Pause();
        void Stop();
        void PlayFile(string path);
        void SeekTo(long timeMs);
        void SkipBy(long offsetMs);
        void LoadExternalSubtitle(string path);
        void TakeSnapshot(string outputPath);
        void SetAspectRatio(string? ratio);
        string GetMediaMetadata();
        void NextFrame();
        long SubtitleDelay { get; set; } // in ms
        float VideoScale { get; set; }   // 0 = auto-fit; 1.0 = 100%; clamped 0.25–4.0 in VM
        long AudioDelay { get; set; }    // in ms

        // Video adjustments (Phase 12.1) — applied via the LibVLC "adjust" filter.
        float Brightness { get; set; }   // 0.0 – 2.0
        float Contrast { get; set; }     // 0.0 – 2.0
        float Saturation { get; set; }   // 0.0 – 3.0
        float Gamma { get; set; }        // 0.01 – 10.0
        float Hue { get; set; }          // -180 – 180 (degrees)
        /// <summary>Re-push all current adjustment values to the player (call after media (re)loads).</summary>
        void ApplyVideoAdjustments();
        /// <summary>Reload current file with updated subtitle style options, then seek back to saved position.</summary>
        void ReloadSubtitleSettings(AppSettings settings);
        /// <summary>Recreate LibVLC instance with new freetype args so subtitle style takes effect immediately.</summary>
        void RecreateWithSubtitleSettings(AppSettings settings);
        /// <summary>Fired after MediaPlayer is recreated — listener must rebind VideoView.MediaPlayer.</summary>
        event EventHandler? MediaPlayerRecreated;
        /// <summary>Fired after seek-back completes following recreation — listener hides frame overlay.</summary>
        event EventHandler? PlaybackResumedAfterRecreation;

        // Events
        event EventHandler<TimeChangedEventArgs> TimeChanged;
        event EventHandler<MediaPlayerPositionChangedEventArgs> PositionChanged;
        event EventHandler Playing;
        event EventHandler Paused;
        event EventHandler Stopped;
        event EventHandler EndReached;
        event EventHandler EncounteredError;
        event EventHandler<MediaPlayerLengthChangedEventArgs> LengthChanged;
    }

    public class TimeChangedEventArgs : EventArgs
    {
        public long Time { get; }
        public TimeChangedEventArgs(long time) => Time = time;
    }
}
