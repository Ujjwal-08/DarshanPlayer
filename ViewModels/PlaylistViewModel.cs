using DarshanPlayer.Models;
using DarshanPlayer.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;

namespace DarshanPlayer.ViewModels
{
    public class PlaylistViewModel : INotifyPropertyChanged
    {
        public PlaylistService Service { get; }
        private readonly SettingsService _settings;

        public ICommand PlaySelectedCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ShuffleCommand { get; }
        public ICommand ToggleRepeatCommand { get; }
        public ICommand ToggleShuffleCommand { get; }
        public ICommand ToggleSortCommand { get; }

        // ─── Filter & Sort (Phase 13.4) ──────────────────────────────────
        public ICollectionView FilteredItems { get; }
        private string _filterText = "";
        private SortMode _sortMode = SortMode.None;

        public string FilterText
        {
            get => _filterText;
            set { _filterText = value; FilteredItems.Refresh(); OnPropertyChanged(); }
        }

        public SortMode SortMode
        {
            get => _sortMode;
            set
            {
                _sortMode = value;
                FilteredItems.SortDescriptions.Clear();
                if (value == SortMode.ByName)
                    FilteredItems.SortDescriptions.Add(new SortDescription(nameof(PlaylistItem.Title), ListSortDirection.Ascending));
                FilteredItems.Refresh();
                OnPropertyChanged();
            }
        }

        private PlaylistItem? _selectedItem;
        public PlaylistItem? SelectedItem
        {
            get => _selectedItem;
            set { _selectedItem = value; OnPropertyChanged(); }
        }

        public PlaylistViewModel(PlaylistService service, SettingsService settings)
        {
            Service = service;
            _settings = settings;

            FilteredItems = CollectionViewSource.GetDefaultView(service.Items);
            FilteredItems.Filter = obj =>
                obj is PlaylistItem item &&
                (string.IsNullOrWhiteSpace(_filterText) ||
                 item.Title.Contains(_filterText, StringComparison.OrdinalIgnoreCase));

            PlaySelectedCommand = new RelayCommand(() => { if (SelectedItem != null) Service.PlayItem(SelectedItem); });
            RemoveCommand = new RelayCommand(() => { if (SelectedItem != null) Service.Remove(SelectedItem); });
            ClearCommand = new RelayCommand(Service.Clear);
            ShuffleCommand = new RelayCommand(Service.Shuffle);
            ToggleRepeatCommand = new RelayCommand(() =>
            {
                Service.RepeatMode = Service.RepeatMode switch
                {
                    RepeatMode.None => RepeatMode.One,
                    RepeatMode.One => RepeatMode.All,
                    RepeatMode.All => RepeatMode.None,
                    _ => RepeatMode.None
                };
                PersistPlaybackOrderSettings();
                OnPropertyChanged(nameof(RepeatIcon));
                OnPropertyChanged(nameof(RepeatLabel));
            });
            ToggleShuffleCommand = new RelayCommand(() =>
            {
                Service.IsShuffle = !Service.IsShuffle;
                PersistPlaybackOrderSettings();
                OnPropertyChanged(nameof(ShuffleIcon));
                OnPropertyChanged(nameof(ShuffleLabel));
            });
            ToggleSortCommand = new RelayCommand(() =>
                SortMode = SortMode == SortMode.None ? SortMode.ByName : SortMode.None);
        }

        public string RepeatIcon => Service.RepeatMode switch
        {
            RepeatMode.None => "🔁",
            RepeatMode.One => "🔂",
            RepeatMode.All => "🔁",
            _ => "🔁"
        };
        // Shuffle off = sequential arrows; shuffle on = crossed shuffle glyph.
        public string ShuffleIcon => Service.IsShuffle ? "🔀" : "↩";
        public string ShuffleLabel => Service.IsShuffle ? "Shuffle On" : "Shuffle Off";
        public string RepeatLabel => Service.RepeatMode switch
        {
            RepeatMode.None => "Repeat Off",
            RepeatMode.One => "Repeat One",
            RepeatMode.All => "Repeat All",
            _ => "Repeat Off"
        };

        private void PersistPlaybackOrderSettings()
        {
            _settings.Current.RepeatMode = Service.RepeatMode;
            _settings.Current.IsShuffle = Service.IsShuffle;
            _settings.Save();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
