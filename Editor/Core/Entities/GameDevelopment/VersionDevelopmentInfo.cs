using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Development progress information for a version.
    /// Managed by developers via game management token.
    /// </summary>
    [System.Serializable]
    public class VersionDevelopmentInfo
    {
        /// <summary>
        /// Current status of development work.
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// ISO 8601 timestamp when development started.
        /// </summary>
        [JsonProperty("started_at")]
        public string StartedAt { get; set; }

        /// <summary>
        /// ISO 8601 timestamp when development was completed.
        /// </summary>
        [JsonProperty("completed_at")]
        public string CompletedAt { get; set; }

        /// <summary>
        /// Priority level for this version development.
        /// </summary>
        [JsonProperty("priority")]
        public string Priority { get; set; }

        /// <summary>
        /// ISO 8601 timestamp for estimated completion.
        /// </summary>
        [JsonProperty("estimated_completion")]
        public string EstimatedCompletion { get; set; }

        /// <summary>
        /// Percentage of completion (0-100).
        /// </summary>
        [JsonProperty("progress_percentage")]
        public int? ProgressPercentage { get; set; }

        /// <summary>
        /// Notes about development progress, blockers, or changes.
        /// </summary>
        [JsonProperty("notes")]
        public string Notes { get; set; }
    }
}