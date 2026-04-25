using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Http;
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
        private const int MAX_UPLOAD_SESSION_ATTEMPTS = 3;
        private const int MAX_START_SESSION_ATTEMPTS = 3;
        private const int MAX_STORAGE_UPLOAD_ATTEMPTS = 3;
        private const int MAX_CONFIRM_ATTEMPTS = 3;
        private const int MAX_ACK_LOOKUP_ATTEMPTS = 3;
        private const int MAX_ACK_ATTEMPTS = 3;
        private readonly IBuildUploadService _buildUploadService;
        private readonly IRemoteVersionService _remoteVersionService;
        private readonly IBuildProgressFactory _buildProgressFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadBuildUseCase"/> class.
        /// </summary>
        /// <param name="buildUploadService">Build upload service abstraction.</param>
        /// <param name="remoteVersionService">Remote version service abstraction.</param>
        /// <param name="buildProgressFactory">Progress reporter factory abstraction.</param>
        public UploadBuildUseCase(
            IBuildUploadService buildUploadService,
            IRemoteVersionService remoteVersionService,
            IBuildProgressFactory buildProgressFactory)
        {
            _buildUploadService = buildUploadService ??
                throw new ArgumentNullException(nameof(buildUploadService));
            _remoteVersionService = remoteVersionService ??
                throw new ArgumentNullException(nameof(remoteVersionService));
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
                return await RunUploadFlowAsync(
                    entry,
                    semver,
                    current.Id,
                    (message, value) => progress.Report(message, value),
                    ct);
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
                return await RunUploadFlowAsync(entry, semver, null, null, ct);
            }
            catch (Exception ex)
            {
                entry.MarkUploadFailed(ex.Message);
                throw;
            }
        }

        private async Task<SGVersionBuildEntry> RunUploadFlowAsync(
            SGVersionBuildEntry entry,
            string semver,
            string knownVersionId,
            Action<string, float> reportProgress,
            CancellationToken ct)
        {
            var build = entry.build;

            await EnsureVersionAcknowledgedForUploadAsync(semver, knownVersionId, ct);

            for (int sessionAttempt = 0; sessionAttempt < MAX_UPLOAD_SESSION_ATTEMPTS; sessionAttempt++)
            {
                ct.ThrowIfCancellationRequested();
                entry.MarkUploadAttempt();

                reportProgress?.Invoke("Starting upload session...", 0.03f);
                entry = await EnsureUploadSessionAsync(entry, semver, knownVersionId, ct);

                if (entry.IsSignedUrlExpired())
                {
                    entry.ClearUploadSession();
                    if (sessionAttempt < MAX_UPLOAD_SESSION_ATTEMPTS - 1)
                    {
                        await DelayWithBackoffAsync(sessionAttempt, ct);
                        continue;
                    }

                    throw new Exception("Signed URL expired before upload could start.");
                }

                if (string.IsNullOrEmpty(entry.sha256))
                {
                    reportProgress?.Invoke("Computing SHA-256...", 0.08f);
                    string sha256 = await Task.Run(
                        () => ComputeSha256(build.path, p =>
                        {
                            reportProgress?.Invoke(
                                $"Computing SHA-256... {(int)(p * 100f)}%",
                                0.08f + 0.27f * p);
                        }),
                        ct);
                    entry.sha256 = sha256;
                }

                reportProgress?.Invoke("Uploading to storage...", 0.40f);
                bool uploadOk = await UploadToStorageWithRetryAsync(
                    build.path,
                    entry.signedUrl,
                    entry.signedUrlContentType,
                    ct);
                if (!uploadOk)
                {
                    entry.ClearUploadSession();
                    if (sessionAttempt < MAX_UPLOAD_SESSION_ATTEMPTS - 1)
                    {
                        await DelayWithBackoffAsync(sessionAttempt, ct);
                        continue;
                    }

                    throw new Exception("Failed to upload file to signed URL.");
                }

                reportProgress?.Invoke("Finalizing upload...", 0.85f);

                var confirmPayload = new ConfirmUploadDTO
                {
                    UploadToken = entry.uploadToken,
                    Semver = semver,
                    Platform = build.GetBuildPlatform(),
                };

                try
                {
                    bool confirmed = await ConfirmUploadWithRetryAsync(confirmPayload, ct);
                    if (!confirmed)
                    {
                        throw new Exception("Failed to confirm build upload.");
                    }

                    entry.MarkUploaded(url: null, sha: entry.sha256);
                    reportProgress?.Invoke("Upload completed.", 1f);
                    return entry;
                }
                catch (RequestFailedException ex) when (ex.ResponseCode == 404 || ex.ResponseCode == 409)
                {
                    entry.MarkUploadFailed(
                        $"Confirm upload failed with status {ex.ResponseCode}. Restarting session.");
                    entry.ClearUploadSession();

                    if (sessionAttempt < MAX_UPLOAD_SESSION_ATTEMPTS - 1)
                    {
                        await DelayWithBackoffAsync(sessionAttempt, ct);
                        continue;
                    }

                    throw;
                }
            }

            throw new Exception("Build upload failed after exhausting retries.");
        }

        private async Task<SGVersionBuildEntry> EnsureUploadSessionAsync(
            SGVersionBuildEntry entry,
            string semver,
            string knownVersionId,
            CancellationToken ct)
        {
            if (entry.HasUploadSession && !entry.IsSignedUrlExpired())
            {
                return entry;
            }

            entry.ClearUploadSession();
            var startData = await StartUploadSessionWithRetryAsync(
                entry.build,
                semver,
                knownVersionId,
                ct);
            if (startData == null ||
                string.IsNullOrEmpty(startData.UploadToken) ||
                string.IsNullOrEmpty(startData.SignedUrl?.Url))
            {
                throw new Exception("Upload session data is incomplete.");
            }

            entry.SetUploadSession(
                startData.UploadToken,
                startData.SignedUrl.Url,
                startData.SignedUrl.ExpiresAt,
                startData.SignedUrl.ContentType);

            return entry;
        }

        private async Task<StartBuildUploadResponseDTO> StartUploadSessionWithRetryAsync(
            SGLocalBuildResult build,
            string semver,
            string knownVersionId,
            CancellationToken ct)
        {
            bool overrideExisting = false;
            for (int attempt = 0; attempt < MAX_START_SESSION_ATTEMPTS; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                var payload = new StartBuildUploadDTO
                {
                    Semver = semver,
                    Platform = build.GetBuildPlatform(),
                    ExecutableName = build.executableName,
                    Filename = Path.GetFileName(build.path),
                    DownloadSize = build.compression.sizeCompressed,
                    InstalledSize = build.compression.sizeUncompressed,
                    Host = FileHost.S3,
                    OverrideExisting = overrideExisting,
                };

                try
                {
                    return await _buildUploadService.StartBuildUploadAsync(payload);
                }
                catch (RequestFailedException ex) when (ex.ResponseCode == 409 && !overrideExisting)
                {
                    overrideExisting = true;
                }
                catch (RequestFailedException ex) when (IsAcknowledgeRequiredError(ex))
                {
                    await EnsureVersionAcknowledgedForUploadAsync(
                        semver,
                        knownVersionId,
                        ct);
                    if (attempt < MAX_START_SESSION_ATTEMPTS - 1)
                    {
                        await DelayWithBackoffAsync(attempt, ct);
                        continue;
                    }

                    throw;
                }
                catch (RequestFailedException ex) when (
                    IsRetryableHttpStatus(ex.ResponseCode) &&
                    attempt < MAX_START_SESSION_ATTEMPTS - 1)
                {
                    await DelayWithBackoffAsync(attempt, ct);
                }
            }

            throw new Exception("Unable to start upload session.");
        }

        private async Task<bool> UploadToStorageWithRetryAsync(
            string filePath,
            string signedUrl,
            string contentType,
            CancellationToken ct)
        {
            for (int attempt = 0; attempt < MAX_STORAGE_UPLOAD_ATTEMPTS; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                bool uploaded = await _buildUploadService.UploadFileToPresignedUrlAsync(
                    filePath,
                    signedUrl,
                    contentType);
                if (uploaded)
                {
                    return true;
                }

                if (attempt < MAX_STORAGE_UPLOAD_ATTEMPTS - 1)
                {
                    await DelayWithBackoffAsync(attempt, ct);
                }
            }

            return false;
        }

        private async Task<bool> ConfirmUploadWithRetryAsync(
            ConfirmUploadDTO payload,
            CancellationToken ct)
        {
            for (int attempt = 0; attempt < MAX_CONFIRM_ATTEMPTS; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    return await _buildUploadService.ConfirmBuildUploadAsync(payload);
                }
                catch (RequestFailedException ex) when (
                    IsRetryableHttpStatus(ex.ResponseCode) &&
                    attempt < MAX_CONFIRM_ATTEMPTS - 1)
                {
                    await DelayWithBackoffAsync(attempt, ct);
                }
            }

            return false;
        }

        private static async Task DelayWithBackoffAsync(int attempt, CancellationToken ct)
        {
            int delayMs = Math.Min(1000 * (int)Math.Pow(2, attempt), 30000);
            await Task.Delay(delayMs, ct);
        }

        private static bool IsRetryableHttpStatus(long statusCode)
        {
            return statusCode == 0 ||
                statusCode == 408 ||
                statusCode == 429 ||
                statusCode >= 500;
        }

        private static bool IsAcknowledgeRequiredError(RequestFailedException ex)
        {
            if (ex?.ErrorBody?.Messages == null)
            {
                return false;
            }

            for (int i = 0; i < ex.ErrorBody.Messages.Length; i++)
            {
                var message = ex.ErrorBody.Messages[i];
                if (string.IsNullOrEmpty(message))
                {
                    continue;
                }

                if (message.IndexOf(
                        "must be acknowledged",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<string> ResolveVersionIdForUploadAsync(
            string semver,
            string knownVersionId,
            CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(knownVersionId))
            {
                return knownVersionId;
            }

            RequestFailedException lastRetryableException = null;
            for (int attempt = 0; attempt < MAX_ACK_LOOKUP_ATTEMPTS; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var versions = await _remoteVersionService.FilterVersionsAsync(
                        new FilterVersionsDTO { SemverRaw = semver });
                    if (versions == null || versions.Length == 0)
                    {
                        return null;
                    }

                    var version = versions[0];
                    if (version == null || string.IsNullOrEmpty(version.Id))
                    {
                        return null;
                    }

                    return version.Id;
                }
                catch (RequestFailedException ex) when (
                    IsRetryableHttpStatus(ex.ResponseCode) &&
                    attempt < MAX_ACK_LOOKUP_ATTEMPTS - 1)
                {
                    lastRetryableException = ex;
                    await DelayWithBackoffAsync(attempt, ct);
                }
            }

            if (lastRetryableException != null)
            {
                throw lastRetryableException;
            }

            return null;
        }

        private async Task EnsureVersionAcknowledgedForUploadAsync(
            string semver,
            string knownVersionId,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                string versionId = await ResolveVersionIdForUploadAsync(
                    semver,
                    knownVersionId,
                    ct);
                if (string.IsNullOrEmpty(versionId))
                {
                    return;
                }

                try
                {
                    var metadata = await _remoteVersionService.GetVersionMetadataAsync(versionId);
                    if (metadata?.Acknowledgment?.Acknowledged == true)
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    // If metadata fetch fails, still try explicit acknowledgment.
                }

                for (int attempt = 0; attempt < MAX_ACK_ATTEMPTS; attempt++)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        bool acknowledged = await _remoteVersionService.AcknowledgeVersionAsync(
                            versionId,
                            "SDK pipeline started for build upload.");
                        if (!acknowledged)
                        {
                            Debug.LogWarning(
                                $"Acknowledge request returned false for version {semver}. " +
                                "Upload flow will continue.");
                        }

                        return;
                    }
                    catch (RequestFailedException ex) when (ex.ResponseCode == 409)
                    {
                        // Conflict can happen when another flow acknowledged concurrently.
                        return;
                    }
                    catch (RequestFailedException ex) when (
                        IsRetryableHttpStatus(ex.ResponseCode) &&
                        attempt < MAX_ACK_ATTEMPTS - 1)
                    {
                        await DelayWithBackoffAsync(attempt, ct);
                    }
                }

                Debug.LogWarning(
                    $"Could not pre-acknowledge version {semver} after retries. " +
                    "Upload flow will continue.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"Could not pre-acknowledge version {semver} before upload. " +
                    $"Upload flow will continue: {ex.Message}");
            }
        }
    }
}
