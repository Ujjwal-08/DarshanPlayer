using DarshanPlayer.Models;
using DarshanPlayer.Services;
using LibVLCSharp.Shared;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;

namespace DarshanPlayer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IMediaService _media;
        private readonly PlaylistService _playlist;
        private readonly SettingsService _settings;
        private readonly LanguageManager _lang;
        private readonly INotificationService _notifications;
        private readonly DispatcherTimer _uiTimer;

        // ─── Playback State ────────────────────────────────
        private bool _isPlaying;
        private long _currentMs;  // current playback time in ms
        public long CurrentMs => _currentMs;
        private double _position; // 0.0 – 1.0 fraction for seek slider
        private long _duration;   // ms
        private int _volume = 80;
        private bool _isMuted;
        private float _rate = 1.0f;
        private string _currentTitle = "No media loaded";
        private string _currentTimeStr = "0:00";
        private string _durationStr = "0:00";

        // ─── UI State ──────────────────────────────────────
        private bool _isPlaylistVisible;
        private bool _isFullscreen;
        private bool _isPipMode;
        private bool _isMediaLoaded;
        private bool _alwaysOnTop;
        private bool _isControlsVisible = true;
        private bool _isSleepTimerActive;
        private string _sleepTimerLabel = "";
        private LanguageOption _selectedLanguage;
        private ObservableCollection<MediaTrackInfo> _audioTracks = new();
        private ObservableCollection<MediaTrackInfo> _subtitleTracks = new();
        private MediaTrackInfo? _selectedAudioTrack;
        private MediaTrackInfo? _selectedSubtitleTrack;
        private string _selectedAspectRatio = "Default";
        private DispatcherTimer? _sleepTimer;
        private DateTime _sleepTimerEnd;
        private bool _isResumePromptVisible;
        private string _resumePromptMessage = string.Empty;
        private bool _isMetadataVisible;
        private string _mediaMetadataText = string.Empty;
        private long _abPointA = -1;
        private long _abPointB = -1;
        private bool _isAbRepeatActive;
        private readonly Stopwatch _seekStopwatch = Stopwatch.StartNew();

        // Named delegates so they can be unsubscribed in Dispose (A4)
        private EventHandler _onPlaying = null!;
        private EventHandler _onPaused = null!;
        private EventHandler _onStopped = null!;
        private EventHandler _onEndReached = null!;
        private EventHandler _onEncounteredError = null!;
        private EventHandler<MediaPlayerLengthChangedEventArgs> _onLengthChanged = null!;
        private EventHandler<TimeChangedEventArgs> _onTimeChanged = null!;

        // Prevents the async Stopped event from resetting state mid-file-switch (fix 8)
        private bool _isChangingMedia;

        // ─── Adjustment / subtitle-style panels ────────────
        private bool _isVideoAdjustVisible;
        private bool _isSubtitleStyleVisible;
        private string _selectedSubtitleFont = "Default";

        public static readonly IReadOnlyList<string> AspectRatios =
            new[] { "Default", "16:9", "4:3", "1:1", "21:9", "Fill" };

        public static readonly IReadOnlyList<float> PlaybackRates =
            new[] { 0.25f, 0.5f, 0.75f, 1.0f, 1.25f, 1.5f, 1.75f, 2.0f };

        // Speed ComboBox indices match this array order.
        private static readonly float[] SpeedValues = { 0.25f, 0.5f, 0.75f, 1.0f, 1.25f, 1.5f, 2.0f, 4.0f };

        // Subtitle fonts. "Default" = let VLC choose. The rest are Windows fonts with good
        // Indian-script coverage (Nirmala UI / Noto / Mangal) plus common Latin fallbacks.
        public static readonly IReadOnlyList<string> SubtitleFonts =
            new[] { "Default", "Nirmala UI", "Noto Sans", "Mangal", "Segoe UI", "Arial", "Verdana" };

        // Preset subtitle colours exposed as swatches in the UI (label, 24-bit RGB).
        public static readonly IReadOnlyList<SubtitleColorOption> SubtitleColors = new[]
        {
            new SubtitleColorOption("White",  0xFFFFFF),
            new SubtitleColorOption("Yellow", 0xFFFF00),
            new SubtitleColorOption("Cyan",   0x00FFFF),
            new SubtitleColorOption("Green",  0x00FF00),
            new SubtitleColorOption("Orange", 0xFF8000),
            new SubtitleColorOption("Red",    0xFF3030),
        };

        public IReadOnlyList<LanguageOption> Languages => LanguageManager.SupportedLanguages;

        // Playlist ViewModel (exposed for XAML binding)
        public PlaylistViewModel? PlaylistVM { get; set; }

        // ─── Properties ────────────────────────────────────
        public bool IsPlaying { get => _isPlaying; private set { _isPlaying = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayPauseIcon)); OnPropertyChanged(nameof(TaskbarProgressState)); OnPropertyChanged(nameof(IsNotPlaying)); } }
        public bool IsNotPlaying => !_isPlaying;
        /// <summary>Seek position as 0.0–1.0 fraction (bound to Slider Maximum="1").</summary>
        public double Position
        {
            get => _position;
            set
            {
                if (Math.Abs(_position - value) < 0.0001) return;
                _position = value;
                OnPropertyChanged();
                
                if (_isDraggingSeek)
                {
                    // Throttled seek during drag — Stopwatch avoids sensitivity to clock adjustments
                    if (_seekStopwatch.ElapsedMilliseconds > 150)
                    {
                        if (_duration > 0)
                            _media.SeekTo((long)(value * _duration));
                        _seekStopwatch.Restart();
                    }
                }
                else
                {
                    if (_duration > 0)
                        _media.SeekTo((long)(value * _duration));
                }
            }
        }
        // Flag set by MainWindow while user is dragging the seek slider
        public bool IsDraggingSeek
        {
            get => _isDraggingSeek;
            set => _isDraggingSeek = value;
        }
        private bool _isDraggingSeek;
        public long Duration { get => _duration; private set { _duration = value; OnPropertyChanged(); OnPropertyChanged(nameof(ABPointAFraction)); OnPropertyChanged(nameof(ABPointBFraction)); } }
        public int Volume
        {
            get => _volume;
            set
            {
                // Clamp to 0-100 to match the UI slider. LibVLC supports 0-200 (>100 = amplification)
                // but amplification is not exposed in the UI to avoid accidental audio distortion.
                value = Math.Clamp(value, 0, 100);
                if (_volume == value) return;
                _volume = value; OnPropertyChanged();
                _media.Volume = value;
                _settings.Current.Volume = value;
                _settings.SaveDebounced();
            }
        }
        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value; OnPropertyChanged(); OnPropertyChanged(nameof(MuteIcon));
                _media.IsMuted = value;
            }
        }
        public float Rate
        {
            get => _rate;
            set
            {
                // Clamp to a sane LibVLC range so a bad XAML CommandParameter cannot break playback.
                value = Math.Clamp(value, 0.25f, 4.0f);
                if (Math.Abs(_rate - value) < 0.0001f) return;
                _rate = value; OnPropertyChanged();
                _media.Rate = value;
                _settings.Current.PlaybackRate = value;
                _settings.SaveDebounced();
                // Notify all IsRate* properties so menu checkmarks update
                OnPropertyChanged(nameof(IsRate025));
                OnPropertyChanged(nameof(IsRate05));
                OnPropertyChanged(nameof(IsRate075));
                OnPropertyChanged(nameof(IsRate10));
                OnPropertyChanged(nameof(IsRate125));
                OnPropertyChanged(nameof(IsRate15));
                OnPropertyChanged(nameof(IsRate175));
                OnPropertyChanged(nameof(IsRate20));
                OnPropertyChanged(nameof(SpeedIndex));
            }
        }
        public int SpeedIndex
        {
            get => Array.FindIndex(SpeedValues, r => Math.Abs(r - _rate) < 0.01f);
            set { if (value >= 0 && value < SpeedValues.Length) Rate = SpeedValues[value]; }
        }
        // Playback rate checkmark helpers (Dummy setters added for WPF binding support)
        public bool IsRate025 { get => Math.Abs(_rate - 0.25f) < 0.01f; set { if (value) Rate = 0.25f; } }
        public bool IsRate05  { get => Math.Abs(_rate  - 0.5f)  < 0.01f; set { if (value) Rate = 0.5f; } }
        public bool IsRate075 { get => Math.Abs(_rate - 0.75f) < 0.01f; set { if (value) Rate = 0.75f; } }
        public bool IsRate10  { get => Math.Abs(_rate - 1.0f)  < 0.01f; set { if (value) Rate = 1.0f; } }
        public bool IsRate125 { get => Math.Abs(_rate - 1.25f) < 0.01f; set { if (value) Rate = 1.25f; } }
        public bool IsRate15  { get => Math.Abs(_rate - 1.5f)  < 0.01f; set { if (value) Rate = 1.5f; } }
        public bool IsRate175 { get => Math.Abs(_rate - 1.75f) < 0.01f; set { if (value) Rate = 1.75f; } }
        public bool IsRate20  { get => Math.Abs(_rate - 2.0f)  < 0.01f; set { if (value) Rate = 2.0f; } }
        // ─── Zoom (Phase 12.4) ────────────────────────────────────────────
        private float _zoom = 1.0f;
        public float Zoom
        {
            get => _zoom;
            set
            {
                // 0 = auto-fit (pass directly to LibVLC); otherwise clamp 0.25–4.0
                _zoom = value <= 0f ? 0f : Math.Clamp(value, 0.25f, 4.0f);
                _media.VideoScale = _zoom;
                _settings.Current.VideoScale = _zoom;
                _settings.SaveDebounced();
                OnPropertyChanged();
            }
        }

        public ICommand RestartAndUpdateCommand { get; }
        public ICommand ReloadWithSubtitleSettingsCommand { get; private set; } = null!;
        public event EventHandler? SubtitleReloadRequested;
        public void TakeCurrentSnapshot(string path) => _media.TakeSnapshot(path);

        // Auto-fit video scale for PiP/compact mode (Scale=0 = LibVLC fits to window)
        public void EnterPipScale() => _media.VideoScale = 0f;
        public void ExitPipScale()  => _media.VideoScale = _zoom;

        // ─── Audio Delay (Phase 11.2) ─────────────────────────────────────
        private long _audioDelay;
        public long AudioDelay
        {
            get => _audioDelay;
            set { _audioDelay = Math.Clamp(value, -2000, 2000); _media.AudioDelay = _audioDelay; OnPropertyChanged(); }
        }

        public string CurrentTitle { get => _currentTitle; private set { _currentTitle = value; OnPropertyChanged(); } }
        public string CurrentTimeStr { get => _currentTimeStr; private set { _currentTimeStr = value; OnPropertyChanged(); } }
        public string DurationStr { get => _durationStr; private set { _durationStr = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalTimeStr)); } }
        /// <summary>Alias for DurationStr used in XAML bindings.</summary>
        public string TotalTimeStr => _durationStr;
        public bool IsPlaylistVisible { get => _isPlaylistVisible; set { _isPlaylistVisible = value; OnPropertyChanged(); } }
        public bool IsFullscreen { get => _isFullscreen; set { _isFullscreen = value; OnPropertyChanged(); } }
        public bool IsPipMode { get => _isPipMode; set { _isPipMode = value; OnPropertyChanged(); } }
        public bool IsMediaLoaded { get => _isMediaLoaded; private set { _isMediaLoaded = value; OnPropertyChanged(); OnPropertyChanged(nameof(TaskbarProgressState)); } }
        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set
            {
                if (_alwaysOnTop == value) return;
                _alwaysOnTop = value;
                OnPropertyChanged();
                _settings.Current.AlwaysOnTop = value;
                _settings.SaveDebounced();
            }
        }
        public bool StartMaximized
        {
            get => _settings.Current.StartMaximized;
            set { _settings.Current.StartMaximized = value; _settings.SaveDebounced(); OnPropertyChanged(); }
        }

        public bool IsControlsVisible { get => _isControlsVisible; set { _isControlsVisible = value; OnPropertyChanged(); } }
        public MahApps.Metro.IconPacks.PackIconMaterialKind PlayPauseIcon => IsPlaying ? MahApps.Metro.IconPacks.PackIconMaterialKind.Pause : MahApps.Metro.IconPacks.PackIconMaterialKind.Play;
        public MahApps.Metro.IconPacks.PackIconMaterialKind MuteIcon 
        {
            get 
            {
                if (IsMuted || Volume == 0) return MahApps.Metro.IconPacks.PackIconMaterialKind.VolumeOff;
                if (Volume < 33) return MahApps.Metro.IconPacks.PackIconMaterialKind.VolumeLow;
                if (Volume < 66) return MahApps.Metro.IconPacks.PackIconMaterialKind.VolumeMedium;
                return MahApps.Metro.IconPacks.PackIconMaterialKind.VolumeHigh;
            }
        }
        public bool IsSleepTimerActive { get => _isSleepTimerActive; private set { _isSleepTimerActive = value; OnPropertyChanged(); } }
        public string SleepTimerLabel { get => _sleepTimerLabel; private set { _sleepTimerLabel = value; OnPropertyChanged(); } }

        public bool IsResumePromptVisible { get => _isResumePromptVisible; set { _isResumePromptVisible = value; OnPropertyChanged(); } }
        public string ResumePromptMessage { get => _resumePromptMessage; set { _resumePromptMessage = value; OnPropertyChanged(); } }

        public bool IsMetadataVisible { get => _isMetadataVisible; set { _isMetadataVisible = value; OnPropertyChanged(); } }
        public string MediaMetadataText { get => _mediaMetadataText; set { _mediaMetadataText = value; OnPropertyChanged(); } }

        public long ABPointA { get => _abPointA; set { _abPointA = value; OnPropertyChanged(); OnPropertyChanged(nameof(ABRepeatStatus)); OnPropertyChanged(nameof(ABPointAFraction)); } }
        public long ABPointB { get => _abPointB; set { _abPointB = value; OnPropertyChanged(); OnPropertyChanged(nameof(ABRepeatStatus)); OnPropertyChanged(nameof(ABPointBFraction)); } }
        public bool IsABRepeatActive { get => _isAbRepeatActive; set { _isAbRepeatActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(ABRepeatStatus)); } }
        public string ABRepeatStatus => !IsABRepeatActive ? "A-B Repeat" : (ABPointB == -1 ? "A-..." : "A-B Loop");

        public double ABPointAFraction =>
            (Duration > 0 && ABPointA >= 0) ? Math.Clamp((double)ABPointA / Duration, 0, 1) : -1;
        public double ABPointBFraction =>
            (Duration > 0 && ABPointB >= 0) ? Math.Clamp((double)ABPointB / Duration, 0, 1) : -1;

        public System.Windows.Shell.TaskbarItemProgressState TaskbarProgressState =>
            !IsMediaLoaded ? System.Windows.Shell.TaskbarItemProgressState.None :
            IsPlaying       ? System.Windows.Shell.TaskbarItemProgressState.Normal :
                              System.Windows.Shell.TaskbarItemProgressState.Paused;

        public long SubtitleDelay
        {
            get => _media.SubtitleDelay;
            set { _media.SubtitleDelay = value; OnPropertyChanged(); OnPropertyChanged(nameof(SubtitleDelayStr)); }
        }
        public string SubtitleDelayStr => $"Subtitle Delay: {SubtitleDelay}ms";

        public bool EnableHardwareAcceleration
        {
            get => _settings.Current.EnableHardwareAcceleration;
            set 
            { 
                _settings.Current.EnableHardwareAcceleration = value; 
                _settings.Save(); 
                OnPropertyChanged();
                OnPropertyChanged(nameof(HardwareAccelerationStatus));
            }
        }

        public string HardwareAccelerationStatus => EnableHardwareAcceleration ? "On" : "Off";

        // ─── Video Adjustments (Phase 12.1) ────────────────
        public bool IsVideoAdjustVisible { get => _isVideoAdjustVisible; set { _isVideoAdjustVisible = value; OnPropertyChanged(); } }

        public float Brightness
        {
            get => _media.Brightness;
            set
            {
                var v = VideoAdjustment.ClampBrightness((float)value);
                if (_media.Brightness == v) return;
                _media.Brightness = v;
                _settings.Current.Brightness = v;
                _settings.SaveDebounced();
                OnPropertyChanged();
                OnPropertyChanged(nameof(BrightnessLabel));
            }
        }
        public float Contrast
        {
            get => _media.Contrast;
            set
            {
                var v = VideoAdjustment.ClampContrast((float)value);
                if (_media.Contrast == v) return;
                _media.Contrast = v;
                _settings.Current.Contrast = v;
                _settings.SaveDebounced();
                OnPropertyChanged();
                OnPropertyChanged(nameof(ContrastLabel));
            }
        }
        public float Saturation
        {
            get => _media.Saturation;
            set
            {
                var v = VideoAdjustment.ClampSaturation((float)value);
                if (_media.Saturation == v) return;
                _media.Saturation = v;
                _settings.Current.Saturation = v;
                _settings.SaveDebounced();
                OnPropertyChanged();
                OnPropertyChanged(nameof(SaturationLabel));
            }
        }
        public float Gamma
        {
            get => _media.Gamma;
            set
            {
                var v = VideoAdjustment.ClampGamma((float)value);
                if (_media.Gamma == v) return;
                _media.Gamma = v;
                _settings.Current.Gamma = v;
                _settings.SaveDebounced();
                OnPropertyChanged();
                OnPropertyChanged(nameof(GammaLabel));
            }
        }
        public float Hue
        {
            get => _media.Hue;
            set
            {
                var v = VideoAdjustment.ClampHue((float)value);
                if (_media.Hue == v) return;
                _media.Hue = v;
                _settings.Current.Hue = v;
                _settings.SaveDebounced();
                OnPropertyChanged();
                OnPropertyChanged(nameof(HueLabel));
            }
        }
        public string BrightnessLabel => Brightness.ToString("0.00");
        public string ContrastLabel => Contrast.ToString("0.00");
        public string SaturationLabel => Saturation.ToString("0.00");
        public string GammaLabel => Gamma.ToString("0.00");
        public string HueLabel => $"{Hue:0}°";

        // ─── Subtitle Appearance (Phase 10.3) ──────────────
        public bool IsSubtitleStyleVisible { get => _isSubtitleStyleVisible; set { _isSubtitleStyleVisible = value; OnPropertyChanged(); } }
        public IReadOnlyList<string> SubtitleFontList => SubtitleFonts;
        public IReadOnlyList<SubtitleColorOption> SubtitleColorOptions => SubtitleColors;

        public int SubtitleFontSize
        {
            get => _settings.Current.SubtitleFontSize;
            set
            {
                var v = value <= 0 ? 0 : Math.Clamp(value, 14, 48);
                if (_settings.Current.SubtitleFontSize == v) return;
                _settings.Current.SubtitleFontSize = v;
                _settings.SaveDebounced();
                OnPropertyChanged();
                OnPropertyChanged(nameof(SubtitleFontSizeLabel));
            }
        }
        public string SubtitleFontSizeLabel => SubtitleFontSize <= 0 ? "Auto" : SubtitleFontSize.ToString();

        public string SelectedSubtitleFont
        {
            get => _selectedSubtitleFont;
            set
            {
                _selectedSubtitleFont = string.IsNullOrWhiteSpace(value) ? "Default" : value;
                // "Default" maps to an empty family so VLC uses its own default.
                _settings.Current.SubtitleFontFamily = _selectedSubtitleFont == "Default" ? "" : _selectedSubtitleFont;
                _settings.SaveDebounced();
                OnPropertyChanged();
            }
        }

        public int SubtitleOutlineThickness
        {
            get => _settings.Current.SubtitleOutlineThickness;
            set
            {
                var v = Math.Clamp(value, 0, 10);
                if (_settings.Current.SubtitleOutlineThickness == v) return;
                _settings.Current.SubtitleOutlineThickness = v;
                _settings.SaveDebounced();
                OnPropertyChanged();
            }
        }

        public int SubtitleBackgroundOpacity
        {
            get => _settings.Current.SubtitleBackgroundOpacity;
            set
            {
                var v = Math.Clamp(value, 0, 255);
                if (_settings.Current.SubtitleBackgroundOpacity == v) return;
                _settings.Current.SubtitleBackgroundOpacity = v;
                _settings.SaveDebounced();
                OnPropertyChanged();
            }
        }

        /// <summary>Live preview swatch for the currently selected subtitle colour.</summary>
        public System.Windows.Media.Brush SubtitleColorPreview
        {
            get
            {
                int rgb = _settings.Current.SubtitleColorRgb;
                return new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF)));
            }
        }

        // ─── Active Track IDs (for menu checkmarks) ────────
        public int CurrentAudioTrackId => _media.CurrentAudioTrack;
        public int CurrentSubtitleTrackId => _media.CurrentSubtitleTrack;

        // ─── Controls-bar track dropdowns (Phase 10.2 / 11.1) ──────────────
        public bool HasMultipleAudioTracks => AudioTracks.Count > 1;
        public IReadOnlyList<MediaTrackInfo> SubtitleTracksWithOff =>
            new[] { new MediaTrackInfo(-1, "Off") }.Concat(SubtitleTracks).ToList();

        public int CurrentAudioTrack
        {
            get => _media.CurrentAudioTrack;
            set { _media.CurrentAudioTrack = value; RefreshTracks(); OnPropertyChanged(); }
        }
        public int CurrentSubtitleTrack
        {
            get => _media.CurrentSubtitleTrack;
            set { _media.CurrentSubtitleTrack = value; RefreshTracks(); OnPropertyChanged(); }
        }

        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value; OnPropertyChanged();
                _lang.Apply(value.Code);
                _settings.Current.Language = value.Code;
                _settings.Save();
            }
        }
        public ObservableCollection<MediaTrackInfo> AudioTracks { get => _audioTracks; private set { _audioTracks = value; OnPropertyChanged(); } }
        public ObservableCollection<MediaTrackInfo> SubtitleTracks { get => _subtitleTracks; private set { _subtitleTracks = value; OnPropertyChanged(); } }
        public MediaTrackInfo? SelectedAudioTrack
        {
            get => _selectedAudioTrack;
            set 
            { 
                _selectedAudioTrack = value; 
                OnPropertyChanged(); 
                if (value != null) 
                {
                    _media.CurrentAudioTrack = value.Id;
                    RefreshTracks(); // Immediate UI update for checkmarks
                }
            }
        }
        public MediaTrackInfo? SelectedSubtitleTrack
        {
            get => _selectedSubtitleTrack;
            set 
            { 
                _selectedSubtitleTrack = value; 
                OnPropertyChanged(); 
                if (value != null) 
                {
                    _media.CurrentSubtitleTrack = value.Id;
                    RefreshTracks(); // Immediate UI update for checkmarks
                }
            }
        }
        public string SelectedAspectRatio
        {
            get => _selectedAspectRatio;
            set
            {
                _selectedAspectRatio = value; OnPropertyChanged();
                _media.SetAspectRatio(value == "Default" || value == "Fill" ? null : value);
            }
        }

        public PlaylistService Playlist => _playlist;
        public ObservableCollection<string> RecentFiles { get; } = new();
        public ObservableCollection<string> RecentFolders { get; } = new();

        // ─── Commands ──────────────────────────────────────
        public ICommand PlayPauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand OpenSubtitleCommand { get; }
        public ICommand SkipForwardCommand { get; }
        public ICommand SkipBackCommand { get; }
        public ICommand NextTrackCommand { get; }
        public ICommand PrevTrackCommand { get; }
        public ICommand ToggleMuteCommand { get; }
        public ICommand TogglePlaylistCommand { get; }
        public ICommand ToggleFullscreenCommand { get; }
        public ICommand ToggleAlwaysOnTopCommand { get; }
        public ICommand TakeScreenshotCommand { get; }
        public ICommand SetSleepTimerCommand { get; }
        public ICommand CancelSleepTimerCommand { get; }
        public ICommand PlayRecentCommand { get; }
        public ICommand OpenRecentFolderCommand { get; }

        public ICommand ResumeSessionCommand { get; }
        public ICommand DismissResumePromptCommand { get; }
        public ICommand ToggleMetadataCommand { get; }
        public ICommand NextFrameCommand { get; }
        public ICommand FrameBackCommand { get; }
        public ICommand AdjustSubtitleDelayCommand { get; }
        public ICommand AdjustAudioDelayCommand { get; }
        public ICommand SavePlaylistCommand { get; }
        public ICommand LoadPlaylistCommand { get; }
        public ICommand SetABPointCommand { get; }
        public ICommand SetAPointCommand { get; }
        public ICommand SetBPointCommand { get; }
        public ICommand ClearABRepeatCommand { get; }
        public ICommand RateCommand { get; }
        public ICommand ChangeLanguageCommand { get; }
        public ICommand SelectAudioTrackCommand { get; }
        public ICommand SelectSubtitleTrackCommand { get; }

        // Video adjustments + subtitle styling (Phases 12.1 / 10.3)
        public ICommand ToggleVideoAdjustCommand { get; }
        public ICommand ResetVideoAdjustmentsCommand { get; }
        public ICommand ToggleSubtitleStyleCommand { get; }
        public ICommand SetSubtitleColorCommand { get; }

        /// <summary>Toast notifications surface (Phase 23). Bind an ItemsControl to this.</summary>
        public ObservableCollection<ToastItem> Toasts => _notifications.Toasts;

        /// <summary>True while any toast is visible — drives the toast Popup's IsOpen so it only
        /// floats above the video (escaping WPF/HWND airspace) when there is something to show.</summary>
        public bool HasToasts => _notifications.Toasts.Count > 0;

        public MainViewModel(IMediaService media, PlaylistService playlist, SettingsService settings,
                             LanguageManager lang, INotificationService notifications)
        {
            _media = media;
            _playlist = playlist;
            _settings = settings;
            _lang = lang;
            _notifications = notifications;

            // Load persisted settings
            _volume = settings.Current.Volume;
            _rate = settings.Current.PlaybackRate;
            _alwaysOnTop = settings.Current.AlwaysOnTop;
            _media.Volume = _volume;
            _media.Rate = _rate;
            _playlist.RepeatMode = settings.Current.RepeatMode;
            _playlist.IsShuffle = settings.Current.IsShuffle;
            // 1.0 was the old default (100% native = crops portrait videos) — treat as auto-fit
            _zoom = settings.Current.VideoScale == 1.0f ? 0f : settings.Current.VideoScale;
            _media.VideoScale = _zoom;

            // Restore persisted video adjustments (re-applied to the player on the Playing event).
            _media.Brightness = settings.Current.Brightness;
            _media.Contrast = settings.Current.Contrast;
            _media.Saturation = settings.Current.Saturation;
            _media.Gamma = settings.Current.Gamma;
            _media.Hue = settings.Current.Hue;
            _selectedSubtitleFont = string.IsNullOrWhiteSpace(settings.Current.SubtitleFontFamily)
                ? "Default" : settings.Current.SubtitleFontFamily;

            // Keep the toast Popup's open-state in sync with the live toast collection.
            _notifications.Toasts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasToasts));

            _selectedLanguage = Languages.FirstOrDefault(l => l.Code == settings.Current.Language)
                ?? Languages[0];

            foreach (var f in settings.Current.RecentFiles)
                RecentFiles.Add(f);
            foreach (var d in settings.Current.RecentFolders)
                RecentFolders.Add(d);

            _selectedAudioTrack = default!;
            _selectedSubtitleTrack = default!;

            // Wire media events — stored as named fields so Dispose() can unsubscribe (A4)
            _onPlaying = (_, _) => App.Current.Dispatcher.Invoke(() => { IsPlaying = true; IsMediaLoaded = true; RefreshTracks(); OnPropertyChanged(nameof(CurrentAudioTrackId)); OnPropertyChanged(nameof(CurrentSubtitleTrackId)); });
            _onPaused = (_, _) => App.Current.Dispatcher.Invoke(() => IsPlaying = false);
            _onStopped = (_, _) =>
            {
                // Ignore the Stopped event fired by Stop() during a file switch — the new file is already loading (fix 8)
                if (_isChangingMedia) return;
                App.Current.Dispatcher.Invoke(() =>
                {
                IsPlaying = false;
                IsMediaLoaded = false;
                _currentMs = 0;
                _position = 0;
                Duration = 0;
                CurrentTitle = "No media loaded";
                CurrentTimeStr = "0:00";
                DurationStr = "0:00";
                // Reassign on UI thread (inside Dispatcher.Invoke above) — required for ObservableCollection (A5)
                AudioTracks = new ObservableCollection<MediaTrackInfo>();
                SubtitleTracks = new ObservableCollection<MediaTrackInfo>();
                OnPropertyChanged(nameof(Position));
                }); // end Dispatcher.Invoke
            }; // end _onStopped
            _onEncounteredError = (_, _) => App.Current.Dispatcher.Invoke(() =>
            {
                IsPlaying = false;
                IsMediaLoaded = false;
                _notifications.ShowError("Playback failed — file may be unsupported or corrupt.");
            });
            _onEndReached = (_, _) => App.Current.Dispatcher.Invoke(() => {
                _settings.Current.LastPlayedFile = string.Empty;
                _settings.Current.LastPlaybackPosition = 0;
                _settings.Save();
                if (_playlist.HasNext()) _playlist.PlayNext();
                else IsMediaLoaded = false;
            });
            _onLengthChanged = (_, e) => App.Current.Dispatcher.Invoke(() =>
            {
                Duration = e.Length;
                DurationStr = FormatTime(e.Length);
                // Reset position fraction when new media loads
                _currentMs = 0;
                _position = 0;
                OnPropertyChanged(nameof(Position));
            });

            int _timeTickCount = 0;
            _onTimeChanged = (_, e) => App.Current.Dispatcher.Invoke(() =>
            {
                _currentMs = e.Time;
                // Only update position if user is not dragging the seek slider
                if (!_isDraggingSeek)
                {
                    _position = _duration > 0 ? (double)_currentMs / _duration : 0;
                    OnPropertyChanged(nameof(Position));
                }
                CurrentTimeStr = FormatTime(e.Time);

                // Save session state periodically. SaveDebounced coalesces rapid ticks into one disk
                // write — without this, fast seeking could fire 60+ writes/sec and corrupt the file.
                _timeTickCount++;
                if (_timeTickCount % 10 == 0 && _media.IsPlaying && !string.IsNullOrEmpty(_settings.Current.LastPlayedFile))
                {
                    _settings.Current.LastPlaybackPosition = e.Time;
                    var path = _settings.Current.LastPlayedFile;
                    _settings.Current.WatchHistory[path] = e.Time;
                    // LRU eviction: keep newest 100 entries by removing one arbitrary key
                    if (_settings.Current.WatchHistory.Count > 100)
                    {
                        var oldest = _settings.Current.WatchHistory.Keys.First();
                        _settings.Current.WatchHistory.Remove(oldest);
                    }
                    _settings.SaveDebounced();
                }

                // Handle A-B Repeat Loop
                if (IsABRepeatActive && ABPointB > 0 && e.Time >= ABPointB)
                {
                    _media.SeekTo(ABPointA);
                }
            });

            _media.Playing += _onPlaying;
            _media.Paused += _onPaused;
            _media.Stopped += _onStopped;
            _media.EndReached += _onEndReached;
            _media.EncounteredError += _onEncounteredError;
            _media.LengthChanged += _onLengthChanged;
            _media.TimeChanged += _onTimeChanged;

            // UI timer for sleep timer countdown
            _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _uiTimer.Tick += UiTimer_Tick;
            _uiTimer.Start();

            // Commands
            RestartAndUpdateCommand = new RelayCommand(() =>
            {
                var updater = ServiceLocator.UpdateService;
                if (updater?.HasPendingUpdate == true)
                    updater.ApplyOnRestart();
                else
                    _notifications.ShowInfo("No update downloaded yet.");
            });
            PlayPauseCommand = new RelayCommand(() =>
            { 
                if (_media.IsPlaying) 
                {
                    _media.Pause(); 
                }
                else 
                {
                    // If we reached the end or it's stopped, try to restart or play next
                    if (_media.MediaPlayer.State == VLCState.Ended || _media.MediaPlayer.State == VLCState.Stopped)
                    {
                        if (_playlist.Current != null)
                        {
                            _media.PlayFile(_playlist.Current.FilePath);
                        }
                    }
                    else if (_playlist.Current != null || _media.MediaPlayer.Media != null) 
                    {
                        _media.Play(); 
                    }
                }
            });
            StopCommand = new RelayCommand(_media.Stop);
            SkipForwardCommand = new RelayCommand(() => _media.SkipBy(10_000));
            SkipBackCommand = new RelayCommand(() => _media.SkipBy(-10_000));
            NextTrackCommand = new RelayCommand(_playlist.PlayNext);
            PrevTrackCommand = new RelayCommand(_playlist.PlayPrevious);
            ToggleMuteCommand = new RelayCommand(() => IsMuted = !IsMuted);
            TogglePlaylistCommand = new RelayCommand(() => IsPlaylistVisible = !IsPlaylistVisible);
            ToggleFullscreenCommand = new RelayCommand(() => IsFullscreen = !IsFullscreen);
            ToggleAlwaysOnTopCommand = new RelayCommand(() => AlwaysOnTop = !AlwaysOnTop);

            OpenFileCommand = new RelayCommand(OpenFile);
            OpenFolderCommand = new RelayCommand(OpenFolder);
            OpenSubtitleCommand = new RelayCommand(OpenSubtitle);
            TakeScreenshotCommand = new RelayCommand(TakeScreenshot);
            SetSleepTimerCommand = new RelayCommand(p => SetSleepTimer((int)p!));
            CancelSleepTimerCommand = new RelayCommand(CancelSleepTimer);
            PlayRecentCommand = new RelayCommand(p => { if (p is string path && !string.IsNullOrEmpty(path)) PlayPath(path); });
            OpenRecentFolderCommand = new RelayCommand(p => { if (p is string dir && !string.IsNullOrEmpty(dir)) OpenFolderPath(dir); });

            ResumeSessionCommand = new RelayCommand(() =>
            {
                IsResumePromptVisible = false;
                var lastFile = _settings.Current.LastPlayedFile;
                var lastPos  = _settings.Current.LastPlaybackPosition;

                if (string.IsNullOrEmpty(lastFile) || !File.Exists(lastFile))
                {
                    _settings.Current.LastPlayedFile = string.Empty;
                    _settings.Save();
                    return;
                }

                try
                {
                    _media.Stop();
                    _playlist.Clear();
                    _playlist.Add(lastFile);
                    _playlist.PlayAt(0);

                    // Seek as soon as LibVLC reports a non-zero Duration (i.e. the file is parsed and
                    // truly seekable). Replaces the old "wait 1 second then hope" pattern that landed
                    // at position 0 if the file was slow to open.
                    EventHandler<MediaPlayerLengthChangedEventArgs>? lenHandler = null;
                    lenHandler = (_, e) =>
                    {
                        if (e.Length <= 0) return;
                        _media.LengthChanged -= lenHandler;
                        // Marshal to UI thread because LengthChanged fires from the native VLC thread.
                        App.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (lastPos > 0 && lastPos < e.Length) _media.SeekTo(lastPos);
                        }));
                    };
                    _media.LengthChanged += lenHandler;
                }
                catch (Exception ex)
                {
                    _notifications.ShowError($"Could not resume session: {ex.Message}");
                }
            });

            DismissResumePromptCommand = new RelayCommand(() =>
            {
                IsResumePromptVisible = false;
                _settings.Current.LastPlayedFile = string.Empty;
                _settings.Current.LastPlaybackPosition = 0;
                _settings.Save();
            });

            ToggleMetadataCommand = new RelayCommand(() =>
            {
                IsMetadataVisible = !IsMetadataVisible;
                if (IsMetadataVisible)
                {
                    MediaMetadataText = _media.GetMediaMetadata();
                }
            });

            NextFrameCommand = new RelayCommand(_media.NextFrame);
            FrameBackCommand = new RelayCommand(() => { if (!IsPlaying && IsMediaLoaded) _media.SkipBy(-34); });
            AdjustSubtitleDelayCommand = new RelayCommand(p =>
            {
                if (p is string s && int.TryParse(s, out int offset))
                {
                    SubtitleDelay += offset;
                }
            });

            AdjustAudioDelayCommand = new RelayCommand(p =>
            {
                if (long.TryParse(p?.ToString(), out long delta))
                    AudioDelay = Math.Clamp(AudioDelay + delta, -2000, 2000);
            });

            SavePlaylistCommand = new RelayCommand(() =>
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "M3U Playlist (*.m3u8)|*.m3u8|All Files (*.*)|*.*",
                    DefaultExt = ".m3u8"
                };
                if (dlg.ShowDialog() == true)
                {
                    try { _playlist.SaveM3U(dlg.FileName); _notifications.ShowSuccess("Playlist saved."); }
                    catch (Exception ex) { _notifications.ShowError($"Save failed: {ex.Message}"); }
                }
            });

            LoadPlaylistCommand = new RelayCommand(() =>
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "M3U Playlist (*.m3u8;*.m3u)|*.m3u8;*.m3u|All Files (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                {
                    try { _playlist.LoadM3U(dlg.FileName); _notifications.ShowSuccess("Playlist loaded."); }
                    catch (Exception ex) { _notifications.ShowError($"Load failed: {ex.Message}"); }
                }
            });

            SetABPointCommand = new RelayCommand(() =>
            {
                if (!IsABRepeatActive)
                {
                    ABPointA = _media.CurrentTime;
                    ABPointB = -1;
                    IsABRepeatActive = true;
                }
                else if (ABPointB == -1)
                {
                    ABPointB = _media.CurrentTime;
                    if (ABPointB <= ABPointA)
                    {
                        ClearABRepeat();
                        return;
                    }
                }
                else
                {
                    ClearABRepeat();
                }
            });

            ClearABRepeatCommand = new RelayCommand(ClearABRepeat);
            SetAPointCommand = new RelayCommand(() =>
            {
                if (!IsMediaLoaded) return;
                ABPointA = _media.CurrentTime;
                ABPointB = -1;
                IsABRepeatActive = true;
            });
            SetBPointCommand = new RelayCommand(() =>
            {
                if (!IsMediaLoaded || !IsABRepeatActive || ABPointA < 0) return;
                var b = _media.CurrentTime;
                if (b > ABPointA) ABPointB = b;
            });

            RateCommand = new RelayCommand(p =>
            {
                // Rate setter already clamps to [0.25, 4.0]; we just need to parse defensively.
                if (p is float f) Rate = f;
                else if (p is double d) Rate = (float)d;
                else if (p is string s && float.TryParse(s, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float r)) Rate = r;
            });

            ChangeLanguageCommand = new RelayCommand(p => 
            {
                if (p is string code)
                {
                    var lang = Languages.FirstOrDefault(l => l.Code == code);
                    if (lang != null) SelectedLanguage = lang;
                }
            });

            SelectAudioTrackCommand = new RelayCommand(p =>
            {
                if (p is MediaTrackInfo track)
                {
                    SelectedAudioTrack = track;
                    OnPropertyChanged(nameof(CurrentAudioTrackId));
                }
            });

            SelectSubtitleTrackCommand = new RelayCommand(p =>
            {
                if (p is MediaTrackInfo track)
                {
                    SelectedSubtitleTrack = track;
                    OnPropertyChanged(nameof(CurrentSubtitleTrackId));
                }
            });

            ToggleVideoAdjustCommand = new RelayCommand(() => IsVideoAdjustVisible = !IsVideoAdjustVisible);
            ToggleSubtitleStyleCommand = new RelayCommand(() => IsSubtitleStyleVisible = !IsSubtitleStyleVisible);

            ResetVideoAdjustmentsCommand = new RelayCommand(() =>
            {
                Brightness = VideoAdjustment.DefaultBrightness;
                Contrast = VideoAdjustment.DefaultContrast;
                Saturation = VideoAdjustment.DefaultSaturation;
                Gamma = VideoAdjustment.DefaultGamma;
                Hue = VideoAdjustment.DefaultHue;
                _notifications.ShowInfo("Video adjustments reset");
            });

            SetSubtitleColorCommand = new RelayCommand(p =>
            {
                int? rgb = p switch
                {
                    SubtitleColorOption opt => opt.Rgb,
                    int i => i,
                    string s when int.TryParse(s, out int parsed) => parsed,
                    _ => null
                };
                if (rgb is int value)
                {
                    _settings.Current.SubtitleColorRgb = value & 0xFFFFFF;
                    _settings.SaveDebounced();
                    OnPropertyChanged(nameof(SubtitleColorPreview));
                }
            });

            ReloadWithSubtitleSettingsCommand = new RelayCommand(() =>
            {
                if (!IsMediaLoaded) return;
                SubtitleReloadRequested?.Invoke(this, EventArgs.Empty); // → MainWindow captures frame overlay
                Task.Run(() => _media.RecreateWithSubtitleSettings(_settings.Current));
                _notifications.ShowSuccess("Applying subtitle settings…");
            });

            _playlist.PlayRequested += (_, item) => {
                try
                {
                    ClearABRepeat(); // Clear on new file
                    SubtitleDelay = 0;
                    AudioDelay = 0;
                    Zoom = 0f; // auto-fit each new file
                    _isChangingMedia = true;  // suppress async Stopped event during file switch (fix 8)
                    _media.Stop();
                    _isChangingMedia = false;
                    IsMediaLoaded = true;
                    _media.PlayFile(item.FilePath);
                    TryAutoLoadSubtitle(item.FilePath);
                    CurrentTitle = item.Title;
                    _settings.Current.LastPlayedFile = item.FilePath;
                    _settings.Current.LastPlaybackPosition = 0;
                    
                    // Automatically add to recent files on play
                    _settings.AddRecentFile(item.FilePath);
                    RefreshRecentFiles();
                    
                    _settings.Save();
                    
                    // Refresh tracks and status
                    RefreshTracks();
                }
                catch (Exception ex)
                {
                    _notifications.ShowError($"Error playing file: {ex.Message}");
                }
            };

            CheckForSessionRestore();
        }

        private void CheckForSessionRestore()
        {
            if (_settings?.Current == null) return;

            var lastFile = _settings.Current.LastPlayedFile;
            if (string.IsNullOrEmpty(lastFile) || !File.Exists(lastFile)) return;

            // Prefer WatchHistory dict; fall back to legacy scalar field
            if (!_settings.Current.WatchHistory.TryGetValue(lastFile, out long savedPos))
                savedPos = _settings.Current.LastPlaybackPosition;

            if (savedPos > 0)
            {
                // Migrate legacy scalar into dict
                if (!_settings.Current.WatchHistory.ContainsKey(lastFile))
                {
                    _settings.Current.WatchHistory[lastFile] = savedPos;
                    _settings.SaveDebounced();
                }
                var fileName = Path.GetFileName(lastFile);
                var time = FormatTime(savedPos);
                ResumePromptMessage = $"Resume {fileName} at {time}?";
                IsResumePromptVisible = true;

                // Auto hide prompt after 10 seconds — single-shot with explicit unsubscribe (A12)
                var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                EventHandler? tick = null;
                tick = (s, e) => { t.Tick -= tick; t.Stop(); IsResumePromptVisible = false; };
                t.Tick += tick;
                t.Start();
            }
        }

        private void OpenFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Media Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.mp3;*.aac;*.flac;*.wav;*.ogg;*.m4a;*.m4v;*.ts;*.m2ts;*.3gp|All Files|*.*",
                Multiselect = true,
                Title = "Open Media File(s)"
            };
            if (dlg.ShowDialog() == true)
            {
                foreach (var f in dlg.FileNames) { _playlist.Add(f); _settings.AddRecentFile(f); }
                if (!string.IsNullOrEmpty(dlg.FileName)) _playlist.PlayAt(_playlist.Items.Count - dlg.FileNames.Length);
                RefreshRecentFiles();
            }
        }

        private void OpenFolder()
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select a folder to add to playlist",
                UseDescriptionForTitle = true
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var dir = dlg.SelectedPath;
            var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mp3", ".aac", ".flac", ".wav", ".ogg", ".m4a", ".m4v", ".ts", ".3gp" };
            var files = Directory.GetFiles(dir)
                .Where(f => exts.Contains(Path.GetExtension(f)))
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var f in files) _playlist.Add(f);
            if (files.Count > 0)
                _playlist.PlayAt(_playlist.Items.IndexOf(_playlist.Items.First(i => i.FilePath == files[0])));
            _settings.AddRecentFolder(dir);
            RefreshRecentFolders();
        }

        private void OpenSubtitle()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Subtitle Files|*.srt;*.ass;*.ssa;*.vtt;*.sub|All Files|*.*", Title = "Load External Subtitle" };
            if (dlg.ShowDialog() == true)
            {
                try { _media.LoadExternalSubtitle(dlg.FileName); }
                catch (InvalidOperationException ex) { _notifications.ShowError(ex.Message); return; }
                
                // Give LibVLC a moment to parse the new track then refresh and auto-select
                DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    RefreshTracks();
                    // Auto-select the last added subtitle track (usually the one just loaded)
                    if (SubtitleTracks.Count > 1)
                    {
                        SelectedSubtitleTrack = SubtitleTracks.Last();
                    }
                    _notifications.ShowSuccess($"Subtitle loaded: {Path.GetFileName(dlg.FileName)}");
                };
                timer.Start();
            }
        }

        private void PlayPath(string path)
        {
            if (!File.Exists(path))
            {
                _notifications.ShowWarning($"File no longer exists: {Path.GetFileName(path)}");
                return;
            }
            try
            {
                _media.Stop(); // Ensure clean state before adding and playing
                _playlist.Add(path);
                var item = _playlist.Items.FirstOrDefault(i => i.FilePath == path);
                if (item != null) _playlist.PlayItem(item);
            }
            catch (Exception ex)
            {
                _notifications.ShowError($"Could not play file: {ex.Message}");
            }
        }

        private void TakeScreenshot()
        {
            if (!IsMediaLoaded)
            {
                _notifications.ShowWarning("Nothing to capture — no media is playing.");
                return;
            }
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DarshanPlayer");
                Directory.CreateDirectory(dir);
                var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                _media.TakeSnapshot(Path.Combine(dir, fileName));
                _notifications.ShowSuccess($"Screenshot saved: {fileName}");
            }
            catch (Exception ex)
            {
                _notifications.ShowError($"Screenshot failed: {ex.Message}");
            }
        }

        private void SetSleepTimer(int minutes)
        {
            _sleepTimer?.Stop();
            _sleepTimerEnd = DateTime.Now.AddMinutes(minutes);
            _sleepTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _sleepTimer.Tick += SleepTimer_Tick;
            _sleepTimer.Start();
            IsSleepTimerActive = true;
        }

        private void CancelSleepTimer()
        {
            _sleepTimer?.Stop();
            _sleepTimer = null;
            IsSleepTimerActive = false;
            SleepTimerLabel = "";
        }

        private void SleepTimer_Tick(object? s, EventArgs e)
        {
            var remaining = _sleepTimerEnd - DateTime.Now;
            if (remaining <= TimeSpan.Zero)
            {
                _media.Pause();
                CancelSleepTimer();
            }
            else
            {
                SleepTimerLabel = $"Sleep: {remaining:mm\\:ss}";
            }
        }

        private void UiTimer_Tick(object? s, EventArgs e) { }

        private void ClearABRepeat()
        {
            IsABRepeatActive = false;
            ABPointA = -1;
            ABPointB = -1;
        }

        private void RefreshTracks()
        {
            var activeAudio = _media.CurrentAudioTrack;
            var activeSub   = _media.CurrentSubtitleTrack;

            // Diff-based sync avoids rebuilding the entire collection (avoids UI thrash on every Playing event, A13)
            SyncTrackCollection(AudioTracks,
                _media.AudioTracks.Select(t => t with { IsActive = t.Id == activeAudio }));
            SyncTrackCollection(SubtitleTracks,
                _media.SubtitleTracks.Select(t => t with { IsActive = t.Id == activeSub }));
            OnPropertyChanged(nameof(HasMultipleAudioTracks));
            OnPropertyChanged(nameof(SubtitleTracksWithOff));
            OnPropertyChanged(nameof(CurrentAudioTrack));
            OnPropertyChanged(nameof(CurrentSubtitleTrack));
        }

        private static void SyncTrackCollection(ObservableCollection<MediaTrackInfo> collection, IEnumerable<MediaTrackInfo> incoming)
        {
            var newList = incoming.ToList();
            var newIds = new HashSet<int>(newList.Select(t => t.Id));

            // Remove stale items
            for (int i = collection.Count - 1; i >= 0; i--)
            {
                if (!newIds.Contains(collection[i].Id))
                    collection.RemoveAt(i);
            }

            // Add or update items
            var existingIds = new HashSet<int>(collection.Select(t => t.Id));
            foreach (var track in newList)
            {
                var idx = collection.IndexOf(collection.FirstOrDefault(t => t.Id == track.Id)!);
                if (idx < 0)
                    collection.Add(track);
                else if (!collection[idx].Equals(track))
                    collection[idx] = track; // update IsActive flag etc.
            }
        }

        private void RefreshRecentFiles()
        {
            RecentFiles.Clear();
            foreach (var f in _settings.Current.RecentFiles) RecentFiles.Add(f);
        }

        private void RefreshRecentFolders()
        {
            RecentFolders.Clear();
            foreach (var d in _settings.Current.RecentFolders) RecentFolders.Add(d);
        }

        private void OpenFolderPath(string dir)
        {
            if (!Directory.Exists(dir)) return;
            var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mp3", ".aac", ".flac", ".wav", ".ogg", ".m4a", ".m4v", ".ts", ".3gp" };
            var files = Directory.GetFiles(dir)
                .Where(f => exts.Contains(Path.GetExtension(f)))
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var f in files) _playlist.Add(f);
            if (files.Count > 0)
                _playlist.PlayAt(_playlist.Items.IndexOf(_playlist.Items.First(i => i.FilePath == files[0])));
            _settings.AddRecentFolder(dir);
            RefreshRecentFolders();
        }

        // ─── Subtitle Auto-Load (Phase 10.1) ─────────────────────────────────
        private static readonly HashSet<string> SubtitleAutoExts =
            new(StringComparer.OrdinalIgnoreCase) { ".srt", ".ass", ".ssa", ".vtt", ".sub" };

        private void TryAutoLoadSubtitle(string mediaPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(mediaPath);
                var baseName = Path.GetFileNameWithoutExtension(mediaPath);
                if (string.IsNullOrEmpty(dir)) return;
                var match = Directory.EnumerateFiles(dir)
                    .FirstOrDefault(f =>
                        Path.GetFileNameWithoutExtension(f).Equals(baseName, StringComparison.OrdinalIgnoreCase)
                        && SubtitleAutoExts.Contains(Path.GetExtension(f)));
                if (match == null) return;
                _media.LoadExternalSubtitle(match);
                _notifications.ShowInfo($"Auto-loaded subtitle: {Path.GetFileName(match)}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TryAutoLoadSubtitle] {ex.Message}");
            }
        }

        private static string FormatTime(long ms)
        {
            if (ms < 0) ms = 0;
            var t = TimeSpan.FromMilliseconds(ms);
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}" : $"{t.Minutes}:{t.Seconds:D2}";
        }

        public void Dispose()
        {
            _media.Playing -= _onPlaying;
            _media.Paused -= _onPaused;
            _media.Stopped -= _onStopped;
            _media.EndReached -= _onEndReached;
            _media.EncounteredError -= _onEncounteredError;
            _media.LengthChanged -= _onLengthChanged;
            _media.TimeChanged -= _onTimeChanged;
            _uiTimer.Stop();
            _uiTimer.Tick -= UiTimer_Tick;
            _sleepTimer?.Stop();
        }

        public void AddDroppedFiles(IEnumerable<string> paths)
        {
            string? firstAddedPath = null;

            var sorted = paths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var path in sorted)
            {
                _playlist.Add(path);
                _settings.AddRecentFile(path);

                firstAddedPath ??= path;
            }

            if (firstAddedPath == null)
                return;

            RefreshRecentFiles();

            var item = _playlist.Items.FirstOrDefault(i => i.FilePath == firstAddedPath);
            if (item != null)
            {
                _playlist.PlayItem(item);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    /// <summary>A named subtitle colour preset (24-bit RGB) shown as a swatch in the UI.</summary>
    public record SubtitleColorOption(string Name, int Rgb)
    {
        /// <summary>Brush for the swatch fill.</summary>
        public System.Windows.Media.Brush Swatch => new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb((byte)((Rgb >> 16) & 0xFF), (byte)((Rgb >> 8) & 0xFF), (byte)(Rgb & 0xFF)));
    }
}
