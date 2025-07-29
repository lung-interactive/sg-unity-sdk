using UnityEngine;
using UnityEditor;
using System;
using SGUnitySDK.Http;
using SGUnitySDK.Editor.Http;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SGUnitySDK.Editor.Utils;
using System.Threading.Tasks;
using HMSUnitySDK;

namespace SGUnitySDK.Editor.Versioning
{
    public static partial class SGVersioner
    {
        private class ReleaseState
        {
            public string originalVersion;
            public bool versionUpdatedLocally;
            public bool versionStartedRemotely;
            public string remoteSemver;
            public List<SGLocalBuildResult> buildResults;
            public SemanticVersionUpdater.VersionIncrementReport versionReport;
        }

        private static HMSRuntimeProfile _originalRuntimeProfile;

        [MenuItem("Tools/SGUnitySDK/Versioning/Versionate and send to remote", false, 0)]
        public static void StartVersioningProcess()
        {
            _ = PerformVersioningProcess();
        }

        private static async Awaitable PerformVersioningProcess()
        {
            var state = new ReleaseState();
            SGVersionLogger.Initialize();
            var hmsRuntimeInfo = HMSRuntimeInfo.GetFromResources();
            _originalRuntimeProfile = hmsRuntimeInfo.Profile;
            hmsRuntimeInfo.SetProfile(SGEditorConfig.instance.RuntimeProfile);

            try
            {
                SGVersionLogger.Log("Starting versioning process...");

                // Step 1: Validate release conditions
                var condition = await ExecuteStep("Validating release conditions", () => ValidateReleaseConditions());
                if (!condition.isMet)
                {
                    SGVersionLogger.LogError(condition.errorMessage);
                    return;
                }

                // Cache original version for potential rollback
                state.originalVersion = SemanticVersionUpdater.LoadCurrentVersion();
                SGVersionLogger.Log($"Original version: {state.originalVersion}");

                // Step 2: Calculate and update version locally (without committing)
                state.versionReport = await ExecuteStep("Calculating and updating version", () => CalculateAndUpdateVersion());
                if (!state.versionReport.success)
                {
                    SGVersionLogger.LogError($"Failed to perform release process: {state.versionReport.errorMessage}");
                    return;
                }
                state.versionUpdatedLocally = true;
                SGVersionLogger.Log($"New version calculated: {state.versionReport.newVersion}");

                // Step 3: Build all platforms
                state.buildResults = await ExecuteStep(
                    "Building all platforms",
                    () => PerformBuilds(state.versionReport.newVersion)
                );
                if (state.buildResults == null || state.buildResults.Any(b => !b.success))
                {
                    SGVersionLogger.LogError("Build process failed");
                    throw new Exception("Build process failed");
                }
                SGVersionLogger.Log($"Successfully built {state.buildResults.Count} platforms");

                // Step 4: Prepare remote version
                var remoteVersion = await ExecuteStep("Preparing remote version", () => PrepareRemoteVersion(state.versionReport.newVersion)) ?? throw new Exception("Failed to prepare remote version");
                state.versionStartedRemotely = true;
                state.remoteSemver = remoteVersion.Semver;
                SGVersionLogger.Log($"Remote version prepared: {remoteVersion.Semver}");

                // Step 5: Upload builds
                var uploadSuccess = await ExecuteStep("Uploading builds", () => UploadBuildsToRemote(state.buildResults, remoteVersion));
                if (!uploadSuccess)
                {
                    SGVersionLogger.LogError("Build upload failed");
                    throw new Exception("Build upload failed");
                }
                SGVersionLogger.Log("All builds uploaded successfully");

                // Step 6: Clean up working directory after builds
                await ExecuteStep("Cleaning up working directory", DiscardWorkingChanges);
                SGVersionLogger.Log("Working directory cleaned of build artifacts.");

                // Step 7: Clean builds directory
                await ExecuteStep("Cleaning builds directory", CleanBuildsDirectory);
                SGVersionLogger.Log("Builds directory cleaned");

                // Step 8: Commit version update to Git
                var commitSuccess = await ExecuteStep("Committing version update", () => CommitVersionChanges(state.versionReport.newVersion));
                if (!commitSuccess)
                {
                    SGVersionLogger.LogError("Failed to commit version update");
                    throw new Exception("Failed to commit version update");
                }
                SGVersionLogger.Log("Version changes committed to Git");

                // Step 9: End version
                await ExecuteStep("Ending version", () => EndRemoteVersion(state.remoteSemver));
                SGVersionLogger.Log("Remote version ended successfully");

                // Step 10: Final cleanup
                await ExecuteStep("Final cleanup", FinalCleanup);
                SGVersionLogger.Log("Final cleanup completed");

                SGVersionLogger.Log("Versioning process completed successfully!");
            }
            catch (Exception ex)
            {
                SGVersionLogger.LogError($"Versioning process failed: {ex.Message}");
                await ExecuteStep("Performing rollback", () => PerformRollback(state));
                SGVersionLogger.Log("Rollback completed");

                // Abre o arquivo de log imediatamente em caso de erro
                SGVersionLogger.SaveLog(openFile: true);
                return;
            }
            finally
            {
                SGVersionLogger.SaveLog();
            }
        }

