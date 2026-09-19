using System;
using DarshanPlayer.Services;
using Xunit;

namespace DarshanPlayer.Tests
{
    public class UpdateServiceTests
    {
        [Fact]
        public void ReleasesPageUrl_PointsAtTheLatestReleaseOfTheUpdateRepo()
        {
            // The title-bar button and Velopack's GithubSource must agree: if they drift, users
            // get sent somewhere other than where the auto-updater pulls builds from.
            Assert.Equal(
                "https://github.com/Ujjwal-08/DarshanPlayer/releases/latest",
                UpdateService.ReleasesPageUrl);
        }

        [Fact]
        public void ReleasesPageUrl_IsAWellFormedHttpsUrl()
        {
            Assert.True(Uri.TryCreate(UpdateService.ReleasesPageUrl, UriKind.Absolute, out var uri));
            Assert.Equal(Uri.UriSchemeHttps, uri!.Scheme);
        }
    }
}
