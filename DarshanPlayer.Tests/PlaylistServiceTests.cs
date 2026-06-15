using DarshanPlayer.Models;
using DarshanPlayer.Services;
using Xunit;

namespace DarshanPlayer.Tests
{
    public class PlaylistServiceTests
    {
        private static PlaylistService WithItems(params string[] paths)
        {
            var svc = new PlaylistService();
            foreach (var p in paths) svc.Add(p);
            return svc;
        }

        [Fact]
        public void Add_IgnoresDuplicatePaths_CaseInsensitive()
        {
            var svc = WithItems(@"C:\Movies\a.mp4");
            svc.Add(@"c:\movies\A.MP4"); // same file, different casing

            Assert.Single(svc.Items);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Add_IgnoresNullOrWhitespace(string? path)
        {
            var svc = new PlaylistService();
            svc.Add(path!);
            Assert.Empty(svc.Items);
        }

        [Fact]
        public void MoveItem_ReordersCollection()
        {
            var svc = WithItems(@"C:\1.mp4", @"C:\2.mp4", @"C:\3.mp4");

            svc.MoveItem(0, 2);

            Assert.Equal(@"C:\2.mp4", svc.Items[0].FilePath);
            Assert.Equal(@"C:\3.mp4", svc.Items[1].FilePath);
            Assert.Equal(@"C:\1.mp4", svc.Items[2].FilePath);
        }

        [Fact]
        public void MoveItem_ThrowsOnOutOfRange()
        {
            var svc = WithItems(@"C:\1.mp4", @"C:\2.mp4");

            Assert.Throws<System.ArgumentOutOfRangeException>(() => svc.MoveItem(0, 5));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => svc.MoveItem(-1, 0));
        }

        [Fact]
        public void MoveItem_KeepsCurrentPointerOnSameTrack()
        {
            var svc = WithItems(@"C:\1.mp4", @"C:\2.mp4", @"C:\3.mp4");
            svc.PlayAt(0); // current = item "1"
            var current = svc.Current;

            svc.MoveItem(0, 2); // move "1" to the end

            Assert.Same(current, svc.Current);
            Assert.Equal(2, svc.CurrentIndex);
        }

        [Fact]
        public void Remove_CurrentItem_KeepsIndexInBounds()
        {
            var svc = WithItems(@"C:\1.mp4", @"C:\2.mp4", @"C:\3.mp4");
            svc.PlayAt(1);

            svc.Remove(svc.Items[1]);

            Assert.Equal(2, svc.Items.Count);
            Assert.Equal(1, svc.CurrentIndex);
            Assert.True(svc.Current!.IsCurrentlyPlaying);
        }

        [Fact]
        public void PlayNext_RepeatAll_WrapsToFirst()
        {
            var svc = WithItems(@"C:\1.mp4", @"C:\2.mp4", @"C:\3.mp4");
            svc.RepeatMode = RepeatMode.All;
            svc.PlayAt(2);

            svc.PlayNext();

            Assert.Equal(0, svc.CurrentIndex);
        }

        [Fact]
        public void PlayNext_RepeatOne_StaysOnCurrentTrack()
        {
            var svc = WithItems(@"C:\1.mp4", @"C:\2.mp4", @"C:\3.mp4");
            svc.RepeatMode = RepeatMode.One;
            svc.PlayAt(1);

            svc.PlayNext();

            Assert.Equal(1, svc.CurrentIndex);
        }

        [Fact]
        public void HasNext_IsFalse_AtEnd_WithoutRepeat()
        {
            var svc = WithItems(@"C:\1.mp4", @"C:\2.mp4");
            svc.RepeatMode = RepeatMode.None;
            svc.IsShuffle = false;
            svc.PlayAt(1);

            Assert.False(svc.HasNext());
        }

        [Fact]
        public void Shuffle_PreservesCurrentItem()
        {
            var svc = WithItems(@"C:\1.mp4", @"C:\2.mp4", @"C:\3.mp4", @"C:\4.mp4", @"C:\5.mp4");
            svc.PlayAt(2);
            var current = svc.Current;

            svc.Shuffle();

            Assert.Same(current, svc.Current);
            Assert.Equal(svc.Items.IndexOf(current!), svc.CurrentIndex);
        }
    }
}
