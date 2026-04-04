using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Complete version metadata structure.
    /// </summary>
    [System.Serializable]
    public class VersionMetadata
    {
        /// <summary>
        /// Acknowledgment information - when dev team accepted the version.
        /// </summary>
        [JsonProperty("acknowledgment")]
        public VersionAcknowledgment Acknowledgment { get; set; }

        /// <summary>
        /// Development progress and status tracking.
        /// </summary>
        [JsonProperty("development")]
        public VersionDevelopmentInfo Development { get; set; }

        /// <summary>
        /// Testing progress, results, and quality metrics.
        /// </summary>
        [JsonProperty("testing")]
        public VersionTestingInfo Testing { get; set; }

        /// <summary>
        /// Additional metadata for external integrations and custom data.
        /// </summary>
        [JsonProperty("metadata")]
        public VersionAdditionalMetadata Metadata { get; set; }
    }
}