using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace DarshanPlayer.Services
{
    public class UpdateService
    {
        // Replace with your actual GitHub repo URL before shipping
        private const string RepoUrl = "https://github.com/Ujjwal-08/DarshanPlayer";

        private UpdateManager? _mgr;
        private UpdateInfo? _pendingUpdate;

        public bool HasPendingUpdate => _pendingUpdate != null;

        public async Task<string?> CheckAndDownloadAsync()
        {
            try
            {
                _mgr = new UpdateManager(new GithubSource(RepoUrl, null, false));
                _pendingUpdate = await _mgr.CheckForUpdatesAsync();
                if (_pendingUpdate == null) return null;

                var version = _pendingUpdate.TargetFullRelease.Version.ToString();
                // Download in background so UI stays responsive
                await _mgr.DownloadUpdatesAsync(_pendingUpdate);
                return version;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateService] {ex.Message}");
                return null;
            }
        }

        /// <summary>Apply the downloaded update on next app restart.</summary>
        public void ApplyOnRestart()
        {
            if (_mgr == null || _pendingUpdate == null) return;
            _mgr.ApplyUpdatesAndRestart(_pendingUpdate);
        }
    }
}
