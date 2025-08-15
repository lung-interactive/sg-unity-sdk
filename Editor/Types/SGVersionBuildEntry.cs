using System;
using UnityEditor;

namespace SGUnitySDK.Editor
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

        // Serialized as Unix time (0 = not uploaded)
        public long uploadUnixTimestamp;

        public readonly DateTime? UploadedAt
        {
            get => uploadUnixTimestamp <= 0
                ? (DateTime?)null
                : DateTimeOffset.FromUnixTimeSeconds(uploadUnixTimestamp).DateTime;
        }

        public void MarkUploaded(string url, string sha)
        {
            uploaded = true;
            remoteUrl = url;
            sha256 = sha;
            uploadError = null;
            uploadUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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