        #region Step Methods (remain unchanged except for logging)

        private static async Awaitable<ReleaseCondition> ValidateReleaseConditions()
        {
            try
            {
                SGVersionLogger.Log("Validating game management token...");
                var request = GameManagementRequest.To("/validate-token");
                var response = await request.SendAsync();

                if (!response.Success)
                {
                    return new ReleaseCondition
                    {
                        isMet = false,
                        errorMessage = "Failed to validate token."
                    };
                }
            }
            catch (Exception ex)
            {
                return new ReleaseCondition
                {
                    isMet = false,
                    errorMessage = $"Failed to validate token: {ex.Message}"
                };
            }

            var buildSetups = SGEditorConfig.instance.BuildSetups;
            if (buildSetups.Count == 0)
            {
                return new ReleaseCondition
                {
                    isMet = false,
                    errorMessage = "Cannot increment version with no build setups defined."
                };
            }

            var gameManagementToken = SGEditorConfig.instance.GMT;
            if (string.IsNullOrEmpty(gameManagementToken))
            {
                return new ReleaseCondition
                {
                    isMet = false,
                    errorMessage = "Cannot increment version with no game management token defined."
                };
            }

            SGVersionLogger.Log("Checking repository status...");
            if (!GitExecutor.IsRepositoryClean())
            {
                return new ReleaseCondition
                {
                    isMet = false,
                    errorMessage = "Cannot increment version with uncommitted changes. Please commit or stash changes first."
                };
            }

            string currentBranch = GitExecutor.GetCurrentBranch();
            if (currentBranch != "develop")
            {
                return new ReleaseCondition
                {
                    isMet = false,
                    errorMessage = "Cannot increment version. Please checkout develop branch first."
                };
            }

            SGVersionLogger.Log("Pulling latest changes from develop branch...");
            if (!GitExecutor.Pull("develop"))
            {
                return new ReleaseCondition
                {
                    isMet = false,
                    errorMessage = "Failed to pull latest changes from develop branch"
                };
            }

            return new ReleaseCondition { isMet = true };
        }

        private static async Awaitable<SemanticVersionUpdater.VersionIncrementReport> CalculateAndUpdateVersion()
        {
            SGVersionLogger.Log("Calculating new version...");
            await Task.CompletedTask;
            return SemanticVersionUpdater.CalculateNewVersionOnly();
        }

        private static async Awaitable<List<SGLocalBuildResult>> PerformBuilds(string targetVersion)
        {
            SGVersionLogger.Log($"Starting builds for version {targetVersion}...");
            var buildSetups = SGEditorConfig.instance.BuildSetups;
            var commonBuildPath = SGEditorConfig.instance.BuildsDirectory;
            return await SGPlayerBuilder.PerformMultipleBuilds(buildSetups, commonBuildPath, targetVersion);
        }

        private static async Awaitable<VersionDTO> PrepareRemoteVersion(string version)
        {
            SGVersionLogger.Log("Preparing remote version...");
            await CancelVersionInPreparation();
            return await StartVersionWithRemote(version);
        }

