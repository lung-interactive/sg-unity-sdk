using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Request payload for starting a build upload to cloud storage.
    /// Contains build metadata needed to generate presigned upload URLs.
    /// </summary>
    [System.Serializable]
    public class StartBuildUploadDTO
    {
        /// <summary>
        /// Semantic version of the build being uploaded.
        /// </summary>
        [JsonProperty("semver")]
        public string Semver;

        /// <summary>
        /// Target platform for this build.
        /// </summary>
        [JsonProperty("platform")]
        public BuildPlatform Platform;

        /// <summary>
        /// Name of the executable file (e.g., "GameClient.exe", "game.app").
        /// </summary>
        [JsonProperty("executable_name")]
        public string ExecutableName;

        /// <summary>
        /// Filename of the packaged build (e.g., "game-1.0.0-windows.zip").
        /// </summary>
        [JsonProperty("filename")]
        public string Filename;

        /// <summary>
        /// Download size of the build in bytes.
        /// </summary>
        [JsonProperty("download_size")]
        public ulong DownloadSize;

        /// <summary>
        /// Installed size of the build in bytes.
        /// Size after extraction/installation on client machine.
        /// </summary>
        [JsonProperty("installed_size")]
        public ulong InstalledSize;

        /// <summary>
        /// Cloud storage provider for this build.
        /// </summary>
        [JsonProperty("host")]
        public FileHost Host;

        /// <summary>
        /// Whether to replace existing build for this platform/version.
        /// If false and build exists, upload will be rejected.
        /// </summary>
        [JsonProperty("override_existing")]
        public bool? OverrideExisting;
    }
}