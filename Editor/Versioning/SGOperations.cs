using System.IO;
using System.Text.RegularExpressions;
using SGUnitySDK.Editor.Http;
using SGUnitySDK.Http;
using UnityEditor;
using UnityEngine;

namespace SGUnitySDK.Editor.Versioning
{
    public class SGOperations
    {
        private const string VersionKeyPattern = @"""version""\s*:\s*""(?<version>\d+\.\d+\.\d+[^""]*)""";

        public static async Awaitable<VersionDTO> StartVersionWithRemote(
            VersioningProcess process,
            SemVerType targetVersion
        )
        {
            var request = GameManagementRequest
                .To("/start-new-version", SGUnitySDK.Http.HttpMethod.Post)
                .SetBody(new StartGameVersionUpdateDTO()
                {
                    VersionUpdateType = VersionUpdateType.Specific,
                    SpecificVersion = targetVersion.Raw,
                    IsPrerelease = false
                });

            var response = await request.SendAsync();
            if (!response.Success)
            {
                var errorBody = response.ReadErrorBody();
                LogErrors(errorBody);
                throw new RequestFailedException(errorBody, response.ResponseCode);
            }

            process.StartedInRemote = true;

            return response.ReadBodyData<VersionDTO>();
        }

        public static async Awaitable CancelVersionPreparation(
            VersioningProcess process
        )
        {
            var request = GameManagementRequest
                .To("/cancel-version-in-preparation", HttpMethod.Delete);

            var response = await request.SendAsync();

            if (!response.Success)
            {
                var errorBody = response.ReadErrorBody();
                LogErrors(errorBody);
                throw new RequestFailedException(errorBody, response.ResponseCode);
            }

            process.StartedInRemote = false;
            process.ClearVersionBuilds();
        }

        public static async Awaitable CloseRemoteVersion(
            VersioningProcess process,
            SemVerType targetVersion
        )
        {
            string version = targetVersion.Raw;
            SGVersionLogger.Log($"Closing remote version {version}...");
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
                    var errorBody = response.ReadErrorBody();
                    LogErrors(errorBody);
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }
            }
            catch (System.Exception ex)
            {
                SGVersionLogger.LogError($"Failed to end version: {ex.Message}");
                throw;
            }
        }

        public static string LoadCurrentVersion()
        {
            string projectRoot = Application.dataPath.Replace("/Assets", "");
            var packageJsonPath = Path.Combine(projectRoot, "package.json");
            try
            {
                if (!File.Exists(packageJsonPath))
                    return "0.0.0";

                string jsonContent = File.ReadAllText(packageJsonPath);
                Match match = Regex.Match(jsonContent, VersionKeyPattern);

                return match.Success ? match.Groups["version"].Value : "0.0.0";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading version: {ex.Message}");
                return "0.0.0";
            }
        }

        public static bool UpdateVersionEverywhere(string newVersion)
        {
            try
            {
                string currentVersion = LoadCurrentVersion();
                if (currentVersion == newVersion)
                {
                    Debug.LogWarning($"Version is already {newVersion}, no update needed");
                    return true;
                }

                if (!UpdatePackageJsonVersion(newVersion))
                    return false;

                PlayerSettings.bundleVersion = newVersion;
                return true;
            }
            catch (System.Exception ex)
            {
                SGLogger.LogError($"Failed to update version: {ex.Message}");
                return false;
            }
        }

        public static void LogErrors(SGHttpResponse.SGErrorBody errorBody)
        {
            foreach (var message in errorBody.Messages)
            {
                SGLogger.LogError(message);
            }
        }


        private static bool UpdatePackageJsonVersion(string newVersion)
        {
            try
            {
                string projectRoot = Application.dataPath.Replace("/Assets", "");
                var packageJsonPath = Path.Combine(projectRoot, "package.json");

                if (!File.Exists(packageJsonPath))
                    return false;

                string jsonContent = File.ReadAllText(packageJsonPath);
                string updatedContent = Regex.Replace(jsonContent, VersionKeyPattern, $"\"version\": \"{newVersion}\"");

                if (updatedContent == jsonContent)
                    return false;

                File.Copy(packageJsonPath, $"{packageJsonPath}.backup", true);
                File.WriteAllText(packageJsonPath, updatedContent);
                return true;
            }
            catch (System.Exception ex)
            {
                SGLogger.LogError($"Error updating package.json: {ex.Message}");
                return false;
            }
        }
    }

}