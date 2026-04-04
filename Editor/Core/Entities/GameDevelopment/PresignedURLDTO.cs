using System;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Presigned URL data for secure file upload to S3.
    /// Contains all necessary information for uploading builds directly to cloud.
    /// </summary>
    [System.Serializable]
    public class PresignedURLDTO
    {
        /// <summary>
        /// Presigned URL for direct upload to S3.
        /// Valid for a limited time specified by ExpiresAt.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// ISO 8601 timestamp when the presigned URL expires.
        /// </summary>
        [JsonProperty("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// HTTP method to use for upload (typically PUT or POST).
        /// </summary>
        [JsonProperty("method")]
        public string Method { get; set; }

        /// <summary>
        /// Object key/path in S3 bucket where file will be stored.
        /// </summary>
        [JsonProperty("file_key")]
        public string FileKey { get; set; }

        /// <summary>
        /// S3 bucket name for the upload destination.
        /// </summary>
        [JsonProperty("bucket")]
        public string Bucket { get; set; }

        /// <summary>
        /// Required Content-Type header for the upload request.
        /// </summary>
        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        /// <summary>
        /// Maximum file size in bytes allowed for upload.
        /// </summary>
        [JsonProperty("size_limit")]
        public int? SizeLimit { get; set; }

        /// <summary>
        /// Expected file checksum for integrity verification.
        /// </summary>
        [JsonProperty("checksum")]
        public string Checksum { get; set; }
    }
}