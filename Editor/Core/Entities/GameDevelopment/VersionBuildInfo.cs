using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Build information for a version.
    /// </summary>
    [System.Serializable]
    public class VersionBuildInfo
    {
        /// <summary>
        /// Commit hash associated with this version build.
        /// </summary>
        [JsonProperty("commit_hash")]
        public string CommitHash { get; set; }

        /// <summary>
        /// Git branch associated with this version build.
        /// </summary>
        [JsonProperty("branch")]
        public string Branch { get; set; }

        /// <summary>
        /// Build number or identifier.
        /// </summary>
        [JsonProperty("build_number")]
        public string BuildNumber { get; set; }
    }
}