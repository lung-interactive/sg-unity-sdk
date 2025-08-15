using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SGUnitySDK.Editor.Versioning;
using SGUnitySDK.Http;
using SGUnitySDK.Editor.Http;
using UnityEditor;
using UnityEngine;

namespace SGUnitySDK.Editor
{
    public static class SGBuildUploader
    {
        private const int BUFFER_SIZE = 256 * 1024;

        /// <summary>
        /// Uploads the entry's zip to remote using presigned URL flow.
        /// Requires a valid remote semver already started in backend.
        /// Returns the updated entry (uploaded=true on success).
        /// </summary>
        public static async Task<SGVersionBuildEntry> UploadBuildZipAsync(
            SGVersionBuildEntry entry,
            string remoteSemver,
            CancellationToken ct = default
        )
        {
            if (string.IsNullOrEmpty(remoteSemver))
                throw new InvalidOperationException("RemoteSemver is not set. Start version in remote before uploading.");

            var build = entry.build;
            if (string.IsNullOrEmpty(build.path) || !File.Exists(build.path))
                throw new FileNotFoundException("Zip file not found.", build.path ?? "(null)");

            using var progress = new SGBuildProgress(
                "Uploading Build",
                $"Preparing {Path.GetFileName(build.path)}...",
                0f
            );

            try
            {
                // 1) Start upload session
                progress.Report("Starting upload session...", 0.03f);

                var startReq = GameManagementRequest.To("/start-build-upload", HttpMethod.Post);
                var body = new StartBuildUploadDTO
                {
                    Semver = remoteSemver,
                    Platform = build.GetBuildPlatform(),
                    ExecutableName = build.executableName,
                    Filename = Path.GetFileName(build.path),
                    DownloadSize = build.compression.sizeCompressed,
                    InstalledSize = build.compression.sizeUncompressed,
                    Host = FileHost.S3,
                    OverrideExisting = true,
                };
                startReq.SetBody(body);

                var startResp = await startReq.SendAsync();
                if (!startResp.Success)
                {
                    var err = startResp.ReadErrorBody();
                    throw new Exception($"Failed to start build upload: {err}");
                }

                var startData = startResp.ReadBodyData<StartBuildUploadResponseDTO>();
                string uploadToken = startData.UploadToken;
                string presignedUrl = startData.SignedUrl?.Url;

                if (string.IsNullOrEmpty(presignedUrl))
                    throw new Exception("SignedUrl was not provided by the backend.");

                // 2) (Optional) compute hash for record/confirm UX
                progress.Report("Computing SHA-256...", 0.08f);
                string sha256 = await Task.Run(() => ComputeSha256(build.path, p =>
                {
                    // simple mapping 0.08..0.35
                    progress.Report($"Computing SHA-256... {(int)(p * 100f)}%", 0.08f + 0.27f * p);
                }), ct);

                entry.sha256 = sha256; // keep it even if upload fails (debug info)

                // 3) Upload to S3 Presigned URL
                progress.Report("Uploading to storage...", 0.40f);
                bool uploadOk = await S3Uploader.UploadFileToPresignedUrl(build.path, presignedUrl);
                if (!uploadOk)
                    throw new Exception($"Failed to upload file to S3 URL.");

                progress.Report("Finalizing upload...", 0.85f);

                // 4) Confirm upload
                var confirmReq = GameManagementRequest.To("/confirm-build-upload", HttpMethod.Post)
                    .SetBody(new
                    {
                        upload_token = uploadToken,
                        semver = remoteSemver,
                        platform = build.GetBuildPlatform(),
                    });

                var confirmResp = await confirmReq.SendAsync();
                if (!confirmResp.Success)
                {
                    var errBody = confirmResp.ReadErrorBody();
                    if (errBody?.Messages != null)
                    {
                        foreach (var m in errBody.Messages)
                            Debug.LogError(m);
                    }
                    throw new Exception("Failed to confirm build upload.");
                }

                // 5) Mark entry as uploaded
                // Nota: normalmente o presigned URL não é a URL pública; deixe vazio
                entry.MarkUploaded(url: null, sha: sha256);

                progress.Report("Upload completed.", 1f);
                return entry;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                entry.MarkUploadFailed(ex.Message);
                Debug.LogError($"Upload failed: {ex.Message}");
                return entry;
            }
        }

        private static string ComputeSha256(string filePath, Action<float> onProgress)
        {
            long length = new FileInfo(filePath).Length;
            long readTotal = 0;

            using var sha = SHA256.Create();
            using var fs = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: BUFFER_SIZE,
                useAsync: false
            );

            int read;
            byte[] buffer = new byte[BUFFER_SIZE];

            while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                sha.TransformBlock(buffer, 0, read, null, 0);
                readTotal += read;
                float p = length == 0 ? 1f : (float)((double)readTotal / length);
                onProgress?.Invoke(p);
            }

            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return BitConverter.ToString(sha.Hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
