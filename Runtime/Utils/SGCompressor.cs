using System;
using System.Collections.Generic;
using System.IO;
using IOCompression = System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace SGUnitySDK.Utils
{

    public static class SGCompressor
    {
        /// <summary>
        /// Compresses files with platform-specific settings
        /// </summary>
        public static async Awaitable<CompressingResult> ZipAllFiles(
            string sourceDirectory,
            string outputZipPath = null,
            List<string> exclusionFilters = null,
            IOCompression.CompressionLevel compressionLevel = IOCompression.CompressionLevel.Optimal,
            CompressionPlatform platform = CompressionPlatform.Windows
        )
        {
            ValidateInputDirectory(sourceDirectory);

            string zipFilePath = GetOutputZipPath(sourceDirectory, outputZipPath);
            PrepareOutputDirectory(zipFilePath);

            try
            {
                var files = GetFilesToCompress(sourceDirectory, exclusionFilters);
                ValidateFileList(files, sourceDirectory);

                ulong uncompressedSize = CalculateUncompressedSize(files);
                int fileCount = files.Count;

                await CreateZipArchive(
                    files,
                    sourceDirectory,
                    zipFilePath,
                    compressionLevel,
                    platform);

                ValidateZipArchive(zipFilePath, fileCount);
                ulong compressedSize = (ulong)new FileInfo(zipFilePath).Length;

                return CreateSuccessResult(
                    zipFilePath,
                    compressedSize,
                    uncompressedSize,
                    fileCount,
                    platform
                );
            }
            catch (Exception ex)
            {
                CleanUpFailedOperation(zipFilePath);
                Debug.LogError($"Failed to create zip file: {ex.Message}");
                throw;
            }
        }

        #region Platform-Specific Implementation

        private static async Task CreateZipArchive(
            List<string> files,
            string sourceDirectory,
            string zipFilePath,
            IOCompression.CompressionLevel compressionLevel,
            CompressionPlatform platform)
        {
            using var zipStream = new FileStream(
                zipFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            using var archive = new IOCompression.ZipArchive(
                zipStream,
                IOCompression.ZipArchiveMode.Create);

            var targetPlatform = platform;

            foreach (string file in files)
            {
                await AddFileToArchive(
                    file,
                    sourceDirectory,
                    archive,
                    compressionLevel,
                    targetPlatform);
            }
        }

        private static async Task AddFileToArchive(
            string filePath,
            string baseDirectory,
            IOCompression.ZipArchive archive,
            IOCompression.CompressionLevel compressionLevel,
            CompressionPlatform platform)
        {
            if (Directory.Exists(filePath)) return;

            try
            {
                string relativePath = GetPlatformSpecificPath(
                    filePath,
                    baseDirectory,
                    platform);

                var entry = archive.CreateEntry(
                    relativePath,
                    compressionLevel);

                await using var fileStream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                await using var entryStream = entry.Open();
                await fileStream.CopyToAsync(entryStream);

                if (platform == CompressionPlatform.Linux)
                {
                    AdjustLinuxFileAttributes(entry);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add file {filePath}: {ex.Message}");
                throw;
            }
        }

        private static string GetPlatformSpecificPath(
            string filePath,
            string baseDirectory,
            CompressionPlatform platform)
        {
            string relativePath = filePath
                .Substring(baseDirectory.Length)
                .TrimStart(Path.DirectorySeparatorChar);

            return platform switch
            {
                CompressionPlatform.Windows => relativePath.Replace('/', '\\'),
                CompressionPlatform.Linux => relativePath.Replace('\\', '/'),
                _ => relativePath
            };
        }

        private static void AdjustLinuxFileAttributes(
            IOCompression.ZipArchiveEntry entry)
        {
            if (ShouldBeExecutable(entry.Name))
            {
                const int LinuxExecutableMode = 0b_001_001_001; // 0o111
                entry.ExternalAttributes =
                    (LinuxExecutableMode << 16) | entry.ExternalAttributes;
            }
        }

        private static bool ShouldBeExecutable(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return string.IsNullOrEmpty(extension) ||
                   extension == ".sh" ||
                   extension == ".bin" ||
                   extension == ".run";
        }

        #endregion

        #region Common Helper Methods

        private static void ValidateInputDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogError($"Directory does not exist: {directoryPath}");
                throw new DirectoryNotFoundException($"Directory does not exist: {directoryPath}");
            }
        }

        private static string GetOutputZipPath(
            string sourceDirectory,
            string outputZipPath)
        {
            return outputZipPath ??
                $"{sourceDirectory.TrimEnd(Path.DirectorySeparatorChar)}.zip";
        }

        private static void PrepareOutputDirectory(string zipFilePath)
        {
            string directory = Path.GetDirectoryName(zipFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }
        }

        private static List<string> GetFilesToCompress(
            string sourceDirectory,
            List<string> exclusionFilters)
        {
            exclusionFilters ??= new List<string> { "DoNotShip" };

            return Directory
                .GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
                .Where(file => !exclusionFilters.Any(filter =>
                    file.Split(Path.DirectorySeparatorChar)
                        .Contains(filter, StringComparer.OrdinalIgnoreCase)))
                .ToList();
        }

        private static void ValidateFileList(
            List<string> files,
            string sourceDirectory)
        {
            if (files.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No files found in directory: {sourceDirectory}");
            }
        }

        private static ulong CalculateUncompressedSize(List<string> files)
        {
            return (ulong)files.Sum(file => new FileInfo(file).Length);
        }

        private static void ValidateZipArchive(
            string zipFilePath,
            int expectedFileCount)
        {
            if (!File.Exists(zipFilePath))
            {
                throw new InvalidOperationException("Zip file creation failed");
            }

            using var archive = IOCompression.ZipFile.OpenRead(zipFilePath);
            if (archive.Entries.Count != expectedFileCount)
            {
                throw new InvalidOperationException(
                    $"Expected {expectedFileCount} entries, got {archive.Entries.Count}");
            }
        }

        private static CompressingResult CreateSuccessResult(
            string outputPath,
            ulong compressedSize,
            ulong uncompressedSize,
            int fileCount,
            CompressionPlatform platform)
        {
            return new CompressingResult
            {
                output = outputPath,
                sizeCompressed = compressedSize,
                sizeUncompressed = uncompressedSize,
                fileCount = fileCount,
                platform = platform
            };
        }

        private static void CleanUpFailedOperation(string zipFilePath)
        {
            if (File.Exists(zipFilePath))
            {
                try { File.Delete(zipFilePath); } catch { }
            }
        }

        #endregion
    }
}