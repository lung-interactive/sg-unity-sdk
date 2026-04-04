using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Game build artifact information.
    /// Represents a compiled and packaged build for a specific platform.
    /// </summary>
    [System.Serializable]
    public class GameBuildDTO
    {
        /// <summary>
        /// Filename of the build artifact (e.g., "game-1.0.0-win.zip").
        /// </summary>
        [JsonProperty("filename")]
        public string Filename { get; set; }

        /// <summary>
        /// Target platform this build was compiled for.
        /// </summary>
        [JsonProperty("platform")]
        public BuildPlatform Platform { get; set; }

        /// <summary>
        /// Source path or URL where the build is located.
        /// </summary>
        [JsonProperty("src")]
        public string Src { get; set; }

        /// <summary>
        /// Cloud storage provider hosting this build.
        /// </summary>
        [JsonProperty("host")]
        public FileHost Host { get; set; }
    }
}