using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Request payload for acknowledging a version by the development team.
    /// Marks when development team accepts a version for work.
    /// </summary>
    [System.Serializable]
    public class AcknowledgeVersionDTO
    {
        /// <summary>
        /// Optional notes about acknowledgment or development plans.
        /// Can include implementation notes, concerns, or status.
        /// </summary>
        [JsonProperty("notes")]
        public string Notes;
    }
}