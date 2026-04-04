using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Request payload for creating a new version.
    /// Specifies version number and release notes for the new version.
    /// </summary>
    [System.Serializable]
    public class StartGameVersionUpdateDTO
    {
        /// <summary>
        /// Type of version increment (Patch, Minor, Major, Specific).
        /// </summary>
        [JsonProperty("version_update_type")]
        public VersionUpdateType VersionUpdateType;

        /// <summary>
        /// Specific version number to use.
        /// Required if VersionUpdateType is Specific, ignored otherwise.
        /// </summary>
        [JsonProperty("specific_version")]
        public string SpecificVersion;

        /// <summary>
        /// Whether this version is a pre-release (alpha, beta, RC).
        /// Pre-release versions are not recommended for production use.
        /// </summary>
        [JsonProperty("is_prerelease")]
        public bool IsPrerelease;

        /// <summary>
        /// Release notes and changelog for this version.
        /// Supports multiple languages via localization.
        /// </summary>
        [JsonProperty("release_notes")]
        public ReleaseNotesDTO ReleaseNotes = new();
    }
}