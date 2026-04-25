using System;
using UnityEditor;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Version-aware entry that couples a local build result with upload status.
    /// </summary>
    [Serializable]
    public struct SGVersionBuildEntry
    {
        public SGLocalBuildResult build;

        // Upload state
        public bool uploaded;
        public string remoteUrl;
        public string sha256;
        public string uploadError;

        // Upload session tracking
        public string uploadToken;
        public string signedUrl;
        public string signedUrlContentType;
        public long signedUrlExpiresUnixTimestamp;
        public int uploadAttemptCount;
        public long lastUploadAttemptUnixTimestamp;

        // Serialized as Unix time (0 = not uploaded)
        public long uploadUnixTimestamp;

        public readonly DateTime? UploadedAt
        {
            get => uploadUnixTimestamp <= 0
                ? (DateTime?)null
                : DateTimeOffset.FromUnixTimeSeconds(uploadUnixTimestamp).DateTime;
        }

        public readonly DateTime? SignedUrlExpiresAt
        {
            get => signedUrlExpiresUnixTimestamp <= 0
                ? (DateTime?)null
                : DateTimeOffset.FromUnixTimeSeconds(signedUrlExpiresUnixTimestamp).DateTime;
        }

        public readonly bool HasUploadSession =>
            !string.IsNullOrEmpty(uploadToken) && !string.IsNullOrEmpty(signedUrl);

        public readonly bool IsSignedUrlExpired(int safetySeconds = 120)
        {
            if (signedUrlExpiresUnixTimestamp <= 0)
            {
                return false;
            }

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(
                signedUrlExpiresUnixTimestamp);
            return DateTimeOffset.UtcNow.AddSeconds(safetySeconds) >= expiresAt;
        }

        public void MarkUploadAttempt()
        {
            uploadAttemptCount++;
            lastUploadAttemptUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public void SetUploadSession(
            string token,
            string url,
            DateTime? expiresAt,
            string contentType)
        {
            uploadToken = token;
            signedUrl = url;
            signedUrlContentType = contentType;
            signedUrlExpiresUnixTimestamp = expiresAt.HasValue
                ? new DateTimeOffset(expiresAt.Value).ToUnixTimeSeconds()
                : 0;
        }

        public void ClearUploadSession()
        {
            uploadToken = null;
            signedUrl = null;
            signedUrlContentType = null;
            signedUrlExpiresUnixTimestamp = 0;
        }

        public void MarkUploaded(string url, string sha)
        {
            uploaded = true;
            remoteUrl = url;
            sha256 = sha;
            uploadError = null;
            uploadUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ClearUploadSession();
        }

        public void MarkUploadFailed(string error)
        {
            uploaded = false;
            uploadError = error ?? "Unknown upload error.";
            // keep previous remoteUrl / sha256 as-is (optional)
        }

        public readonly bool IsBuildOkAndZipExists()
        {
            return build.success
                   && !string.IsNullOrEmpty(build.path)
                   && System.IO.File.Exists(build.path);
        }

        public readonly bool CanUpload()
        {
            return IsBuildOkAndZipExists() && !uploaded;
        }
    }
}
