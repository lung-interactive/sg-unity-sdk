using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Request payload for confirming build upload completion.
    /// Verifies that build file was successfully uploaded to S3.
    /// </summary>
    [System.Serializable]
    public class ConfirmUploadDTO
    {
        /// <summary>
        /// Upload session token from StartBuildUploadResponseDTO.
        /// Identifies which upload session is being confirmed.
        /// </summary>
        [JsonProperty("upload_token")]
        public string UploadToken;

        /// <summary>
        /// Semantic version of the uploaded build.
        /// </summary>
        [JsonProperty("semver")]
        public string Semver;

        /// <summary>
        /// Target platform of the uploaded build.
        /// </summary>
        [JsonProperty("platform")]
        public BuildPlatform Platform;
    }
}