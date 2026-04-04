using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using UnityEditor;
using UnityEngine;
using SGUnitySDK.Editor.Core.Singletons;
using SGUnitySDK.Editor.Core.Utils;

namespace SGUnitySDK.Editor.Core.UseCases
{
    /// <summary>
    /// Use case responsible for uploading a single build artifact using the
    /// presigned URL flow. Provides both a UI-friendly `ExecuteAsync` that
    /// reports progress, and a background-safe `ExecuteBackgroundAsync`
    /// suitable for parallel execution.
    /// </summary>
    public sealed class UploadBuildUseCase
    {
        private const int BUFFER_SIZE = 256 * 1024;
        private readonly IBuildUploadService _buildUploadService;
        private readonly IBuildProgressFactory _buildProgressFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadBuildUseCase"/> class.
        /// </summary>
        /// <param name="buildUploadService">Build upload service abstraction.</param>
        /// <param name="buildProgressFactory">Progress reporter factory abstraction.</param>
        public UploadBuildUseCase(
            IBuildUploadService buildUploadService,
            IBuildProgressFactory buildProgressFactory)
        {
            _buildUploadService = buildUploadService ??
                throw new ArgumentNullException(nameof(buildUploadService));
            _buildProgressFactory = buildProgressFactory ??
                throw new ArgumentNullException(nameof(buildProgressFactory));
        }

        public async Task<SGVersionBuildEntry> ExecuteAsync(SGVersionBuildEntry entry, CancellationToken ct = default)
        {
            if (DevelopmentProcess.instance.CurrentStep != DevelopmentStep.Development)
                throw new InvalidOperationException("Current development step is not 'Development'. Aborting upload.");

            var current = DevelopmentProcess.instance.CurrentVersion;
            if (current == null || current.Semver == null)
                throw new InvalidOperationException("No current development version available in DevelopmentProcess.");

            var semver = current.Semver.Raw;

            var build = entry.build;
            if (string.IsNullOrEmpty(build.path) || !File.Exists(build.path))
                throw new FileNotFoundException("Zip file not found.", build.path ?? "(null)");

            using var progress = _buildProgressFactory.Create(
                "Uploading Build",
                $"Preparing {Path.GetFileName(build.path)}...",
                0f
            );

            try
            {
                progress.Report("Starting upload session...", 0.03f);

                var body = new StartBuildUploadDTO
                {
                    Semver = semver,
                    Platform = build.GetBuildPlatform(),
                    ExecutableName = build.executableName,
                    Filename = Path.GetFileName(build.path),
                    DownloadSize = build.compression.sizeCompressed,
                    InstalledSize = build.compression.sizeUncompressed,
                    Host = FileHost.S3,
                    OverrideExisting = true,
                };

                var startData = await _buildUploadService.StartBuildUploadAsync(body);
                string uploadToken = startData.UploadToken;
                string presignedUrl = startData.SignedUrl?.Url;

                if (string.IsNullOrEmpty(presignedUrl))
                    throw new Exception("SignedUrl was not provided by the backend.");

                progress.Report("Computing SHA-256...", 0.08f);
                string sha256 = await Task.Run(() => ComputeSha256(build.path, p =>
                {
                    progress.Report($"Computing SHA-256... {(int)(p * 100f)}%", 0.08f + 0.27f * p);
                }), ct);

                entry.sha256 = sha256;

                progress.Report("Uploading to storage...", 0.40f);
                bool uploadOk = await _buildUploadService.UploadFileToPresignedUrlAsync(build.path, presignedUrl);
                if (!uploadOk)
                    throw new Exception("Failed to upload file to S3 URL.");

                progress.Report("Finalizing upload...", 0.85f);

                var confirmPayload = new ConfirmUploadDTO
                {
                    UploadToken = uploadToken,
                    Semver = semver,
                    Platform = build.GetBuildPlatform(),
                };

                bool confirmed = await _buildUploadService.ConfirmBuildUploadAsync(confirmPayload);
                if (!confirmed)
                    throw new Exception("Failed to confirm build upload.");

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

        public Awaitable<SGVersionBuildEntry> ExecuteAwaitable(SGVersionBuildEntry entry, CancellationToken ct = default)
        {
            var task = ExecuteAsync(entry, ct);
            return TaskAwaitableAdapter.FromTask(task);
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

        /// <summary>
        /// Background-safe variant suitable for parallel execution. Does not
        /// interact with Unity APIs and can be executed on thread-pool
        /// threads.
        /// </summary>
        public async Task<SGVersionBuildEntry> ExecuteBackgroundAsync(SGVersionBuildEntry entry, string semver, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(semver))
                throw new InvalidOperationException("Semver must be provided for background upload.");

            var build = entry.build;
            if (string.IsNullOrEmpty(build.path) || !File.Exists(build.path))
                throw new FileNotFoundException("Zip file not found.", build.path ?? "(null)");

            try
            {
                var body = new StartBuildUploadDTO
                {
                    Semver = semver,
                    Platform = build.GetBuildPlatform(),
                    ExecutableName = build.executableName,
                    Filename = Path.GetFileName(build.path),
                    DownloadSize = build.compression.sizeCompressed,
                    InstalledSize = build.compression.sizeUncompressed,
                    Host = FileHost.S3,
                    OverrideExisting = true,
                };

                var startData = await _buildUploadService.StartBuildUploadAsync(body);
                string uploadToken = startData.UploadToken;
                string presignedUrl = startData.SignedUrl?.Url;

                if (string.IsNullOrEmpty(presignedUrl))
                    throw new Exception("SignedUrl was not provided by the backend.");

                string sha256 = await Task.Run(() => ComputeSha256(build.path, _ => { }), ct);
                entry.sha256 = sha256;

                bool uploadOk = await _buildUploadService.UploadFileToPresignedUrlAsync(build.path, presignedUrl);
                if (!uploadOk)
                    throw new Exception("Failed to upload file to S3 URL.");

                var confirmPayload = new ConfirmUploadDTO
                {
                    UploadToken = uploadToken,
                    Semver = semver,
                    Platform = build.GetBuildPlatform(),
                };

                bool confirmed = await _buildUploadService.ConfirmBuildUploadAsync(confirmPayload);
                if (!confirmed)
                {
                    throw new Exception("Failed to confirm build upload.");
                }

                entry.MarkUploaded(url: null, sha: sha256);
                return entry;
            }
            catch (Exception)
            {
                entry.MarkUploadFailed("Background upload failed.");
                throw;
            }
        }
    }
}
