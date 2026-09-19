using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Shell;

namespace DarshanPlayer.Services
{
    /// <summary>
    /// Recent files in the taskbar and Start Menu right-click menu (checklist 19.2).
    ///
    /// Entries are JumpTasks that relaunch the exe with the file as an argument, rather than
    /// JumpPaths, which Windows silently hides unless the app is the registered default for that
    /// extension. Relaunching goes through single-instance handoff, so it plays in the open window.
    /// </summary>
    public static class JumpListService
    {
        public const int MaxRecentItems = 10;
        private const string RecentCategory = "Recent";

        /// <summary>The jump-list entries for a recent-files list; pure, so it can be tested.</summary>
        public static IReadOnlyList<JumpTask> BuildTasks(IEnumerable<string> recentFiles, string exePath)
        {
            return recentFiles
                .Where(f => !string.IsNullOrWhiteSpace(f) && File.Exists(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxRecentItems)
                .Select(f => new JumpTask
                {
                    Title = Path.GetFileNameWithoutExtension(f),
                    Description = f,
                    ApplicationPath = exePath,
                    Arguments = $"\"{f}\"",
                    IconResourcePath = exePath,
                    IconResourceIndex = 0,
                    CustomCategory = RecentCategory,
                })
                .ToList();
        }

        /// <summary>Replace the app's jump list. Failures are logged, never thrown.</summary>
        public static void Update(IEnumerable<string> recentFiles)
        {
            try
            {
                var app = Application.Current;
                if (app == null) return;

                var list = new JumpList { ShowRecentCategory = false, ShowFrequentCategory = false };
                foreach (var task in BuildTasks(recentFiles, AppIdentity.ExePath))
                    list.JumpItems.Add(task);

                JumpList.SetJumpList(app, list);
                list.Apply();
            }
            catch (Exception ex)
            {
                Log.Warn("Shell", "Jump list update failed: {Reason}", ex.Message);
            }
        }
    }
}
