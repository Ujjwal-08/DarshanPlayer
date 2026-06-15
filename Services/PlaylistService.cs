using DarshanPlayer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace DarshanPlayer.Services
{
    public class PlaylistService
    {
        private readonly Random _rng = new();
        private List<int> _shufflePool = new();

        public ObservableCollection<PlaylistItem> Items { get; } = new();
        public int CurrentIndex { get; private set; } = -1;
        public RepeatMode RepeatMode { get; set; } = RepeatMode.None;

        private bool _isShuffle;
        public bool IsShuffle
        {
            get => _isShuffle;
            set { _isShuffle = value; _shufflePool.Clear(); }
        }

        public PlaylistItem? Current => CurrentIndex >= 0 && CurrentIndex < Items.Count
            ? Items[CurrentIndex] : null;

        public event EventHandler<PlaylistItem>? PlayRequested;

        public void Add(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;
            // Windows paths are case-insensitive — "C:\Foo.mp4" and "c:\foo.MP4" are the same file.
            if (!Items.Any(i => string.Equals(i.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
            {
                Items.Add(new PlaylistItem { FilePath = filePath });
                _shufflePool.Clear(); // stale indices after count change
            }
        }

        /// <summary>
        /// Move an item from one index to another, keeping <see cref="CurrentIndex"/> pointed at the
        /// same logical track. Used by drag-and-drop reorder (future feature 13.5).
        /// </summary>
        public void MoveItem(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= Items.Count)
                throw new ArgumentOutOfRangeException(nameof(fromIndex));
            if (toIndex < 0 || toIndex >= Items.Count)
                throw new ArgumentOutOfRangeException(nameof(toIndex));
            if (fromIndex == toIndex) return;

            var trackedCurrent = Current; // capture the item the user is currently on
            Items.Move(fromIndex, toIndex);

            // After Move, locate the previously-current item and re-point CurrentIndex at it.
            if (trackedCurrent != null)
            {
                var newIdx = Items.IndexOf(trackedCurrent);
                if (newIdx >= 0) CurrentIndex = newIdx;
            }
        }

        public void Remove(PlaylistItem item)
        {
            var idx = Items.IndexOf(item);
            var wasCurrent = idx == CurrentIndex;
            Items.Remove(item);
            _shufflePool.Clear(); // stale indices after count change

            if (Items.Count == 0)
            {
                CurrentIndex = -1;
                return;
            }

            if (wasCurrent)
            {
                CurrentIndex = Math.Min(idx, Items.Count - 1);
                for (int i = 0; i < Items.Count; i++)
                    Items[i].IsCurrentlyPlaying = i == CurrentIndex;
                PlayRequested?.Invoke(this, Items[CurrentIndex]);
                return;
            }

            if (idx < CurrentIndex)
                CurrentIndex--;
        }

        public void Clear()
        {
            Items.Clear();
            _shufflePool.Clear();
            CurrentIndex = -1;
        }

        public void PlayAt(int index)
        {
            if (index < 0 || index >= Items.Count) return;
            if (CurrentIndex >= 0 && CurrentIndex < Items.Count)
                Items[CurrentIndex].IsCurrentlyPlaying = false;
            CurrentIndex = index;
            Items[CurrentIndex].IsCurrentlyPlaying = true;
            PlayRequested?.Invoke(this, Items[CurrentIndex]);
        }

        public void PlayItem(PlaylistItem item)
        {
            var idx = Items.IndexOf(item);
            if (idx >= 0) PlayAt(idx);
        }

        public bool HasNext()
        {
            if (Items.Count == 0) return false;
            if (RepeatMode == RepeatMode.All || IsShuffle) return true;
            return CurrentIndex < Items.Count - 1;
        }

        private void RefillShufflePool()
        {
            _shufflePool = Enumerable.Range(0, Items.Count)
                .Where(i => i != CurrentIndex)
                .OrderBy(_ => _rng.Next())
                .ToList();
        }

        public void PlayNext()
        {
            if (Items.Count == 0) return;
            if (RepeatMode == RepeatMode.One) { PlayAt(CurrentIndex); return; }
            if (IsShuffle)
            {
                if (_shufflePool.Count == 0) RefillShufflePool();
                if (_shufflePool.Count == 0) return; // single-track edge case
                var idx = _shufflePool[^1];
                _shufflePool.RemoveAt(_shufflePool.Count - 1);
                PlayAt(idx);
                return;
            }
            if (CurrentIndex < Items.Count - 1) PlayAt(CurrentIndex + 1);
            else if (RepeatMode == RepeatMode.All) PlayAt(0);
        }

        public void PlayPrevious()
        {
            if (Items.Count == 0) return;
            if (CurrentIndex > 0) PlayAt(CurrentIndex - 1);
            else if (RepeatMode == RepeatMode.All) PlayAt(Items.Count - 1);
        }

        public void SaveM3U(string path)
        {
            var lines = new List<string> { "#EXTM3U" };
            foreach (var item in Items)
                lines.Add(item.FilePath);
            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        public void LoadM3U(string path)
        {
            foreach (var line in File.ReadLines(path, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
                Add(line.Trim());
            }
        }

        public void Shuffle()
        {
            var currentItem = Current;

            // Fisher-Yates in-place
            for (int i = Items.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = Items[i];
                Items[i] = Items[j];
                Items[j] = tmp;
            }
            if (currentItem != null)
            {
                CurrentIndex = Items.IndexOf(currentItem);
            }
            else
            {
                CurrentIndex = Items.Count > 0 ? 0 : -1;
            }
        }
    }
}
