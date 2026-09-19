using System;
using System.Collections.Generic;
using DarshanPlayer.Models;
using DarshanPlayer.Services;
using LibVLCSharp.Shared;

namespace DarshanPlayer.Tests
{
    /// <summary>
    /// Recording stand-in for <see cref="IMediaService"/>. Lets the view-model be exercised without
    /// LibVLC, so tests can assert "the right value reached the player" — the shape of every
    /// MediaPlayer-facing test the checklist asks for.
    ///
    /// <see cref="LibVlc"/> and <see cref="MediaPlayer"/> return null: they are only reachable from
    /// code paths that genuinely need the native engine, and no test should be touching those.
    /// </summary>
    public class FakeMediaService : IMediaService
    {
        public LibVLC LibVlc => null!;
        public MediaPlayer MediaPlayer => null!;

        // ── Recorded calls ────────────────────────────────────────────────
        public List<string> PlayedFiles { get; } = new();
        public List<string?> AspectRatiosSet { get; } = new();
        public List<EqualizerProfile?> EqualizersSet { get; } = new();
        public List<string> LoadedSubtitles { get; } = new();
        public List<long> SeekTargets { get; } = new();
        public int PlayCount { get; private set; }
        public int PauseCount { get; private set; }
        public int StopCount { get; private set; }
        public int RefreshChaptersCount { get; private set; }
        public int ApplyVideoAdjustmentsCount { get; private set; }

        // ── State ─────────────────────────────────────────────────────────
        public bool IsPlaying { get; set; }
        public long Duration { get; set; }
        public long CurrentTime { get; set; }
        public float Position { get; set; }
        public int Volume { get; set; } = 80;
        public bool IsMuted { get; set; }
        public float Rate { get; set; } = 1.0f;

        public IReadOnlyList<MediaTrackInfo> AudioTracks { get; set; } = Array.Empty<MediaTrackInfo>();
        public IReadOnlyList<MediaTrackInfo> SubtitleTracks { get; set; } = Array.Empty<MediaTrackInfo>();
        public int CurrentAudioTrack { get; set; }
        public int CurrentSubtitleTrack { get; set; }

        public long SubtitleDelay { get; set; }
        public float VideoScale { get; set; }
        public long AudioDelay { get; set; }

        public float Brightness { get; set; } = 1f;
        public float Contrast { get; set; } = 1f;
        public float Saturation { get; set; } = 1f;
        public float Gamma { get; set; } = 1f;
        public float Hue { get; set; }

        public string? CropGeometry { get; set; }
        public string? DeinterlaceMode { get; set; }
        public bool AudioNormalization { get; set; }
        public AudioChannelMode AudioChannel { get; set; } = AudioChannelMode.Stereo;

        private List<ChapterInfo> _chapters = new();
        public IReadOnlyList<ChapterInfo> Chapters => _chapters;
        public int CurrentChapter { get; set; }

        /// <summary>Seed the chapter list a RefreshChapters() call will publish.</summary>
        public void SeedChapters(params ChapterInfo[] chapters) => _chapters = new List<ChapterInfo>(chapters);

        // ── Behaviour ─────────────────────────────────────────────────────
        public void Play() { PlayCount++; IsPlaying = true; }
        public void Pause() { PauseCount++; IsPlaying = false; }
        public void Stop() { StopCount++; IsPlaying = false; }
        public void PlayFile(string path) { PlayedFiles.Add(path); IsPlaying = true; }
        public void SeekTo(long timeMs) { SeekTargets.Add(timeMs); CurrentTime = timeMs; }
        public void SkipBy(long offsetMs) => SeekTo(CurrentTime + offsetMs);
        public void LoadExternalSubtitle(string path) => LoadedSubtitles.Add(path);
        public void TakeSnapshot(string outputPath) { }
        public void SetAspectRatio(string? ratio) => AspectRatiosSet.Add(ratio);
        public string GetMediaMetadata() => string.Empty;
        public void NextFrame() { }
        public void ApplyVideoAdjustments() => ApplyVideoAdjustmentsCount++;
        public void ReloadSubtitleSettings(AppSettings settings) { }
        public void RecreateWithSubtitleSettings(AppSettings settings) { }
        public void SetEqualizer(EqualizerProfile? profile) => EqualizersSet.Add(profile);
        public void RefreshChapters() => RefreshChaptersCount++;

        /// <summary>Most recent aspect ratio pushed, or null if none.</summary>
        public string? LastAspectRatio =>
            AspectRatiosSet.Count > 0 ? AspectRatiosSet[^1] : null;

        /// <summary>Most recent equalizer profile pushed, or null if none.</summary>
        public EqualizerProfile? LastEqualizer =>
            EqualizersSet.Count > 0 ? EqualizersSet[^1] : null;

        // ── Events (never raised unless a test asks) ──────────────────────
        public event EventHandler<TimeChangedEventArgs>? TimeChanged;
        public event EventHandler<MediaPlayerPositionChangedEventArgs>? PositionChanged;
        public event EventHandler? Playing;
        public event EventHandler? Paused;
        public event EventHandler? Stopped;
        public event EventHandler? EndReached;
        public event EventHandler? EncounteredError;
        public event EventHandler<MediaPlayerLengthChangedEventArgs>? LengthChanged;
        public event EventHandler? MediaPlayerRecreated;
        public event EventHandler? PlaybackResumedAfterRecreation;

        /// <summary>
        /// Keeps the compiler quiet about events no test raises, without suppressing the warning
        /// globally — and documents that raising them needs a WPF dispatcher (the view-model
        /// marshals every handler through App.Current).
        /// </summary>
        public void AssertEventsAreDeclared()
        {
            _ = TimeChanged; _ = PositionChanged; _ = Playing; _ = Paused; _ = Stopped;
            _ = EndReached; _ = EncounteredError; _ = LengthChanged;
            _ = MediaPlayerRecreated; _ = PlaybackResumedAfterRecreation;
        }

        public void Dispose() { }
    }
}
