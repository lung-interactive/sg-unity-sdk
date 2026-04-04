using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Request payload for sending a version to homologation.
    /// This payload matches the server's `SendToHomologationDTO` shape.
    /// </summary>
    [System.Serializable]
    public class SendToHomologationDTO
    {
        /// <summary>
        /// Semantic version being sent to homologation.
        /// </summary>
        [JsonProperty("semver")]
        public string Semver;
    }
}