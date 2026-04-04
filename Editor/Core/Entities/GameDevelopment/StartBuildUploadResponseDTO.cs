using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Response from build upload initiation.
    /// Contains presigned URL and token for completing the upload.
    /// </summary>
    [System.Serializable]
    public class StartBuildUploadResponseDTO
    {
        /// <summary>
        /// Token identifying this upload session.
        /// Required when confirming upload completion.
        /// </summary>
        [JsonProperty("upload_token")]
        public string UploadToken;

        /// <summary>
        /// Presigned URL data for direct S3 upload.
        /// Contains URL, expiration time, and security credentials.
        /// </summary>
        [JsonProperty("signed_url")]
        public PresignedURLDTO SignedUrl;
    }
}