using System;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Query parameters for filtering game versions.
    /// All parameters are optional and combined using AND logic.
    /// </summary>
    [Serializable]
    public class FilterVersionsDTO
    {
        /// <summary>
        /// Filter by version state (1-8).
        /// See GameVersionState enum for valid values.
        /// </summary>
        public int? State;

        /// <summary>
        /// Filter by current version flag.
        /// Only returns versions marked as current release.
        /// </summary>
        public bool? IsCurrent;

        /// <summary>
        /// Filter by prerelease status.
        /// True: only pre-release versions. False: only stable versions.
        /// </summary>
        public bool? IsPrerelease;

        /// <summary>
        /// Filter versions created after this date (ISO 8601 format).
        /// Example: 2026-01-15T10:30:00Z
        /// </summary>
        public string CreatedAfter;

        /// <summary>
        /// Filter versions created before this date (ISO 8601 format).
        /// Example: 2026-01-15T10:30:00Z
        /// </summary>
        public string CreatedBefore;

        /// <summary>
        /// Filter by semantic version raw string.
        /// Supports exact match or pattern matching.
        /// Example: 1.2.3-alpha.1
        /// </summary>
        public string SemverRaw;
    }
}