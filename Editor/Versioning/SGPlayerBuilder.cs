using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using HMSUnitySDK;
using SGUnitySDK.Utils;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SGUnitySDK.Editor.Versioning
{
    /// <summary>
    /// Builds multiple player targets and zips outputs. Integrates with
    /// HMSRuntimeInfo so that the HMSRuntimeProfile configured in
    /// SGEditorConfig is applied to the Resources asset during builds and then
    /// restored afterward.
    /// </summary>
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

        // ─────────────────────────────────────────────────────────────────────
        // HMS Runtime Profile Patch (apply/restore)
        // ─────────────────────────────────────────────────────────────────────

        private static HMSRuntimeProfile _cachedProfile;
        private static bool _profilePatched;

        /// <summary>
        /// Applies the runtime profile from SGEditorConfig into the
        /// HMSRuntimeInfo asset in Resources, caching the original profile
        /// so it can be restored later.
        /// </summary>
        private static void ApplyRuntimeProfilePatch()
        {
#if UNITY_EDITOR
            try
            {
                var cfg = SGUnitySDK.Editor.SGEditorConfig.instance;
                var selectedProfile = cfg != null ? cfg.RuntimeProfile : null;

                var runtimeInfo = HMSRuntimeInfo.GetFromResources();
                if (runtimeInfo == null)
                {
                    Debug.LogWarning(
                        "[SG Build] HMSRuntimeInfo asset not found in " +
                        "Resources. Profile patch will be skipped."
                    );
                    return;
                }

                _cachedProfile = runtimeInfo.Profile;
                _profilePatched = true;

                runtimeInfo.SetProfile(selectedProfile);
                EditorUtility.SetDirty(runtimeInfo);
                AssetDatabase.SaveAssets();
                HMSRuntimeInfo.ClearCache();

                Debug.Log(
                    "[SG Build] HMSRuntimeProfile applied to Resources: " +
                    (selectedProfile != null ? selectedProfile.name : "NULL")
                );
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[SG Build] Failed to apply HMSRuntimeProfile patch. " +
                    $"Reason: {ex.Message}"
                );
            }
#endif
        }

        /// <summary>
        /// Restores the previous HMSRuntimeProfile into the HMSRuntimeInfo
        /// asset if it had been patched by ApplyRuntimeProfilePatch.
        /// </summary>
        private static void RestoreRuntimeProfilePatch()
        {
#if UNITY_EDITOR
            if (!_profilePatched) return;

            try
            {
                var runtimeInfo = HMSRuntimeInfo.GetFromResources();
                if (runtimeInfo == null)
                {
                    Debug.LogWarning(
                        "[SG Build] HMSRuntimeInfo asset not found in " +
                        "Resources during restore. Profile may remain altered."
                    );
                    _cachedProfile = null;
                    _profilePatched = false;
                    return;
                }

                runtimeInfo.SetProfile(_cachedProfile);
                EditorUtility.SetDirty(runtimeInfo);
                AssetDatabase.SaveAssets();
                HMSRuntimeInfo.ClearCache();

                Debug.Log(
                    "[SG Build] HMSRuntimeProfile restored to: " +
                    (_cachedProfile != null ? _cachedProfile.name : "NULL")
                );
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[SG Build] Failed to restore HMSRuntimeProfile. " +
                    $"Reason: {ex.Message}"
                );
            }
            finally
            {
                _cachedProfile = null;
                _profilePatched = false;
            }
#endif
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Perform multiple builds synchronously on the main thread. Ensures
        /// HMSRuntimeInfo.Profile is patched to the SGEditorConfig profile
        /// during the builds, and restored afterward.
        /// </summary>
        /// <param name="setups">Build setups to execute.</param>
        /// <param name="commonBuildPath">Root output folder.</param>
        /// <param name="targetVersion">Semantic version to embed.</param>
        /// <returns>List of local build results.</returns>
        public static List<SGLocalBuildResult> PerformMultipleBuilds(
            List<SGBuildSetup> setups,
            string commonBuildPath,
            string targetVersion
        )
        {
            var buildResults = new List<SGLocalBuildResult>();

            // Apply HMS profile patch once for the full batch.
            ApplyRuntimeProfilePatch();

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
                            "Error building profile " +
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
                // Always restore HMS profile patch.
                RestoreRuntimeProfilePatch();
            }

            return buildResults;
        }

        /// <summary>
        /// Build one setup and zip its output synchronously.
        /// </summary>
        /// <param name="setup">Build setup definition.</param>
        /// <param name="commonBuildPath">Root output folder.</param>
        /// <param name="targetVersion">Semantic version to embed.</param>
        /// <param name="completed">Index of the current setup.</param>
        /// <param name="totalSetups">Total number of setups.</param>
        /// <returns>Local build result for this setup.</returns>
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
                        "Build failed for profile " +
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

        // ─────────────────────────────────────────────────────────────────────
        // Build helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Use Unity Build Pipeline with the given profile.
        /// </summary>
        /// <param name="profile">Build profile to activate.</param>
        /// <param name="outputPath">Full output path for the player.</param>
        /// <param name="buildOptions">Additional build options.</param>
        /// <returns>Build summary produced by the pipeline.</returns>
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

        /// <summary>
        /// Returns the common base name: Product.vX_Y_Z (sanitized).
        /// </summary>
        private static string GetBaseName(
            BuildProfile profile,
            string version
        )
        {
            string product = SanitizeFileName(Application.productName);
            string ver = SanitizeVersion(version);
            return $"{product}.v{ver}";
        }

        /// <summary>
        /// Returns the build output directory name.
        /// </summary>
        private static string GetBuildFolderName(
            BuildProfile profile,
            string version
        )
        {
            return $"{GetBaseName(profile, version)}." +
                   $"{GetPlatformName(profile.GetBuildTargetInternal())}";
        }

        /// <summary>
        /// Returns the executable name, platform-aware.
        /// </summary>
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

        /// <summary>
        /// Maps Unity BuildTarget to human-readable platform name.
        /// </summary>
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

        /// <summary>
        /// Removes invalid filename characters.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Concat(name.Split(invalid));
        }

        /// <summary>
        /// Converts '1.2.3' into '1_2_3'.
        /// </summary>
        private static string SanitizeVersion(string version)
        {
            return version.Replace('.', '_');
        }

        /// <summary>
        /// Resolves compression platform so the zipper can adjust
        /// platform-specific details (e.g., chmod +x on Linux).
        /// </summary>
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

        /// <summary>
        /// Deletes the builds directory if present.
        /// </summary>
        public static void ClearBuildsDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
