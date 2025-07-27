using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Net.Http;

namespace SGUnitySDK.Editor
{
    public static class S3Uploader
    {
        public static async Task<bool> UploadFileAsync(
            string filePath,
            string bucketName,
            string region,
            string accessKey,
            string secretKey,
            int maxRetries = 3)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found: {filePath}");
                return false;
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = 100;

            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(region),
                Timeout = TimeSpan.FromMinutes(15),
                BufferSize = 81920,
                MaxErrorRetry = 3
            };

            using (var client = new AmazonS3Client(accessKey, secretKey, config))
            {
                var transferUtility = new TransferUtility(client);
                var request = new TransferUtilityUploadRequest
                {
                    BucketName = bucketName,
                    Key = $"game-builds/{Path.GetFileName(filePath)}",
                    FilePath = filePath,
                    // Removido CannedACL pois o bucket não permite ACLs
                    PartSize = 50 * 1024 * 1024 // 50MB part size for multipart upload
                };

                // Configuração do evento de progresso
                request.UploadProgressEvent += (sender, e) =>
                {
                    float progress = (float)e.TransferredBytes / e.TotalBytes;
                    EditorUtility.DisplayProgressBar(
                        "Uploading to S3",
                        $"{Path.GetFileName(filePath)} - {FormatBytes(e.TransferredBytes)}/{FormatBytes(e.TotalBytes)}",
                        progress);
                };

                int attempt = 0;
                while (attempt < maxRetries)
                {
                    attempt++;
                    try
                    {
                        await transferUtility.UploadAsync(request);
                        Debug.Log($"Upload successful to s3://{bucketName}/game-builds/{Path.GetFileName(filePath)}");
                        return true;
                    }
                    catch (AmazonS3Exception e) when (e.ErrorCode == "AccessControlListNotSupported")
                    {
                        // Tentar novamente sem ACLs
                        Debug.LogWarning("Bucket doesn't allow ACLs. Retrying without ACL...");
                        request.CannedACL = null;
                        continue;
                    }
                    catch (AmazonS3Exception e) when (e.ErrorCode == "RequestTimeout")
                    {
                        Debug.LogWarning($"Attempt {attempt} failed (Timeout): {e.Message}");
                        if (attempt >= maxRetries) break;
                        await Task.Delay(CalculateBackoffDelay(attempt));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Attempt {attempt} failed: {e.GetType().Name}\n{e.Message}");
                        if (attempt >= maxRetries) break;
                        await Task.Delay(CalculateBackoffDelay(attempt));
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            Debug.LogError($"All {maxRetries} upload attempts failed for {filePath}");
            return false;
        }

        public static async Task<bool> UploadFileToPresignedUrl(string filePath, string presignedUrl)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File not found: {filePath}");
                    return false;
                }

                byte[] fileBytes;
                try
                {
                    fileBytes = File.ReadAllBytes(filePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to read file {filePath}: {ex.Message}");
                    return false;
                }

                using (var httpClient = new HttpClient())
                using (var content = new ByteArrayContent(fileBytes))
                {
                    // Configura o content type apropriado para o arquivo
                    string contentType = GetContentType(Path.GetExtension(filePath));
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

                    // Configura o método PUT (que é o padrão para uploads S3 com URL pré-assinada)
                    using (var response = await httpClient.PutAsync(presignedUrl, content))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            Debug.LogError($"Upload failed. Status: {response.StatusCode}, Response: {responseContent}");
                            return false;
                        }

                        Debug.Log($"Successfully uploaded {Path.GetFileName(filePath)} to S3");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during upload: {ex.Message}");
                return false;
            }
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