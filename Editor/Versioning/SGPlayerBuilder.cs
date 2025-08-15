using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SGUnitySDK.Utils;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SGUnitySDK.Editor.Versioning
{
    public static class SGPlayerBuilder
    {
        private static readonly List<string> DefaultExclusionFilters = new()
        {
            "DoNotShip",
            "BackUp",
            "Temp",
            "~",
            ".tmp",
            ".bak",
            ".git",
            ".svn",
            ".vs",
            ".idea",
            "Logs",
            "Obj",
            "Library",
            "ProjectSettings~",
            "csc.rsp",
            "mcs.rsp",
            "gmcs.rsp",
            "smcs.rsp",
            "Thumbs.db",
            ".DS_Store"
        };

        /// <summary>
        /// Perform multiple builds synchronously on the main thread.
        /// </summary>
        public static List<SGLocalBuildResult> PerformMultipleBuilds(
            List<SGBuildSetup> setups,
            string commonBuildPath,
            string targetVersion
        )
        {
            var buildResults = new List<SGLocalBuildResult>();

            try
            {
                int totalSetups = setups.Count;

                if (Directory.Exists(commonBuildPath))
                {
                    Directory.Delete(commonBuildPath, true);
                }
                Directory.CreateDirectory(commonBuildPath);

                for (int i = 0; i < setups.Count; i++)
                {
                    var setup = setups[i];

                    try
                    {
                        var result = BuildAndZipSetup(
                            setup,
                            commonBuildPath,
                            targetVersion,
                            i,
                            totalSetups
                        );

                        buildResults.Add(result);
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.ClearProgressBar();

                        buildResults.Add(new SGLocalBuildResult
                        {
                            success = false,
                            productName = setup.profile.name,
                            errorMessage = ex.Message
                        });

                        Debug.LogError(
                            $"Error building profile " +
                            $"{setup.profile.name}: {ex.Message}"
                        );
                    }
                }

                int successful = buildResults.Count(r => r.success);

                Debug.Log(
                    $"Successfully built {successful} out of " +
                    $"{setups.Count} profiles."
                );
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"Build process failed: {ex.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return buildResults;
        }

        /// <summary>
        /// Build one setup and zip its output synchronously.
        /// </summary>
        public static SGLocalBuildResult BuildAndZipSetup(
            SGBuildSetup setup,
            string commonBuildPath,
            string targetVersion,
            int completed,
            int totalSetups
        )
        {
            var result = new SGLocalBuildResult
            {
                productName = Application.productName,
                platform = setup.profile.GetBuildTargetInternal(),
                BuiltAt = DateTime.Now
            };

            try
            {
                // ---------------- Build phase ----------------
                string buildMsg = $"Building {setup.profile.name}...";
                float buildPct = totalSetups > 0
                    ? Mathf.Clamp01((float)completed / totalSetups)
                    : 0f;

                EditorUtility.DisplayProgressBar(
                    "Building Player",
                    buildMsg,
                    buildPct
                );

                string buildFolderName = GetBuildFolderName(
                    setup.profile,
                    targetVersion
                );

                string buildPath = Path.Combine(
                    commonBuildPath,
                    buildFolderName
                );

                Directory.CreateDirectory(buildPath);

                string executableName = GetExecutableName(
                    setup.profile,
                    targetVersion
                );

                string buildCompletePath = Path.Combine(
                    buildPath,
                    executableName
                );

                var summary = BuildUsingProfile(
                    setup.profile,
                    buildCompletePath
                );

                if (summary.result != BuildResult.Succeeded)
                {
                    EditorUtility.ClearProgressBar();

                    result.success = false;
                    result.errorMessage =
                        $"Build failed for profile " +
                        $"{setup.profile.name}";
                    Debug.LogError(result.errorMessage);

                    return result;
                }

                result.path = buildPath;

                // ---------------- Zip phase (SYNC) -----------
                EditorUtility.ClearProgressBar();

                string zipFileName =
                    $"{GetBaseName(setup.profile, targetVersion)}." +
                    $"{GetPlatformName(setup.profile.GetBuildTargetInternal())}.zip";

                string zipFilePath = Path.Combine(
                    commonBuildPath,
                    zipFileName
                );

                int lastFilesDone = 0;
                ulong lastBytes = 0;

                bool setExecBit =
                    setup.profile.GetBuildTargetInternal()
                    == BuildTarget.StandaloneLinux64;

                var compression = SGCompressor.ZipAllFilesSync(
                    buildPath,
                    zipFilePath,
                    DefaultExclusionFilters,
                    System.IO.Compression.CompressionLevel.Optimal,
                    ResolveCompressionPlatform(
                        setup.profile.GetBuildTargetInternal()
                    ),
                    onProgress: p =>
                    {
                        string msg =
                            $"Zipping {setup.profile.name} " +
                            $"({p.FilesDone}/{p.FilesTotal})\n" +
                            $"{p.CurrentPath}";

                        EditorUtility.DisplayProgressBar(
                            "Compressing Files",
                            msg,
                            Mathf.Clamp01(p.TotalProgress)
                        );

                        // Optional lightweight log throttle
                        if (p.FilesDone != lastFilesDone ||
                            p.BytesDone - lastBytes > (1UL << 22))
                        {
                            lastFilesDone = p.FilesDone;
                            lastBytes = p.BytesDone;
                            // Debug.Log($"[ZIP] {p.FilesDone}/{p.FilesTotal}");
                        }
                    },
                    setLinuxExecBit: setExecBit
                );

                result.success = true;
                result.executableName = executableName;
                result.path = zipFilePath;
                result.compression = compression;

                return result;
            }
            catch (Exception ex)
            {
                result.success = false;
                result.errorMessage = ex.Message;
                return result;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Use Unity Build Pipeline with the given profile.
        /// </summary>
        private static BuildSummary BuildUsingProfile(
            BuildProfile profile,
            string outputPath,
            BuildOptions buildOptions = BuildOptions.None
        )
        {
            var dir = Path.GetDirectoryName(outputPath);
            Directory.CreateDirectory(dir);
            BuildProfile.SetActiveBuildProfile(profile);

            var options = new BuildPlayerWithProfileOptions
            {
                locationPathName = outputPath,
                options = buildOptions,
                buildProfile = profile
            };

            var report = BuildPipeline.BuildPlayer(options);
            return report.summary;
        }

        private static string GetBaseName(
            BuildProfile profile,
            string version
        )
        {
            string product = SanitizeFileName(Application.productName);
            string ver = SanitizeVersion(version);
            return $"{product}.v{ver}";
        }

        private static string GetBuildFolderName(
            BuildProfile profile,
            string version
        )
        {
            return $"{GetBaseName(profile, version)}." +
                   $"{GetPlatformName(profile.GetBuildTargetInternal())}";
        }

        private static string GetExecutableName(
            BuildProfile profile,
            string version
        )
        {
            string baseName = GetBaseName(profile, version);
            var platform = profile.GetBuildTargetInternal();

            return platform switch
            {
                BuildTarget.StandaloneWindows
                or BuildTarget.StandaloneWindows64
                    => $"{baseName}.exe",
                BuildTarget.StandaloneLinux64
                    => $"{baseName}.x86_64",
                BuildTarget.StandaloneOSX
                    => $"{baseName}.app",
                _ => $"{baseName}.exe"
            };
        }

        private static string GetPlatformName(BuildTarget target)
        {
            return target switch
            {
                BuildTarget.StandaloneWindows
                or BuildTarget.StandaloneWindows64 => "windows",
                BuildTarget.StandaloneLinux64 => "linux",
                BuildTarget.StandaloneOSX => "macos",
                _ => "windows"
            };
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Concat(name.Split(invalid));
        }

        private static string SanitizeVersion(string version)
        {
            return version.Replace('.', '_');
        }

        private static CompressionPlatform ResolveCompressionPlatform(
            BuildTarget target
        )
        {
            return target switch
            {
                BuildTarget.StandaloneWindows
                or BuildTarget.StandaloneWindows64
                    => CompressionPlatform.Windows,
                BuildTarget.StandaloneLinux64
                    => CompressionPlatform.Linux,
                _ => CompressionPlatform.Windows
            };
        }

        public static void ClearBuildsDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
