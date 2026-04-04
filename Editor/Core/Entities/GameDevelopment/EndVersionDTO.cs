using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Data transfer object for ending/releasing a version.
    /// Used internally for API mapping and version lifecycle transitions.
    /// </summary>
    [System.Serializable]
    public class EndVersionDTO
    {
        /// <summary>
        /// Semantic version of the version being released.
        /// </summary>
        [JsonProperty("semver")]
        public string Semver;
    }
}