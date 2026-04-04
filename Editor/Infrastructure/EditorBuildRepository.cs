using System.Collections.Generic;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.Singletons;

namespace SGUnitySDK.Editor.Infrastructure
{
    /// <summary>
    /// Editor persistence implementation for local build results.
    /// Stores results in DevelopmentProcess version build entries.
    /// </summary>
    public class EditorBuildRepository : IBuildRepository
    {
        /// <inheritdoc />
        public void SaveBuildResult(SGLocalBuildResult buildResult)
        {
            var process = DevelopmentProcess.instance;
            var entries = process.VersionBuilds ?? new List<SGVersionBuildEntry>();

            var entry = new SGVersionBuildEntry { build = buildResult };

            int existingIndex = entries.FindIndex(e =>
                e.build.path == buildResult.path &&
                e.build.platform == buildResult.platform);

            if (existingIndex >= 0)
            {
                entries[existingIndex] = entry;
            }
            else
            {
                entries.Add(entry);
            }

            process.VersionBuilds = entries;
        }

        /// <inheritdoc />
        public SGLocalBuildResult GetBuildResult(string version)
        {
            var process = DevelopmentProcess.instance;
            var entries = process.VersionBuilds;

            if (entries == null || entries.Count == 0)
            {
                return default;
            }

            if (string.IsNullOrEmpty(version))
            {
                return entries[0].build;
            }

            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.build.path) && entry.build.path.Contains(version))
                {
                    return entry.build;
                }
            }

            return default;
        }
    }
}
