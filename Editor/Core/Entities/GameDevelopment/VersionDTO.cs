using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Data transfer object representing a game version.
    /// Contains all core version information returned from the API.
    /// </summary>
    [System.Serializable]
    public class VersionDTO
    {
        /// <summary>
        /// Unique identifier (UUID v4) for this version.
        /// </summary>
        [JsonProperty("id")]
        public string Id;

        /// <summary>
        /// Semantic version information (major.minor.patch).
        /// </summary>
        [JsonProperty("semver")]
        public SemVerType Semver;

        /// <summary>
        /// Current lifecycle state of this version.
        /// See GameVersionState enum for possible values.
        /// </summary>
        [JsonProperty("state")]
        public int State;

        /// <summary>
        /// Whether this version is marked as the current released version.
        /// Only one version should have this flag set to true.
        /// </summary>
        [JsonProperty("is_current")]
        public bool IsCurrent;

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
        public ReleaseNotesDTO ReleaseNotes;
    }
}