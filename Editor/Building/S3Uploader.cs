using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using System.Net.Http;
using System.Net.Http.Headers;
using SGUnitySDK.Editor.Core.Utils;

namespace SGUnitySDK.Editor
{
    public static class S3Uploader
    {
        public static async Task<bool> UploadFileToPresignedUrl(
            string filePath,
            string presignedUrl,
            string contentType = null,
            int maxAttempts = 3)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File not found: {filePath}");
                    return false;
                }

                if (maxAttempts < 1)
                {
                    maxAttempts = 1;
                }

                using (var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) })
                {
                    for (int attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        using (var fileStream = new FileStream(
                            filePath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read,
                            bufferSize: 81920,
                            useAsync: true))
                        using (var content = new StreamContent(fileStream))
                        {
                            string resolvedContentType = string.IsNullOrEmpty(contentType)
                                ? GetContentType(Path.GetExtension(filePath))
                                : contentType;
                            content.Headers.ContentType = new MediaTypeHeaderValue(
                                resolvedContentType);

                            using (var response = await httpClient.PutAsync(
                                presignedUrl,
                                content))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    Debug.Log(
                                        $"Successfully uploaded {Path.GetFileName(filePath)} to storage");
                                    return true;
                                }

                                string responseContent = await response.Content.ReadAsStringAsync();
                                Debug.LogError(
                                    $"Upload failed (attempt {attempt + 1}/{maxAttempts}). " +
                                    $"Status: {response.StatusCode}, Response: {responseContent}");

                                bool canRetry = attempt < maxAttempts - 1 &&
                                    IsRetryableStatusCode(response.StatusCode);
                                if (!canRetry)
                                {
                                    return false;
                                }
                            }

                            int delayMs = CalculateBackoffDelay(attempt);
                            await Task.Delay(delayMs);
                        }

                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during upload: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Awaitable wrapper for <see cref="UploadFileToPresignedUrl"/> so Awaitable-based
        /// callers can await uploads without changing existing Task-based implementation.
        /// </summary>
        public static Awaitable<bool> UploadFileToPresignedUrlAwaitable(string filePath, string presignedUrl)
        {
            return TaskAwaitableAdapter.FromTask(UploadFileToPresignedUrl(filePath, presignedUrl));
        }

        private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
        {
            int code = (int)statusCode;
            return code == 408 || code == 429 || code >= 500;
        }

        private static string GetContentType(string fileExtension)
        {
            switch (fileExtension.ToLower())
            {
                case ".zip":
                    return "application/zip";
                case ".exe":
                    return "application/x-msdownload";
                case ".dmg":
                    return "application/x-apple-diskimage";
                case ".pkg":
                    return "application/x-newton-compatible-pkg";
                case ".apk":
                    return "application/vnd.android.package-archive";
                default:
                    return "application/octet-stream";
            }
        }

        private static int CalculateBackoffDelay(int attempt)
        {
            return Math.Min(1000 * (int)Math.Pow(2, attempt), 30000);
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double number = bytes;

            while (number >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                number /= 1024;
                suffixIndex++;
            }

            return $"{number:0.##} {suffixes[suffixIndex]}";
        }
    }
}