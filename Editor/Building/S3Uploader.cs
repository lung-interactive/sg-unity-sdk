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