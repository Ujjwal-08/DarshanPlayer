using System;
using System.Collections.Generic;
using System.ComponentModel;
using DarshanPlayer.Models;
using Xunit;

namespace DarshanPlayer.Tests
{
    public class PlaylistItemTests
    {
        [Theory]
        [InlineData(0, "--:--")]           // not yet parsed
        [InlineData(-5, "--:--")]          // defensive: negative durations
        [InlineData(9, "0:09")]
        [InlineData(62, "1:02")]
        [InlineData(222, "3:42")]
        [InlineData(3599, "59:59")]
        [InlineData(3600, "1:00:00")]      // hour boundary switches format
        [InlineData(3735, "1:02:15")]
        [InlineData(37230, "10:20:30")]
        public void FormatDuration_MatchesMediaPlayerConventions(int seconds, string expected)
        {
            Assert.Equal(expected, PlaylistItem.FormatDuration(TimeSpan.FromSeconds(seconds)));
        }

        [Fact]
        public void Duration_StartsAsPlaceholder()
        {
            Assert.Equal("--:--", new PlaylistItem { FilePath = @"C:\a.mp4" }.Duration);
        }

        [Fact]
        public void Duration_TracksDurationTimeSpan()
        {
            var item = new PlaylistItem { FilePath = @"C:\a.mp4" };
            item.DurationTimeSpan = TimeSpan.FromSeconds(222);
            Assert.Equal("3:42", item.Duration);
        }

        [Fact]
        public void SettingDuration_RaisesPropertyChangedForBoundColumn()
        {
            var item = new PlaylistItem { FilePath = @"C:\a.mp4" };
            var seen = new List<string?>();
            ((INotifyPropertyChanged)item).PropertyChanged += (_, e) => seen.Add(e.PropertyName);

            item.DurationTimeSpan = TimeSpan.FromMinutes(3);

            // Without the Duration notification the playlist column would keep showing "--:--".
            Assert.Contains(nameof(PlaylistItem.DurationTimeSpan), seen);
            Assert.Contains(nameof(PlaylistItem.Duration), seen);
        }

        [Fact]
        public void SettingSameDuration_DoesNotRenotify()
        {
            var item = new PlaylistItem { FilePath = @"C:\a.mp4", DurationTimeSpan = TimeSpan.FromMinutes(3) };
            var count = 0;
            ((INotifyPropertyChanged)item).PropertyChanged += (_, _) => count++;

            item.DurationTimeSpan = TimeSpan.FromMinutes(3);

            Assert.Equal(0, count);
        }

        [Fact]
        public void Title_FallsBackToUnknownOnBlankPath()
        {
            Assert.Equal("Unknown", new PlaylistItem { FilePath = "" }.Title);
        }
    }
}
