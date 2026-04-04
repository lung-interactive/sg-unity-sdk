using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Testing information for a version.
    /// Tracks testing progress and results.
    /// </summary>
    [System.Serializable]
    public class VersionTestingInfo
    {
        /// <summary>
        /// Current status of testing phase.
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Current testing phase (internal, alpha, beta, etc).
        /// </summary>
        [JsonProperty("phase")]
        public string Phase { get; set; }

        /// <summary>
        /// Number of tests completed.
        /// </summary>
        [JsonProperty("tests_completed")]
        public int? TestsCompleted { get; set; }

        /// <summary>
        /// Total number of tests planned.
        /// </summary>
        [JsonProperty("tests_total")]
        public int? TestsTotal { get; set; }

        /// <summary>
        /// Number of critical issues found.
        /// </summary>
        [JsonProperty("critical_issues_found")]
        public int? CriticalIssuesFound { get; set; }

        /// <summary>
        /// ISO 8601 timestamp when testing started.
        /// </summary>
        [JsonProperty("started_at")]
        public string StartedAt { get; set; }

        /// <summary>
        /// ISO 8601 timestamp when testing was completed.
        /// </summary>
        [JsonProperty("completed_at")]
        public string CompletedAt { get; set; }

        /// <summary>
        /// Notes about testing results, issues found, or test coverage.
        /// </summary>
        [JsonProperty("notes")]
        public string Notes { get; set; }
    }
}