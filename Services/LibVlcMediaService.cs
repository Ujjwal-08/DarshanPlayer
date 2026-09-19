using DarshanPlayer.Models;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DarshanPlayer.Services
{
    public class LibVlcMediaService : IMediaService
    {
        public LibVLC LibVlc { get; private set; }
        public MediaPlayer MediaPlayer { get; private set; }
        public event EventHandler? MediaPlayerRecreated;
        public event EventHandler? PlaybackResumedAfterRecreation;
        private volatile bool _isRecreating = false;

        // Cached video-adjustment values (source of truth lives here so getters never depend on a
        // particular LibVLCSharp getter signature). Defaults are the neutral/no-op values.
        private float _brightness = VideoAdjustment.DefaultBrightness;
        private float _contrast = VideoAdjustment.DefaultContrast;
        private float _saturation = VideoAdjustment.DefaultSaturation;
        private float _gamma = VideoAdjustment.DefaultGamma;
        private float _hue = VideoAdjustment.DefaultHue;

        public bool IsPlaying => MediaPlayer.IsPlaying;
        public long Duration => MediaPlayer.Length;
        public long CurrentTime => MediaPlayer.Time;
        public float Position
        {
            get => MediaPlayer.Position;
            set => MediaPlayer.Position = value;
        }
        public int Volume
        {
            get => MediaPlayer.Volume;
            set => MediaPlayer.Volume = Math.Clamp(value, 0, 200);
        }
        public bool IsMuted
        {
            get => MediaPlayer.Mute;
            set => MediaPlayer.Mute = value;
        }
        public float Rate
        {
            get => MediaPlayer.Rate;
            set => MediaPlayer.SetRate(value);
        }

        public IReadOnlyList<MediaTrackInfo> AudioTracks
        {
            get
            {
                var desc = MediaPlayer.AudioTrackDescription;
                if (desc == null) return new List<MediaTrackInfo>();
                return desc.Select(t => new MediaTrackInfo(t.Id, t.Name)).ToList();
            }
        }

        public IReadOnlyList<MediaTrackInfo> SubtitleTracks
        {
            get
            {
                var desc = MediaPlayer.SpuDescription;
                if (desc == null) return new List<MediaTrackInfo>();
                return desc.Select(t => new MediaTrackInfo(t.Id, t.Name)).ToList();
            }
        }

        public int CurrentAudioTrack
        {
            get => MediaPlayer.AudioTrack;
            set => MediaPlayer.SetAudioTrack(value);
        }
        public int CurrentSubtitleTrack
        {
            get => MediaPlayer.Spu;
            set => MediaPlayer.SetSpu(value);
        }

        // Events
        public event EventHandler<TimeChangedEventArgs>? TimeChanged;
        public event EventHandler<MediaPlayerPositionChangedEventArgs>? PositionChanged;
        public event EventHandler? Playing;
        public event EventHandler? Paused;
        public event EventHandler? Stopped;
        public event EventHandler? EndReached;
        public event EventHandler? EncounteredError;
        public event EventHandler<MediaPlayerLengthChangedEventArgs>? LengthChanged;

        public LibVlcMediaService(AppSettings? settings = null)
        {
            Core.Initialize();
            LibVlc = new LibVLC(enableDebugLogs: false, BuildLibVlcOptions(settings));
            MediaPlayer = new MediaPlayer(LibVlc);
            SubscribeMediaPlayerEvents();
        }

        private void SubscribeMediaPlayerEvents()
        {
            MediaPlayer.TimeChanged += (_, e) => TimeChanged?.Invoke(this, new TimeChangedEventArgs(e.Time));
            MediaPlayer.PositionChanged += (_, e) => PositionChanged?.Invoke(this, e);
            MediaPlayer.Playing += (_, _) =>
            {
                ApplyVideoAdjustments();
                Playing?.Invoke(this, EventArgs.Empty);
            };
            MediaPlayer.Paused += (_, _) => Paused?.Invoke(this, EventArgs.Empty);
            MediaPlayer.Stopped += (_, _) => Stopped?.Invoke(this, EventArgs.Empty);
            MediaPlayer.EndReached += (_, _) =>
            {
                Log.Debug_("Playback", "End reached: {File}", _currentFilePath);
                EndReached?.Invoke(this, EventArgs.Empty);
            };
            MediaPlayer.EncounteredError += (_, _) =>
            {
                Log.Error("Playback", null, "LibVLC reported an error on {File}", _currentFilePath);
                EncounteredError?.Invoke(this, EventArgs.Empty);
            };
            MediaPlayer.LengthChanged += (_, e) => LengthChanged?.Invoke(this, e);
        }

        public void RecreateWithSubtitleSettings(AppSettings settings)
        {
            if (_isRecreating || string.IsNullOrWhiteSpace(_currentFilePath)) return;
            _isRecreating = true;
            var time = MediaPlayer.Time;
            var filePath = _currentFilePath;
            var subPath = _currentExternalSubtitlePath;

            // Stop and dispose old instances
            MediaPlayer.Stop();
            var oldPlayer = MediaPlayer;
            var oldVlc = LibVlc;

            // Create new LibVLC with updated freetype args
            LibVlc = new LibVLC(enableDebugLogs: false, BuildLibVlcOptions(settings));
            MediaPlayer = new MediaPlayer(LibVlc);
            SubscribeMediaPlayerEvents();

            // Notify MainWindow to rebind VideoView.MediaPlayer
            MediaPlayerRecreated?.Invoke(this, EventArgs.Empty);

            // Replay from saved position
            _currentFilePath = filePath;
            _currentExternalSubtitlePath = subPath;

            EventHandler<EventArgs>? handler = null;
            handler = (_, _) =>
            {
                MediaPlayer.Playing -= handler;
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    // Minimal delay — just enough for decoder to accept seek
                    await System.Threading.Tasks.Task.Delay(80);
                    MediaPlayer.Time = time;
                    if (!string.IsNullOrWhiteSpace(subPath))
                    {
                        await System.Threading.Tasks.Task.Delay(120);
                        MediaPlayer.AddSlave(MediaSlaveType.Subtitle,
                            new Uri(subPath).AbsoluteUri, true);
                    }
                    _isRecreating = false;
                    // Signal overlay to hide
                    PlaybackResumedAfterRecreation?.Invoke(this, EventArgs.Empty);
                });
            };
            MediaPlayer.Playing += handler;

            var media = new Media(LibVlc, filePath, FromType.FromPath);
            if (settings.EnableHardwareAcceleration)
            {
                media.AddOption(":avcodec-hw=any");
                media.AddOption(":d3d11va-hw-decoding");
            }
            MediaPlayer.Play(media);
            media.Dispose();

            // Dispose old after new is playing; also reset guard in case Playing never fired
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(1000);
                _isRecreating = false; // fallback reset if Playing never fired
                oldPlayer.Dispose();
                oldVlc.Dispose();
            });
        }

        public void Play() => MediaPlayer.Play();
        public void Pause() => MediaPlayer.Pause();
        public void Stop() => MediaPlayer.Stop();

        private string _currentFilePath = "";

        public void PlayFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            _currentFilePath = path;
            _currentExternalSubtitlePath = ""; // reset on new file
            var media = new Media(LibVlc, path, FromType.FromPath);

            var settings = ServiceLocator.SettingsService?.Current;
            if (settings != null && settings.EnableHardwareAcceleration)
            {
                media.AddOption(":avcodec-hw=any");
                media.AddOption(":d3d11va-hw-decoding");
            }
            ApplyPerMediaOptions(media);

            Log.Info("Playback", "Opening {File} (hwaccel={Hw}, crop={Crop}, deinterlace={Deint}, normalize={Norm})",
                Path.GetFileName(path), settings?.EnableHardwareAcceleration ?? false,
                _cropGeometry ?? "off", _deinterlaceMode ?? "off", _audioNormalization);

            // Subtitle appearance (Phase 10.3). These freetype-* options are applied per-media, so
            // changing them takes effect on the next opened file. Invalid options are ignored by VLC.
            if (settings != null)
                ApplySubtitleStyleOptions(media, settings);

            MediaPlayer.Play(media);
            media.Dispose();
        }

        public void ReloadSubtitleSettings(AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(_currentFilePath)) return;
            var time = MediaPlayer.Time;
            if (time <= 0) return;

            var externalSub = _currentExternalSubtitlePath;

            // One-shot handler: seek back then re-add external subtitle via AddSlave.
            // Must not block LibVLC's event thread — use Task.Run.
            EventHandler<EventArgs>? handler = null;
            handler = (_, _) =>
            {
                MediaPlayer.Playing -= handler;
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(300);
                    MediaPlayer.Time = time;
                    if (!string.IsNullOrWhiteSpace(externalSub))
                    {
                        await System.Threading.Tasks.Task.Delay(150);
                        MediaPlayer.AddSlave(MediaSlaveType.Subtitle,
                            new Uri(externalSub).AbsoluteUri, true);
                    }
                });
            };
            MediaPlayer.Playing += handler;

            var media = new Media(LibVlc, _currentFilePath, FromType.FromPath);
            var hwSettings = ServiceLocator.SettingsService?.Current;
            if (hwSettings != null && hwSettings.EnableHardwareAcceleration)
            {
                media.AddOption(":avcodec-hw=any");
                media.AddOption(":d3d11va-hw-decoding");
            }
            MediaPlayer.Play(media);
            media.Dispose();
        }

        // Build freetype args for LibVLC initialization — the only reliable way to set
        // subtitle style in LibVLC (per-media AddOption does not reach the freetype renderer).
        private static string[] BuildLibVlcOptions(AppSettings? s)
        {
            if (s == null) return Array.Empty<string>();
            var args = new List<string>();
            if (s.SubtitleFontSize > 0)
                args.Add($"--freetype-fontsize={s.SubtitleFontSize}");
            if (!string.IsNullOrWhiteSpace(s.SubtitleFontFamily))
                args.Add($"--freetype-font={s.SubtitleFontFamily}");
            args.Add($"--freetype-color={s.SubtitleColorRgb}");
            args.Add($"--freetype-outline-thickness={s.SubtitleOutlineThickness}");
            args.Add($"--freetype-background-opacity={s.SubtitleBackgroundOpacity}");
            return args.ToArray();
        }

        // Keep for future use (per-media options do NOT reach freetype — use BuildLibVlcOptions instead)
        private static void ApplySubtitleStyleOptions(Media media, AppSettings s) { }

        public void SeekTo(long timeMs)
        {
            if (MediaPlayer.IsSeekable)
                MediaPlayer.Time = timeMs;
        }

        public void SkipBy(long offsetMs)
        {
            var length = MediaPlayer.Length;
            if (length <= 0) return; // Cannot skip if no duration
            var newTime = Math.Clamp(MediaPlayer.Time + offsetMs, 0, length);
            SeekTo(newTime);
        }

        private string _currentExternalSubtitlePath = "";

        private static readonly HashSet<string> SubtitleExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".srt", ".ass", ".ssa", ".vtt", ".sub", ".idx", ".usf", ".smi", ".jss", ".mpl" };

        public void LoadExternalSubtitle(string path)
        {
            var ext = System.IO.Path.GetExtension(path);
            if (!SubtitleExtensions.Contains(ext))
                throw new InvalidOperationException($"'{ext}' is not a supported subtitle format. Use .srt, .ass, .vtt, .sub, etc.");
            _currentExternalSubtitlePath = path;
            MediaPlayer.AddSlave(MediaSlaveType.Subtitle, new Uri(path).AbsoluteUri, true);
        }

        public void TakeSnapshot(string outputPath)
        {
            MediaPlayer.TakeSnapshot(0, outputPath, 0, 0);
        }

        public void SetAspectRatio(string? ratio)
        {
            MediaPlayer.AspectRatio = ratio;
        }

        public void NextFrame()
        {
            MediaPlayer.NextFrame();
        }

        public long SubtitleDelay
        {
            get => MediaPlayer.SpuDelay / 1000; // micro to milli
            set => MediaPlayer.SetSpuDelay(value * 1000); // milli to micro
        }

        public float VideoScale
        {
            get => MediaPlayer.Scale;
            set => MediaPlayer.Scale = value;
        }

        public long AudioDelay
        {
            get => MediaPlayer.AudioDelay / 1000; // µs → ms
            set => MediaPlayer.SetAudioDelay(value * 1000); // ms → µs
        }

        // ─── Video geometry (Phases 12.3, 12.5, 12.6) ────────────────────
        // Values are cached locally and re-pushed from ApplyVideoAdjustments() so they survive the
        // MediaPlayer being recreated (RecreateWithSubtitleSettings) or new media being loaded.

        private string? _cropGeometry;
        public string? CropGeometry
        {
            get => _cropGeometry;
            set
            {
                _cropGeometry = string.IsNullOrWhiteSpace(value) ? null : value;
                TryApply("CropGeometry", () => MediaPlayer.CropGeometry = _cropGeometry);
            }
        }

        private string? _deinterlaceMode;
        public string? DeinterlaceMode
        {
            get => _deinterlaceMode;
            set
            {
                _deinterlaceMode = string.IsNullOrWhiteSpace(value) || value == "Off" ? null : value;
                // Passing an empty mode is how LibVLC is told to detach the filter.
                TryApply("Deinterlace", () =>
                    MediaPlayer.SetDeinterlace(_deinterlaceMode?.ToLowerInvariant() ?? string.Empty));
            }
        }

        // ─── Audio (Phases 11.3, 11.4, 11.5) ─────────────────────────────

        private Equalizer? _equalizer;
        public void SetEqualizer(EqualizerProfile? profile)
        {
            TryApply("Equalizer", () =>
            {
                if (profile == null || profile.IsFlat)
                {
                    MediaPlayer.UnsetEqualizer();
                    _equalizer?.Dispose();
                    _equalizer = null;
                    return;
                }

                var normalized = profile.Normalized();
                var eq = new Equalizer();
                eq.SetPreamp(normalized.PreAmp);
                for (uint i = 0; i < EqualizerProfile.BandCount; i++)
                    eq.SetAmp(normalized.Bands[i], i);

                MediaPlayer.SetEqualizer(eq);

                // Dispose only after the new one is attached — freeing the live filter first can
                // leave the audio chain pointing at released native memory.
                _equalizer?.Dispose();
                _equalizer = eq;
            });
        }

        private bool _audioNormalization;
        public bool AudioNormalization
        {
            get => _audioNormalization;
            set
            {
                if (_audioNormalization == value) return;
                _audioNormalization = value;
                // The compressor filter is an instance-level option, so it only takes effect for
                // media opened afterwards. Surfaced in the UI as applying to the next file.
                Debug.WriteLine($"[MediaService] AudioNormalization={value} (applies to next media)");
            }
        }

        private AudioChannelMode _audioChannel = AudioChannelMode.Stereo;
        public AudioChannelMode AudioChannel
        {
            get => _audioChannel;
            set
            {
                _audioChannel = value;
                TryApply("AudioChannel", () => MediaPlayer.SetChannel((AudioOutputChannel)(int)value));
            }
        }

        // ─── Chapters (Phase 9.4) ────────────────────────────────────────

        private List<ChapterInfo> _chapters = new();
        public IReadOnlyList<ChapterInfo> Chapters => _chapters;

        public int CurrentChapter
        {
            get => MediaPlayer.Chapter;
            set
            {
                if (_chapters.Count == 0) return;
                MediaPlayer.Chapter = Math.Clamp(value, 0, _chapters.Count - 1);
            }
        }

        public void RefreshChapters()
        {
            var list = new List<ChapterInfo>();
            try
            {
                // Chapter 0 of title -1 = the chapters of the current title.
                var descriptions = MediaPlayer.FullChapterDescriptions(-1);
                if (descriptions != null)
                {
                    for (int i = 0; i < descriptions.Length; i++)
                    {
                        var d = descriptions[i];
                        list.Add(new ChapterInfo
                        {
                            Index = i,
                            Title = d.Name ?? string.Empty,
                            StartMs = d.TimeOffset,
                            DurationMs = d.Duration,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Most files have no chapters at all; that is not an error worth surfacing.
                Debug.WriteLine($"[MediaService] Chapter enumeration failed: {ex.Message}");
            }

            _chapters = list;
        }

        /// <summary>
        /// Options that must be attached per-media rather than set at runtime.
        /// </summary>
        private void ApplyPerMediaOptions(Media media)
        {
            if (_audioNormalization)
                media.AddOption(":audio-filter=compressor");
        }

        /// <summary>
        /// Run a native call that is only valid once a media player has output attached. These
        /// throw or crash if invoked too early, and every one of them is a cosmetic setting —
        /// never worth taking the app down for.
        /// </summary>
        private void TryApply(string what, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediaService] {what} failed: {ex.Message}");
                Log.Warn("MediaService", "{Setting} could not be applied: {Reason}", what, ex.Message);
            }
        }

        // ─── Video Adjustments (Phase 12.1) ──────────────────────────────
        public float Brightness
        {
            get => _brightness;
            set { _brightness = VideoAdjustment.ClampBrightness(value); ApplyVideoAdjustments(); }
        }
        public float Contrast
        {
            get => _contrast;
            set { _contrast = VideoAdjustment.ClampContrast(value); ApplyVideoAdjustments(); }
        }
        public float Saturation
        {
            get => _saturation;
            set { _saturation = VideoAdjustment.ClampSaturation(value); ApplyVideoAdjustments(); }
        }
        public float Gamma
        {
            get => _gamma;
            set { _gamma = VideoAdjustment.ClampGamma(value); ApplyVideoAdjustments(); }
        }
        public float Hue
        {
            get => _hue;
            set { _hue = VideoAdjustment.ClampHue(value); ApplyVideoAdjustments(); }
        }

        public void ApplyVideoAdjustments()
        {
            try
            {
                // The adjust filter must be enabled before any value takes effect.
                MediaPlayer.SetAdjustInt(VideoAdjustOption.Enable, 1);
                MediaPlayer.SetAdjustFloat(VideoAdjustOption.Brightness, _brightness);
                MediaPlayer.SetAdjustFloat(VideoAdjustOption.Contrast, _contrast);
                MediaPlayer.SetAdjustFloat(VideoAdjustOption.Saturation, _saturation);
                MediaPlayer.SetAdjustFloat(VideoAdjustOption.Gamma, _gamma);
                MediaPlayer.SetAdjustFloat(VideoAdjustOption.Hue, _hue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LibVlcMediaService] ApplyVideoAdjustments failed: {ex.Message}");
            }
        }

        public string GetMediaMetadata()
        {
            var media = MediaPlayer.Media;
            if (media == null || media.Tracks == null) return "No media info available.";

            var lines = new List<string> { $"Filename: {Uri.UnescapeDataString(media.Mrl.Split('/').Last())}" };
            var tracks = media.Tracks;
            
            var videoInfo = string.Empty;
            var audioInfo = string.Empty;

            foreach (var track in tracks)
            {
                if (track.TrackType == TrackType.Video)
                {
                    double fps = 0;
                    if (track.Data.Video.FrameRateDen > 0)
                        fps = (double)track.Data.Video.FrameRateNum / track.Data.Video.FrameRateDen;
                    
                    var codecInt = track.OriginalFourcc;
                    string codec = DecodeFourCc(codecInt == 0 ? track.Codec : codecInt);

                    videoInfo = $"[Video] {track.Data.Video.Width}x{track.Data.Video.Height} | Codec: {codec} | FPS: {fps:0.##}";
                }
                else if (track.TrackType == TrackType.Audio)
                {
                    var codecInt = track.OriginalFourcc;
                    string codec = DecodeFourCc(codecInt == 0 ? track.Codec : codecInt);
                    audioInfo = $"[Audio] {track.Data.Audio.Channels} ch | {track.Data.Audio.Rate} Hz | Codec: {codec}";
                }
            }

            if (!string.IsNullOrEmpty(videoInfo)) lines.Add(videoInfo);
            if (!string.IsNullOrEmpty(audioInfo)) lines.Add(audioInfo);

            return string.Join(Environment.NewLine, lines);
        }

        // A15: BitConverter produces garbage chars for non-ASCII bytes; keep only printable ASCII
        private static string DecodeFourCc(uint code) =>
            new string(BitConverter.GetBytes(code)
                .Select(b => b is >= 32 and <= 126 ? (char)b : '?')
                .ToArray()).TrimEnd('?', '\0');

        public void Dispose()
        {
            MediaPlayer.Stop();
            MediaPlayer.Dispose();
            LibVlc.Dispose();
        }
    }
}