        private static async Awaitable<bool> UploadBuildsToRemote(
            List<SGLocalBuildResult> buildResults,
            VersionDTO remoteVersion
        )
        {
            SGVersionLogger.Log($"Uploading {buildResults.Count} builds to remote...");
            try
            {
                var uploadTasks = buildResults.Select(build =>
                    UploadSingleBuild(build, remoteVersion)).ToList();

                var results = await Task.WhenAll(uploadTasks);

                return results.All(result => result);
            }
            catch (Exception ex)
            {
                SGVersionLogger.LogError($"Failed during parallel uploads: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> UploadSingleBuild(SGLocalBuildResult build, VersionDTO remoteVersion)
        {
            SGVersionLogger.Log($"Uploading build: {Path.GetFileName(build.path)}");
            try
            {
                var request = GameManagementRequest.To("/start-build-upload", HttpMethod.Post);
                var body = new StartBuildUploadDTO()
                {
                    Semver = remoteVersion.Semver,
                    Platform = build.GetBuildPlatform(),
                    ExecutableName = build.executableName,
                    Filename = Path.GetFileName(build.path),
                    DownloadSize = build.compression.sizeCompressed,
                    InstalledSize = build.compression.sizeUncompressed,
                    Host = FileHost.S3,
                    OverrideExisting = true,
                };

                request.SetBody(body);
                var response = await request.SendAsync();

                if (!response.Success)
                {
                    SGVersionLogger.LogError($"Failed to start build upload: {response.ReadErrorBody()}");
                    return false;
                }

                var responseBody = response.ReadBodyData<StartBuildUploadResponseDTO>();
                var uploadToken = responseBody.UploadToken;
                var signedURL = responseBody.SignedUrl;

                var uploadSuccess = await S3Uploader.UploadFileToPresignedUrl(build.path, signedURL.Url);
                if (!uploadSuccess)
                {
                    SGVersionLogger.LogError($"Failed to upload build file: {build.path}");
                    return false;
                }

                var confirmRequest = GameManagementRequest.To("/confirm-build-upload", HttpMethod.Post)
                    .SetBody(new
                    {
                        upload_token = uploadToken,
                        semver = remoteVersion.Semver,
                        platform = build.GetBuildPlatform(),
                    });
                var confirmResponse = await confirmRequest.SendAsync();

                if (!confirmResponse.Success)
                {
                    SGVersionLogger.LogError($"Failed to confirm build upload:");
                    var errorBody = confirmResponse.ReadErrorBody();
                    foreach (var error in errorBody.Messages)
                    {
                        SGVersionLogger.LogError(error);
                    }
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                SGVersionLogger.LogError($"Failed to upload build {build.path}: {ex.Message}");
                return false;
            }
        }

        private static async Awaitable DiscardWorkingChanges()
        {
            SGVersionLogger.Log("Discarding all changes in working directory...");
            GitExecutor.DiscardAllChanges();
            await Task.CompletedTask;
        }

        private static async Awaitable CleanBuildsDirectory()
        {
            SGVersionLogger.Log("Cleaning builds directory...");
            IOHelper.ClearDirectoryContents(SGEditorConfig.instance.BuildsDirectory);
            await Task.CompletedTask;
        }

        private static async Awaitable EndRemoteVersion(string version)
        {
            SGVersionLogger.Log($"Ending remote version {version}...");
            try
            {
                var request = GameManagementRequest.To("/end-version", HttpMethod.Post);
                request.SetBody(new EndVersionDTO()
                {
                    Semver = version,
                });

                var response = await request.SendAsync();

                if (!response.Success)
                {
                    SGVersionLogger.LogError($"Failed to end version:");
                    var messages = response.ReadErrorBody().Messages;
                    foreach (var message in messages)
                    {
                        SGVersionLogger.LogError(message);
                    }
                    throw new Exception("Remote version end failed. See logs for details.");
                }
            }
            catch (Exception ex)
            {
                SGVersionLogger.LogError($"Failed to end version: {ex.Message}");
                throw;
            }
        }

        private static async Awaitable<bool> CommitVersionChanges(string version)
        {
            try
            {
                SGVersionLogger.Log($"Committing version changes for {version}...");

                var result = SemanticVersionUpdater.CommitVersionUpdate(version);

                if (!result.Success)
                {
                    // Special case: No changes needed is still considered success
                    if (result.ErrorMessage.Contains("already up to date") ||
                        result.ErrorMessage.Contains("No changes to commit"))
                    {
                        SGVersionLogger.Log("Version files already up to date, commit skipped");
                        return true;
                    }

                    // Detailed error logging
                    SGVersionLogger.LogError($"Version commit failed at step: {result.FailedStep}");
                    SGVersionLogger.LogError($"Error: {result.ErrorMessage}");

                    if (!string.IsNullOrEmpty(result.GitOutput))
                    {
                        SGVersionLogger.LogError($"Git Output: {result.GitOutput}");
                    }

                    if (!string.IsNullOrEmpty(result.GitError))
                    {
                        SGVersionLogger.LogError($"Git Error: {result.GitError}");
                    }

                    // Additional diagnostics for common failure cases
                    if (result.FailedStep == "Merge develop into main" ||
                        result.FailedStep == "Merge main into develop")
                    {
                        // Get branch status for debugging
                        var (mainStatus, _, _) = GitExecutor.ExecuteGitCommand("log --oneline -n 3 main");
                        var (developStatus, _, _) = GitExecutor.ExecuteGitCommand("log --oneline -n 3 develop");

                        SGVersionLogger.LogError($"Main branch last commits:\n{mainStatus}");
                        SGVersionLogger.LogError($"Develop branch last commits:\n{developStatus}");
                    }

                    return false;
                }

                // Log successful commit details if available
                if (!string.IsNullOrEmpty(result.GitOutput))
                {
                    SGVersionLogger.Log($"Git operation output: {result.GitOutput}");
                }

                SGVersionLogger.Log("Version changes committed successfully");
                return true;
            }
            catch (Exception ex)
            {
                SGVersionLogger.LogError($"Unexpected error during version commit: {ex.Message}");
                SGVersionLogger.LogError($"Stack trace: {ex.StackTrace}");
                return false;
            }
            finally
            {
                await Task.CompletedTask; // Ensure the method is properly async
            }
        }

        private static async Awaitable FinalCleanup()
        {
            SGVersionLogger.Log("Performing final cleanup...");

            var hmsRuntimeInfo = HMSRuntimeInfo.GetFromResources();
            hmsRuntimeInfo.SetProfile(_originalRuntimeProfile);

            try
            {
                string currentBranch = GitExecutor.GetCurrentBranch();
                if (currentBranch != "develop")
                {
                    SGVersionLogger.Log("Checking out to develop branch");
                    GitExecutor.Checkout("develop");
                }

                SGVersionLogger.Log("Cleaning builds directory");
                IOHelper.ClearDirectoryContents(SGEditorConfig.instance.BuildsDirectory);

                SGVersionLogger.Log("Discarding any remaining changes");
                GitExecutor.DiscardAllChanges();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                SGVersionLogger.LogError($"Error during final cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods (remain unchanged except for logging)

        private static async Awaitable<VersionDTO> StartVersionWithRemote(string targetVersion)
        {
            SGVersionLogger.Log($"Starting remote version for {targetVersion}...");
            try
            {
                var request = GameManagementRequest
                    .To("/start-new-version", HttpMethod.Post)
                    .SetBody(new StartGameVersionUpdateDTO()
                    {
                        VersionUpdateType = VersionUpdateType.Specific,
                        SpecificVersion = targetVersion,
                        IsPrerelease = false
                    });

                var response = await request.SendAsync();
                var version = response.ReadBodyData<VersionDTO>();

                return version;
            }
            catch (Exception ex)
            {
                SGVersionLogger.LogError($"Failed to start version: {ex.Message}");
                throw;
            }
        }

        private static async Awaitable CancelVersionInPreparation()
        {
            SGVersionLogger.Log("Cancelling any version in preparation...");
            var request = GameManagementRequest
                .To("/cancel-version-in-preparation", HttpMethod.Delete);

            await request.SendAsync();
        }

        private static async Awaitable PerformRollback(ReleaseState state)
        {
            SGVersionLogger.Log("Starting rollback process...");
            try
            {
                var hmsRuntimeInfo = HMSRuntimeInfo.GetFromResources();
                hmsRuntimeInfo.SetProfile(_originalRuntimeProfile);

                if (state.versionUpdatedLocally && !string.IsNullOrEmpty(state.originalVersion))
                {
                    SGVersionLogger.Log($"Reverting to original version: {state.originalVersion}");
                    SemanticVersionUpdater.UpdateVersionEverywhere(state.originalVersion);
                }

                if (state.versionStartedRemotely && !string.IsNullOrEmpty(state.remoteSemver))
                {
                    SGVersionLogger.Log($"Cancelling remote version: {state.remoteSemver}");
                    await CancelVersionInPreparation();
                }

                string currentBranch = GitExecutor.GetCurrentBranch();
                if (currentBranch != "develop")
                {
                    SGVersionLogger.Log("Checking out to develop branch");
                    GitExecutor.Checkout("develop");
                }

                SGVersionLogger.Log("Discarding all changes in working directory");
                GitExecutor.DiscardAllChanges();

                SGVersionLogger.Log("Cleaning builds directory");
                IOHelper.ClearDirectoryContents(SGEditorConfig.instance.BuildsDirectory);
            }
            catch (Exception ex)
            {
                SGVersionLogger.LogError($"Error during rollback: {ex.Message}");
            }
        }

        private static async Awaitable<T> ExecuteStep<T>(string stepName, Func<Awaitable<T>> stepAction)
        {
            SGVersionLogger.Log($"Starting step: {stepName}");
            try
            {
                EditorApplication.LockReloadAssemblies();
                var result = await stepAction();
                SGVersionLogger.Log($"Completed step: {stepName}");
                return result;
            }
            catch (Exception ex)
            {
                SGVersionLogger.LogError($"Failed during step '{stepName}': {ex.Message}");
                throw;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        private static async Awaitable ExecuteStep(string stepName, Func<Awaitable> stepAction)
        {
            SGVersionLogger.Log($"Starting step: {stepName}");
            try
            {
                EditorApplication.LockReloadAssemblies();
                await stepAction();
                SGVersionLogger.Log($"Completed step: {stepName}");
            }
            catch (Exception ex)
            {
                SGVersionLogger.LogError($"Failed during step '{stepName}': {ex.Message}");
                throw;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        #endregion

        private struct ReleaseCondition
        {
            public bool isMet;
            public string errorMessage;
        }
    }
}
