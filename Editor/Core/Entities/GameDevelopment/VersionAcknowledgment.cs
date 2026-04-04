using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Acknowledgment information for a version.
    /// Tracks when the development team acknowledged and started working on the version.
    /// </summary>
    [System.Serializable]
    public class VersionAcknowledgment
    {
        /// <summary>
        /// Whether the version has been acknowledged by the development team.
        /// </summary>
        [JsonProperty("acknowledged")]
        public bool Acknowledged { get; set; }

        /// <summary>
        /// ISO 8601 timestamp when the version was acknowledged.
        /// </summary>
        [JsonProperty("acknowledged_at")]
        public string AcknowledgedAt { get; set; }

        /// <summary>
        /// Optional notes about the acknowledgment or development plans.
        /// </summary>
        [JsonProperty("notes")]
        public string Notes { get; set; }
    }
}