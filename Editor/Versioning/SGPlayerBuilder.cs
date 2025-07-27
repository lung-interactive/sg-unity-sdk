using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
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
            "DoNotShip",           // Official Unity exclusion
            "BackUp",              // Common backup folders
            "Temp",                // Temporary files
            "~",                   // Temporary/backup files
            ".tmp",                // Temporary files
            ".bak",                // Backup files
            ".git",                // Version control
            ".svn",                // Version control
            ".vs",                 // Visual Studio files
            ".idea",               // Rider/IntelliJ files
            "Builds",              // Common build output
            "Logs",                // Log files
            "Obj",                 // Intermediate build files
            "Library",             // Unity generated files
            "ProjectSettings~",    // Backup settings
            "csc.rsp",             // Compiler response files
            "mcs.rsp",
            "gmcs.rsp",
            "smcs.rsp",
            "Thumbs.db",           // Windows thumbnail cache
            ".DS_Store"            // macOS metadata
        };

        /// <summary>
        /// Performs multiple build operations asynchronously for a list of build setups.
        /// </summary>
        /// <param name="setups">A list of build setups to process.</param>
        /// <param name="commonBuildPath">The directory path where all builds will be stored.</param>
        /// <param name="targetVersion">The target version for the builds.</param>
        /// <returns>A list of results for each build, indicating success or failure with additional details.</returns>
        public static async Awaitable<List<SGLocalBuildResult>> PerformMultipleBuilds(
            List<SGBuildSetup> setups,
            string commonBuildPath,
            string targetVersion
        )
        {
            List<SGLocalBuildResult> buildResults = new();

            try
            {
                int totalSetups = setups.Count;

                // Clean build directory
                if (Directory.Exists(commonBuildPath))
                {
                    Directory.Delete(commonBuildPath, true);
                    Directory.CreateDirectory(commonBuildPath);
                }

                foreach (var setup in setups)
                {
                    try
                    {
                        var result = await BuildAndZipSetup(
                            setup,
                            commonBuildPath,
                            targetVersion,
                            buildResults.Count,
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
                        Debug.LogError($"Error building profile {setup.profile.name}: {ex.Message}");
                    }
                }

                // Display summary of builds
                int successfulBuilds = buildResults.Count(r => r.success);

                Debug.Log($"Successfully built {successfulBuilds} out of {totalSetups} profiles.");
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
        /// Builds and zips a given <see cref="SGBuildSetup"/>.</summary>
        /// <param name="setup">The build setup to build and zip.</param>
        /// <param name="commonBuildPath">The path to where the build will be saved.</param>
        /// <param name="targetVersion">The version string to use for the build.</param>
        /// <param name="completed">The number of builds that have been completed so far.</param>
        /// <param name="totalSetups">The total number of build setups to build.</param>
        /// <returns>A <see cref="SGLocalBuildResult"/> containing the result of the build and zip operation.</returns>
        public static async Awaitable<SGLocalBuildResult> BuildAndZipSetup(
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
                BuiltAt = System.DateTime.Now
            };

            try
            {
                // Show build progress with percentage
                string buildMessage = $"Building {setup.profile.name}...";
                EditorUtility.DisplayProgressBar(
                    "Building Player",
                    buildMessage,
                    (float)completed / totalSetups
                );

                string buildFolderName = GetBuildFolderName(setup.profile, targetVersion);
                string buildPath = Path.Combine(commonBuildPath, buildFolderName);
                Directory.CreateDirectory(buildPath);

                string executableName = GetExecutableName(setup.profile, targetVersion);
                string buildCompletePath = Path.Combine(buildPath, executableName);

                // Let Unity handle its own build progress bar
                var summary = SGPlayerBuilder.BuildUsingProfile(setup.profile, buildCompletePath);

                if (summary.result != BuildResult.Succeeded)
                {
                    result.success = false;
                    result.errorMessage = $"Build failed for profile {setup.profile.name}";
                    Debug.LogError(result.errorMessage);
                    return result;
                }


                // The 'buildPath' currently holds the uncompressed build directory path.
                // We will assign this to result.Path initially, then update it to the zip path.
                result.path = buildPath;

                // Clear Unity's build progress before showing our zipping progress
                EditorUtility.ClearProgressBar();

                // Show indeterminate progress for zipping
                EditorUtility.DisplayProgressBar(
                    "Compressing Files",
                    $"Zipping {setup.profile.name}...",
                    -1
                );

                string zipFileName = $"{GetBaseName(setup.profile, targetVersion)}.{GetPlatformName(setup.profile.GetBuildTargetInternal())}.zip";
                string zipFilePath = Path.Combine(commonBuildPath, zipFileName);

                // Call SGCompressor.ZipAllFiles and directly capture its CompressingResult
                var compressionResult = await SGCompressor.ZipAllFiles(
                    buildPath,
                    zipFilePath,
                    DefaultExclusionFilters,
                    System.IO.Compression.CompressionLevel.Optimal,
                    ResolveCompressionPlatform(setup.profile.GetBuildTargetInternal())
                );

                result.success = true;
                result.executableName = executableName;
                result.path = zipFilePath; // Update result.Path to the final zip file path
                result.compression = compressionResult; // Assign the full CompressingResult

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
        /// Builds the player using the specified profile and options.
        /// </summary>
        /// <param name="profile">The build profile to use.</param>
        /// <param name="outputPath">The path to write the built player to.</param>
        /// <param name="buildOptions">The build options to use.</param>
        /// <returns>A <see cref="BuildSummary"/> containing information about the build.</returns>
        private static BuildSummary BuildUsingProfile(
            BuildProfile profile,
            string outputPath,
            BuildOptions buildOptions = BuildOptions.None
        )
        {
            var directoryName = Path.GetDirectoryName(outputPath);
            Directory.CreateDirectory(directoryName);
            BuildProfile.SetActiveBuildProfile(profile);

            var options = new BuildPlayerWithProfileOptions()
            {
                locationPathName = outputPath,
                options = buildOptions,
                buildProfile = profile
            };

            var report = BuildPipeline.BuildPlayer(options);
            return report.summary;
        }

        private static string GetBaseName(BuildProfile profile, string version)
        {
            string productName = SanitizeFileName(Application.productName);
            string sanitizedVersion = SanitizeVersion(version);
            return $"{productName}.v{sanitizedVersion}";
        }

        private static string GetBuildFolderName(BuildProfile profile, string version)
        {
            return $"{GetBaseName(profile, version)}.{GetPlatformName(profile.GetBuildTargetInternal())}";
        }

        private static string GetExecutableName(BuildProfile profile, string version)
        {
            string baseName = GetBaseName(profile, version);
            var platform = profile.GetBuildTargetInternal();

            return platform switch
            {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => $"{baseName}.exe",
                BuildTarget.StandaloneLinux64 => $"{baseName}.x86_64",
                BuildTarget.StandaloneOSX => $"{baseName}.app",
                _ => $"{baseName}.exe"
            };
        }

        private static string GetPlatformName(BuildTarget buildTarget)
        {
            return buildTarget switch
            {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => "windows",
                BuildTarget.StandaloneLinux64 => "linux",
                BuildTarget.StandaloneOSX => "macos",
                _ => "windows"
            };
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Concat(name.Split(invalidChars));
        }

        private static string SanitizeVersion(string version)
        {
            return version.Replace('.', '_');
        }

        private static CompressionPlatform ResolveCompressionPlatform(BuildTarget buildTarget)
        {
            return buildTarget switch
            {
                BuildTarget.StandaloneWindows
                or BuildTarget.StandaloneWindows64 => CompressionPlatform.Windows,
                BuildTarget.StandaloneLinux64 => CompressionPlatform.Linux,
                _ => CompressionPlatform.Windows
            };
        }
    }
}