using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SGUnitySDK.Editor.Infrastructure.Http.Transport
{
    /// <summary>
    /// Transport payload for start-build-upload endpoint.
    /// </summary>
    [Serializable]
    public sealed class TransportStartBuildUploadRequest
    {
        [JsonProperty("semver")]
        public string Semver;

        [JsonProperty("platform")]
        public int Platform;

        [JsonProperty("executable_name")]
        public string ExecutableName;

        [JsonProperty("filename")]
        public string Filename;

        [JsonProperty("download_size")]
        public ulong DownloadSize;

        [JsonProperty("installed_size")]
        public ulong InstalledSize;

        [JsonProperty("host")]
        public int Host;

        [JsonProperty("override_existing")]
        public bool? OverrideExisting;
    }

    /// <summary>
    /// Transport payload for confirm-build-upload endpoint.
    /// </summary>
    [Serializable]
    public sealed class TransportConfirmUploadRequest
    {
        [JsonProperty("upload_token")]
        public string UploadToken;

        [JsonProperty("semver")]
        public string Semver;

        [JsonProperty("platform")]
        public int Platform;
    }

    /// <summary>
    /// Transport payload for send-to-homologation endpoint.
    /// </summary>
    [Serializable]
    public sealed class TransportSendToHomologationRequest
    {
        [JsonProperty("semver")]
        public string Semver;
    }

    /// <summary>
    /// Transport payload for acknowledge-version endpoint.
    /// </summary>
    [Serializable]
    public sealed class TransportAcknowledgeVersionRequest
    {
        [JsonProperty("notes")]
        public string Notes;
    }

    /// <summary>
    /// Transport query object for versions/filter endpoint.
    /// </summary>
    [Serializable]
    public sealed class TransportVersionFilterQuery
    {
        public int? State;
        public bool? IsCurrent;
        public bool? IsPrerelease;
        public string CreatedAfter;
        public string CreatedBefore;
        public string SemverRaw;
    }

    /// <summary>
    /// Transport payload for start-build-upload endpoint response.
    /// </summary>
    [Serializable]
    public sealed class TransportStartBuildUploadResponse
    {
        [JsonProperty("upload_token")]
        public string UploadToken;

        [JsonProperty("signed_url")]
        public TransportPresignedUrl SignedUrl;
    }

    /// <summary>
    /// Transport payload for presigned URL response fields.
    /// </summary>
    [Serializable]
    public sealed class TransportPresignedUrl
    {
        [JsonProperty("url")]
        public string Url;

        [JsonProperty("expires_at")]
        public DateTime? ExpiresAt;

        [JsonProperty("method")]
        public string Method;

        [JsonProperty("file_key")]
        public string FileKey;

        [JsonProperty("bucket")]
        public string Bucket;

        [JsonProperty("content_type")]
        public string ContentType;

        [JsonProperty("size_limit")]
        public int? SizeLimit;

        [JsonProperty("checksum")]
        public string Checksum;
    }

    /// <summary>
    /// Transport payload for version API responses.
    /// </summary>
    [Serializable]
    public sealed class TransportVersionResponse
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("semver")]
        public JToken Semver;

        [JsonProperty("state")]
        public int State;

        [JsonProperty("is_current")]
        public bool IsCurrent;

        [JsonProperty("is_prerelease")]
        public bool IsPrerelease;

        [JsonProperty("release_notes")]
        public JToken ReleaseNotes;
    }

    /// <summary>
    /// Transport payload for version metadata API responses.
    /// </summary>
    [Serializable]
    public sealed class TransportVersionMetadataResponse
    {
        [JsonProperty("acknowledgment")]
        public JToken Acknowledgment;

        [JsonProperty("development")]
        public JToken Development;

        [JsonProperty("testing")]
        public JToken Testing;

        [JsonProperty("metadata")]
        public JToken Metadata;
    }
}
