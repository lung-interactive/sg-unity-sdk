using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.IO;
using System;

namespace SGUnitySDK.Editor.Versioning
{
    public static class SemanticVersionUpdater
    {
        private static string packageJsonPath;
        private const string VersionKeyPattern = @"""version""\s*:\s*""(?<version>\d+\.\d+\.\d+[^""]*)""";
        private static string cachedVersionForRollback;

        public struct VersionIncrementReport
        {
            public bool success;
            public string newVersion;
            public string errorMessage;
            public VersionIncrementType incrementType;
        }

        public enum VersionIncrementType { None, Patch, Minor, Major }

        static SemanticVersionUpdater()
        {
            string projectRoot = Application.dataPath.Replace("/Assets", "");
            packageJsonPath = Path.Combine(projectRoot, "package.json");

            if (!File.Exists(packageJsonPath))
            {
                File.WriteAllText(packageJsonPath, @"{
                    ""name"": ""com.yourcompany.yourpackage"",
                    ""version"": ""0.0.0"",
                    ""displayName"": ""Your Package"",
                    ""description"": ""Package description"",
                }");
                Debug.Log("Created default package.json file");
            }
        }

        public static VersionIncrementReport CalculateNewVersionOnly()
        {
            cachedVersionForRollback = string.Empty;
            var report = new VersionIncrementReport();

            try
            {
                EditorApplication.LockReloadAssemblies();

                string commitMessages = GitExecutor.GetCommitMessagesSinceLastVersion();
                cachedVersionForRollback = LoadCurrentVersion();

                // Se não há tags e nenhum commit recente, mantenha a versão atual
                if (string.IsNullOrEmpty(commitMessages))
                {
                    report.errorMessage = "No new commits or version tags found. Version remains unchanged.";
                    report.newVersion = cachedVersionForRollback;
                    return report;
                }

                report.incrementType = AnalyzeCommitMessages(commitMessages);
                report.newVersion = CalculateNewVersion(cachedVersionForRollback, report.incrementType);
                report.success = true;
                return report;
            }
            catch (Exception ex)
            {
                report.errorMessage = $"Error during version calculation: {ex.Message}";
                return report;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        public struct CommitVersionResult
        {
            public bool Success;
            public string ErrorMessage;
            public string FailedStep;
            public string GitOutput;
            public string GitError;
        }

        public static CommitVersionResult CommitVersionUpdate(string version)
        {
            try
            {
                EditorApplication.LockReloadAssemblies();

                // 1. Checkout para main
                if (!GitExecutor.Checkout("main"))
                {
                    var (output, error, _) = GitExecutor.ExecuteGitCommand("rev-parse --abbrev-ref HEAD");
                    return new CommitVersionResult
                    {
                        Success = false,
                        FailedStep = "Checkout main",
                        ErrorMessage = $"Failed to checkout main branch. Current branch: {output}",
                        GitOutput = output,
                        GitError = error
                    };
                }

                // 2. Atualizar arquivos de versão explicitamente
                if (!UpdateVersionEverywhere(version))
                {
                    return new CommitVersionResult
                    {
                        Success = false,
                        FailedStep = "Update version files",
                        ErrorMessage = "Failed to update version in project files",
                        GitOutput = string.Empty,
                        GitError = string.Empty
                    };
                }

                // 3. Stage das mudanças de versão
                var (addOutput, addError, addSuccess) = GitExecutor.ExecuteGitCommand("add package.json ProjectSettings/ProjectSettings.asset");
                if (!addSuccess)
                {
                    return new CommitVersionResult
                    {
                        Success = false,
                        FailedStep = "Stage version changes",
                        ErrorMessage = "Failed to stage version files",
                        GitOutput = addOutput,
                        GitError = addError
                    };
                }

                // 4. Merge develop into main
                var (mergeOutput, mergeError, mergeSuccess) = GitExecutor.ExecuteGitCommand($"merge develop -m \"Merge develop into main for version {version}\"");
                if (!mergeSuccess)
                {
                    return new CommitVersionResult
                    {
                        Success = false,
                        FailedStep = "Merge develop into main",
                        ErrorMessage = "Merge conflict or other merge error",
                        GitOutput = mergeOutput,
                        GitError = mergeError
                    };
                }

                // 5. Verificar se há mudanças para commitar
                var (statusOutput, statusError, statusSuccess) = GitExecutor.ExecuteGitCommand("status --porcelain");
                if (!statusSuccess)
                {
                    return new CommitVersionResult
                    {
                        Success = false,
                        FailedStep = "Check git status",
                        ErrorMessage = "Failed to check repository status",
                        GitOutput = statusOutput,
                        GitError = statusError
                    };
                }

                // 6. Criar commit apenas se houver mudanças
                if (!string.IsNullOrWhiteSpace(statusOutput))
                {
                    var (commitOutput, commitError, commitSuccess) = GitExecutor.ExecuteGitCommand($"commit -m \"Update version to {version}\"");
                    if (!commitSuccess)
                    {
                        return new CommitVersionResult
                        {
                            Success = false,
                            FailedStep = "Create commit",
                            ErrorMessage = "Failed to create version commit",
                            GitOutput = commitOutput,
                            GitError = commitError
                        };
                    }
                }

                // 7. Criar e push da tag
                if (!GitExecutor.CreateAndPushVersionTag(version))
                {
                    return new CommitVersionResult
                    {
                        Success = false,
                        FailedStep = "Create and push tag",
                        ErrorMessage = "Failed to create or push version tag",
                        GitOutput = string.Empty,
                        GitError = string.Empty
                    };
                }

                // 8. Push para main
                var (pushMainOutput, pushMainError, pushMainSuccess) = GitExecutor.ExecuteGitCommand("push origin main");
                if (!pushMainSuccess)
                {
                    return new CommitVersionResult
                    {
                        Success = false,
                        FailedStep = "Push main branch",
                        ErrorMessage = "Failed to push main branch",
                        GitOutput = pushMainOutput,
                        GitError = pushMainError
                    };
                }

                // 9. Checkout para develop
                if (!GitExecutor.Checkout("develop"))
                {
                    return new CommitVersionResult
                    {
                        Success = false,
                        FailedStep = "Checkout develop",
                        ErrorMessage = "Failed to checkout develop branch",
                        GitOutput = string.Empty,
                        GitError = string.Empty
                    };
                }

                // 10. Merge main into develop
                var (mergeBackOutput, mergeBackError, mergeBackSuccess) = GitExecutor.ExecuteGitCommand($"merge main -m \"Merge main into develop after version update to {version}\"");
                if (!mergeBackSuccess)
                {
                    return new CommitVersionResult
                    {
                        Success = false,
                        FailedStep = "Merge main into develop",
                        ErrorMessage = "Failed to merge main back into develop",
                        GitOutput = mergeBackOutput,
                        GitError = mergeBackError
                    };
                }

                // 11. Push para develop
                var (pushDevOutput, pushDevError, pushDevSuccess) = GitExecutor.ExecuteGitCommand("push origin develop");
                if (!pushDevSuccess)
                {
                    return new CommitVersionResult
                    {
                        Success = false,
                        FailedStep = "Push develop branch",
                        ErrorMessage = "Failed to push develop branch",
                        GitOutput = pushDevOutput,
                        GitError = pushDevError
                    };
                }

                return new CommitVersionResult { Success = true };
            }
            catch (Exception ex)
            {
                return new CommitVersionResult
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                    FailedStep = "Unknown",
                    GitOutput = string.Empty,
                    GitError = ex.ToString()
                };
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        public static string LoadCurrentVersion()
        {
            try
            {
                if (!File.Exists(packageJsonPath))
                    return "0.0.0";

                string jsonContent = File.ReadAllText(packageJsonPath);
                Match match = Regex.Match(jsonContent, VersionKeyPattern);

                return match.Success ? match.Groups["version"].Value : "0.0.0";
            }
            catch (Exception ex)
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
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update version: {ex.Message}");
                return false;
            }
        }

        private static void PerformRollback()
        {
            if (!string.IsNullOrEmpty(cachedVersionForRollback))
            {
                UpdateVersionEverywhere(cachedVersionForRollback);
            }
        }

        private static VersionIncrementType AnalyzeCommitMessages(string commitMessages)
        {
            bool hasBreaking = Regex.IsMatch(commitMessages, @"^(breaking|major)(\(.*\))?:", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            bool hasFeat = Regex.IsMatch(commitMessages, @"^(feat|minor)(\(.*\))?:", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            bool hasFix = Regex.IsMatch(commitMessages, @"^(fix|patch)(\(.*\))?:", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            if (hasBreaking) return VersionIncrementType.Major;
            if (hasFeat) return VersionIncrementType.Minor;
            if (hasFix) return VersionIncrementType.Patch;
            return VersionIncrementType.None;
        }

        private static string CalculateNewVersion(string currentVersion, VersionIncrementType incrementType)
        {
            var parts = currentVersion.Split('.');
            if (parts.Length != 3 ||
                !int.TryParse(parts[0], out int major) ||
                !int.TryParse(parts[1], out int minor) ||
                !int.TryParse(parts[2], out int patch))
            {
                return "1.0.0";
            }

            switch (incrementType)
            {
                case VersionIncrementType.Major: return $"{major + 1}.0.0";
                case VersionIncrementType.Minor: return $"{major}.{minor + 1}.0";
                case VersionIncrementType.Patch: return $"{major}.{minor}.{patch + 1}";
                default: return currentVersion;
            }
        }

        private static bool UpdatePackageJsonVersion(string newVersion)
        {
            try
            {
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
            catch (Exception ex)
            {
                Debug.LogError($"Error updating package.json: {ex.Message}");
                return false;
            }
        }
    }
}