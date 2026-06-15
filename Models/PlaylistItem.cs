using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace DarshanPlayer.Models
{
    public class PlaylistItem : INotifyPropertyChanged
    {
        private bool _isCurrentlyPlaying;
        private string _filePath = string.Empty;
        private TimeSpan _durationTimeSpan;
        private string? _artist;

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Title));
            }
        }

        /// <summary>
        /// Display title derived from <see cref="FilePath"/>. Returns "Unknown" when the path is
        /// null/empty so the playlist UI never blows up on a malformed entry.
        /// </summary>
        public string Title
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_filePath)) return "Unknown";
                try
                {
                    var name = Path.GetFileNameWithoutExtension(_filePath);
                    return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
                }
                catch
                {
                    // Defensive: Path.* can throw on paths with invalid chars.
                    return "Unknown";
                }
            }
        }

        /// <summary>Human-readable duration string for the playlist column (e.g. "3:42" or "1:02:15").</summary>
        public string Duration { get; set; } = "--:--";

        /// <summary>Structured duration; populated by metadata extraction (future feature 13.6).</summary>
        public TimeSpan DurationTimeSpan
        {
            get => _durationTimeSpan;
            set
            {
                _durationTimeSpan = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Artist tag from the media file (TagLib# or LibVLC). Null when unknown.</summary>
        public string? Artist
        {
            get => _artist;
            set { _artist = value; OnPropertyChanged(); }
        }

        public bool IsCurrentlyPlaying
        {
            get => _isCurrentlyPlaying;
            set { _isCurrentlyPlaying = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
