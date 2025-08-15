using System;
using System.Collections.Generic;
using System.IO;
using IOCompression = System.IO.Compression;
using System.Linq;
using UnityEngine;

namespace SGUnitySDK.Utils
{
    /// <summary>
    /// Progress payload for zip operations.
    /// </summary>
    public sealed class ZipProgress
    {
        /// <summary>0..1 for total bytes progress.</summary>
        public float TotalProgress { get; set; }

        /// <summary>Processed files count.</summary>
        public int FilesDone { get; set; }

        /// <summary>Total files count.</summary>
        public int FilesTotal { get; set; }

        /// <summary>Currently processed relative path.</summary>
        public string CurrentPath { get; set; } = string.Empty;

        /// <summary>Total bytes processed so far.</summary>
        public ulong BytesDone { get; set; }

        /// <summary>Total bytes to process.</summary>
        public ulong BytesTotal { get; set; }
    }

    public static class SGCompressor
    {
        /// <summary>
        /// Synchronous zipping on the caller thread. No async/await, no Tasks.
        /// Reports progress via onProgress callback (same thread).
        /// </summary>
        public static CompressingResult ZipAllFilesSync(
            string sourceDirectory,
            string outputZipPath = null,
            List<string> exclusionFilters = null,
            IOCompression.CompressionLevel compressionLevel =
                IOCompression.CompressionLevel.Optimal,
            CompressionPlatform platform = CompressionPlatform.Windows,
            Action<ZipProgress> onProgress = null,
            bool setLinuxExecBit = true
        )
        {
            ValidateInputDirectory(sourceDirectory);

            string zipFilePath = GetOutputZipPath(sourceDirectory, outputZipPath);
            PrepareOutputDirectory(zipFilePath);

            try
            {
                var files = GetFilesToCompress(sourceDirectory, exclusionFilters);
                ValidateFileList(files, sourceDirectory);

                ulong totalBytes = CalculateUncompressedSize(files);
                int filesTotal = files.Count;

                CreateZipArchiveSync(
                    files,
                    sourceDirectory,
                    zipFilePath,
                    compressionLevel,
                    platform,
                    totalBytes,
                    filesTotal,
                    onProgress,
                    setLinuxExecBit
                );

                ValidateZipArchive(zipFilePath, filesTotal);

                ulong compressedSize = (ulong)new FileInfo(zipFilePath).Length;

                return CreateSuccessResult(
                    zipFilePath,
                    compressedSize,
                    totalBytes,
                    filesTotal,
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

        #region Core (sync)

        private static void CreateZipArchiveSync(
            List<string> files,
            string sourceDirectory,
            string zipFilePath,
            IOCompression.CompressionLevel compressionLevel,
            CompressionPlatform platform,
            ulong totalBytes,
            int filesTotal,
            Action<ZipProgress> onProgress,
            bool setLinuxExecBit
        )
        {
            using var zipStream = new FileStream(
                zipFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );

            using var archive = new IOCompression.ZipArchive(
                zipStream,
                IOCompression.ZipArchiveMode.Create
            );

            int filesDone = 0;
            ulong bytesDone = 0;

            foreach (var file in files)
            {
                AddFileToArchiveSync(
                    file,
                    sourceDirectory,
                    archive,
                    compressionLevel,
                    platform,
                    (addedBytes, relPath) =>
                    {
                        bytesDone += addedBytes;
                        onProgress?.Invoke(new ZipProgress
                        {
                            TotalProgress = totalBytes == 0
                                ? 1f
                                : (float)((double)bytesDone / totalBytes),
                            FilesDone = filesDone,
                            FilesTotal = filesTotal,
                            CurrentPath = relPath,
                            BytesDone = bytesDone,
                            BytesTotal = totalBytes
                        });
                    },
                    setLinuxExecBit
                );

                filesDone++;

                onProgress?.Invoke(new ZipProgress
                {
                    TotalProgress = totalBytes == 0
                        ? 1f
                        : (float)((double)bytesDone / totalBytes),
                    FilesDone = filesDone,
                    FilesTotal = filesTotal,
                    CurrentPath = string.Empty,
                    BytesDone = bytesDone,
                    BytesTotal = totalBytes
                });
            }
        }

        private static void AddFileToArchiveSync(
            string filePath,
            string baseDirectory,
            IOCompression.ZipArchive archive,
            IOCompression.CompressionLevel compressionLevel,
            CompressionPlatform platform,
            Action<ulong, string> onBytesAppended,
            bool setLinuxExecBit
        )
        {
            if (Directory.Exists(filePath)) return;

            try
            {
                string relativePath = GetPlatformSpecificPath(
                    filePath,
                    baseDirectory,
                    platform
                );

                // Long path support on Windows Editor
                string openPath = filePath;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                if (openPath.Length >= 240 && !openPath.StartsWith(@"\\?\"))
                {
                    openPath = @"\\?\" + Path.GetFullPath(openPath);
                }
#endif

                IOCompression.CompressionLevel levelForFile =
                    ShouldStoreUncompressed(relativePath)
                        ? IOCompression.CompressionLevel.NoCompression
                        : compressionLevel;

                var entry = archive.CreateEntry(relativePath, levelForFile);

                const int BufferSize = 128 * 1024;
                byte[] buffer = new byte[BufferSize];

                using var fileStream = new FileStream(
                    openPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );

                using var entryStream = entry.Open();

                int read;
                while ((read = fileStream.Read(buffer, 0, BufferSize)) > 0)
                {
                    entryStream.Write(buffer, 0, read);
                    onBytesAppended?.Invoke((ulong)read, relativePath);
                }

                entryStream.Flush();

                if (setLinuxExecBit && platform == CompressionPlatform.Linux)
                {
                    TryAdjustLinuxFileAttributes(entry);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add file {filePath}: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Platform helpers

        private static string GetPlatformSpecificPath(
            string filePath,
            string baseDirectory,
            CompressionPlatform platform
        )
        {
            string relativePath = Path.GetRelativePath(baseDirectory, filePath);

            return platform switch
            {
                CompressionPlatform.Windows => relativePath.Replace('/', '\\'),
                CompressionPlatform.Linux => relativePath.Replace('\\', '/'),
                _ => relativePath
            };
        }

        private static void TryAdjustLinuxFileAttributes(
            IOCompression.ZipArchiveEntry entry
        )
        {
            try
            {
                if (!ShouldBeExecutable(entry.Name)) return;

                const int LinuxExecutableMode = 0b_001_001_001; // 0o111
                int current = entry.ExternalAttributes;
                entry.ExternalAttributes =
                    (LinuxExecutableMode << 16) | (current & 0xFFFF);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"Exec-bit set skipped for '{entry.FullName}': {ex.Message}"
                );
            }
        }

        private static bool ShouldBeExecutable(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return string.IsNullOrEmpty(extension)
                   || extension == ".sh"
                   || extension == ".bin"
                   || extension == ".run";
        }

        private static bool ShouldStoreUncompressed(string relativePath)
        {
            string ext = Path.GetExtension(relativePath).ToLowerInvariant();
            switch (ext)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".mp3":
                case ".ogg":
                case ".zip":
                case ".7z":
                case ".rar":
                case ".dll":
                case ".so":
                case ".a":
                case ".bundle":
                case ".pak":
                case ".data":
                case ".ress":
                case ".mp4":
                case ".webm":
                case ".bin":
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Common helpers

        private static void ValidateInputDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogError($"Directory does not exist: {directoryPath}");
                throw new DirectoryNotFoundException(
                    $"Directory does not exist: {directoryPath}"
                );
            }
        }

        private static string GetOutputZipPath(
            string sourceDirectory,
            string outputZipPath
        )
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
            List<string> exclusionFilters
        )
        {
            Debug.Log($"Compressing directory: {sourceDirectory}");

            var filters = exclusionFilters is { Count: > 0 }
                ? new HashSet<string>(exclusionFilters,
                    StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(new[] { "DoNotShip" },
                    StringComparer.OrdinalIgnoreCase);

            var allFiles = Directory.EnumerateFiles(
                sourceDirectory,
                "*",
                SearchOption.AllDirectories
            );

            var kept = new List<string>();

            foreach (var fullPath in allFiles)
            {
                var relative = Path.GetRelativePath(sourceDirectory, fullPath);

                var segments = relative.Split(
                    new[]
                    {
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar
                    },
                    StringSplitOptions.RemoveEmptyEntries
                );

                bool excluded = segments.Any(seg => filters.Contains(seg));
                if (!excluded) kept.Add(fullPath);
            }

            Debug.Log(
                $"Files found: {kept.Count} " +
                $"(excluded: {allFiles.Count() - kept.Count})."
            );

            return kept;
        }

        private static void ValidateFileList(
            List<string> files,
            string sourceDirectory
        )
        {
            if (files.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No files found in directory: {sourceDirectory}"
                );
            }
        }

        private static ulong CalculateUncompressedSize(List<string> files)
        {
            return (ulong)files.Sum(f => new FileInfo(f).Length);
        }

        private static void ValidateZipArchive(
            string zipFilePath,
            int expectedFileCount
        )
        {
            if (!File.Exists(zipFilePath))
            {
                throw new InvalidOperationException(
                    "Zip file creation failed"
                );
            }

            using var archive = IOCompression.ZipFile.OpenRead(zipFilePath);
            if (archive.Entries.Count != expectedFileCount)
            {
                throw new InvalidOperationException(
                    $"Expected {expectedFileCount} entries, " +
                    $"got {archive.Entries.Count}"
                );
            }
        }

        private static CompressingResult CreateSuccessResult(
            string outputPath,
            ulong compressedSize,
            ulong uncompressedSize,
            int fileCount,
            CompressionPlatform platform
        )
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
