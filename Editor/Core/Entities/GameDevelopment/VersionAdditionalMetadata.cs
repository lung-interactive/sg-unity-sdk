using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Additional metadata for a version.
    /// Flexible structure for tracking external references and custom data.
    /// </summary>
    [System.Serializable]
    public class VersionAdditionalMetadata
    {
        /// <summary>
        /// Array of tags for categorization or filtering.
        /// Example: ["hotfix", "breaking-change", "feature-update"]
        /// </summary>
        [JsonProperty("tags")]
        public string[] Tags { get; set; }

        /// <summary>
        /// Array of linked issue IDs (JIRA, GitHub, Trello, etc).
        /// Example: ["JIRA-123", "GH-456"]
        /// </summary>
        [JsonProperty("linked_issues")]
        public string[] LinkedIssues { get; set; }

        /// <summary>
        /// Build or commit information.
        /// </summary>
        [JsonProperty("build_info")]
        public VersionBuildInfo BuildInfo { get; set; }

        /// <summary>
        /// Arbitrary custom fields for project-specific extensibility.
        /// </summary>
        [JsonProperty("custom_fields")]
        public Dictionary<string, object> CustomFields { get; set; }
    }
}