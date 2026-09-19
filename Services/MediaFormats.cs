using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DarshanPlayer.Services
{
    /// <summary>
    /// The single source of truth for which files Darshan Player opens. Used by the folder scan,
    /// drag-and-drop, command-line handling and the Windows file-type registration, which used to
    /// keep separate lists that had drifted apart (the folder scan was missing .m2ts).
    /// </summary>
    public static class MediaFormats
    {
        public static readonly IReadOnlyList<string> VideoExtensions = new[]
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v",
            ".ts", ".m2ts", ".3gp", ".mpg", ".mpeg",
        };

        public static readonly IReadOnlyList<string> AudioExtensions = new[]
        {
            ".mp3", ".aac", ".flac", ".wav", ".ogg", ".m4a", ".opus", ".wma",
        };

        public static IEnumerable<string> AllExtensions => VideoExtensions.Concat(AudioExtensions);

        private static readonly HashSet<string> All =
            new(VideoExtensions.Concat(AudioExtensions), StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> Audio =
            new(AudioExtensions, StringComparer.OrdinalIgnoreCase);

        public static bool IsMedia(string path) =>
            !string.IsNullOrWhiteSpace(path) && All.Contains(Path.GetExtension(path));

        public static bool IsAudio(string path) =>
            !string.IsNullOrWhiteSpace(path) && Audio.Contains(Path.GetExtension(path));

        /// <summary>
        /// Turn whatever the shell or a drag-and-drop handed us into playable files: folders are
        /// expanded to the media directly inside them (name-sorted), unsupported files are dropped,
        /// and duplicates are removed while keeping first-seen order.
        /// </summary>
        public static List<string> ExpandToMediaFiles(IEnumerable<string> paths)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in paths)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var path = raw.Trim().Trim('"');

                IEnumerable<string> candidates;
                try
                {
                    candidates = Directory.Exists(path)
                        ? Directory.EnumerateFiles(path)
                            .Where(IsMedia)
                            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                        : IsMedia(path) ? new[] { path } : Array.Empty<string>();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Log.Warn("Shell", "Could not read {Path}: {Reason}", path, ex.Message);
                    continue;
                }

                foreach (var file in candidates)
                    if (seen.Add(file)) result.Add(file);
            }

            return result;
        }
    }
}
