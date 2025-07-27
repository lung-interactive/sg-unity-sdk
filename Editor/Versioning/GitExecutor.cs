using UnityEngine;
using SystemDiagnostics = System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using System.IO;

namespace SGUnitySDK.Editor.Versioning
{
    public static class GitExecutor
    {
        private static string GetProjectRootPath()
        {
            try
            {
                string path = Path.GetFullPath(Application.dataPath.Replace("/Assets", ""));
                if (!Directory.Exists(path))
                {
                    Debug.LogError($"Project root path does not exist: {path}");
                    return string.Empty;
                }
                return path;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get project root path: {ex.Message}");
                return string.Empty;
            }
        }

        public static (string output, string error, bool success) ExecuteGitCommand(string arguments)
        {
            string workingDir = GetProjectRootPath();
            if (string.IsNullOrEmpty(workingDir))
            {
                return (string.Empty, "Invalid working directory", false);
            }

            try
            {
                using (var process = new SystemDiagnostics.Process())
                {
                    process.StartInfo = new SystemDiagnostics.ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = workingDir
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    bool success = process.ExitCode == 0 ||
                                  (process.ExitCode != 0 && error.Contains("Already up to date"));

                    // Log detalhado para diagnóstico
                    Debug.Log($"Git Command: git {arguments}\n" +
                             $"Exit Code: {process.ExitCode}\n" +
                             $"Output: {output}\n" +
                             $"Error: {error}\n" +
                             $"Success: {success}");

                    return (output, error, success);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Git command execution failed: {ex.Message}");
                return (string.Empty, ex.Message, false);
            }
        }

        public static bool ExecuteGitCommandWithStatus(string arguments)
        {
            var (output, error, success) = ExecuteGitCommand(arguments);

            if (!success && !error.Contains("Already up to date"))
            {
                Debug.LogError($"Git command failed: git {arguments}\nError: {error}");
                return false;
            }

            Debug.Log($"Git command executed: git {arguments}\nOutput: {output}\nError: {error}");
            return true;
        }

        public static string GetCurrentBranch()
        {
            var (output, error, success) = ExecuteGitCommand("rev-parse --abbrev-ref HEAD");
            if (!success)
            {
                Debug.LogError($"Failed to get current branch: {error}");
                return string.Empty;
            }
            return output.Trim();
        }

        public static bool IsRepositoryClean()
        {
            var (status, error, success) = ExecuteGitCommand("status --porcelain");
            if (!success)
            {
                Debug.LogError($"Failed to check repository status: {error}");
                return false;
            }
            return string.IsNullOrEmpty(status);
        }

        public static bool Pull(string branchName)
        {
            var (output, error, success) = ExecuteGitCommand($"pull origin {branchName}");
            if (!success && !error.Contains("Already up to date"))
            {
                Debug.LogError($"Failed to pull branch {branchName}: {error}");
                return false;
            }

            if (error.Contains("Already up to date"))
            {
                Debug.Log($"Branch {branchName} is already up to date");
            }
            else
            {
                Debug.Log($"Successfully pulled branch {branchName}");
            }

            return true;
        }

        public static bool Checkout(string branchName)
        {
            var (output, error, success) = ExecuteGitCommand($"checkout {branchName}");
            if (!success)
            {
                Debug.LogError($"Failed to checkout branch {branchName}: {error}");
                return false;
            }
            Debug.Log($"Successfully checked out branch {branchName}");
            return true;
        }

        public static bool Merge(string sourceBranch, string message)
        {
            var (output, error, success) = ExecuteGitCommand($"merge {sourceBranch} -m \"{message}\"");
            if (!success)
            {
                Debug.LogError($"Failed to merge {sourceBranch}: {error}");
                return false;
            }
            Debug.Log($"Successfully merged {sourceBranch}");
            return true;
        }

        public static bool Push(string branchName)
        {
            var (output, error, success) = ExecuteGitCommand($"push origin {branchName}");
            if (!success)
            {
                Debug.LogError($"Failed to push branch {branchName}: {error}");
                return false;
            }
            Debug.Log($"Successfully pushed branch {branchName}");
            return true;
        }

        public static string GetCommitMessagesSinceLastVersion()
        {
            string latestTag = GetLatestSemanticVersionTag();

            if (string.IsNullOrEmpty(latestTag))
            {
                Debug.LogWarning("No semantic version tag found. Assuming initial version (0.0.0).");
                return string.Empty; // Retorna vazio para forçar versão inicial
            }

            var (output, error, success) = ExecuteGitCommand($"log {latestTag}..HEAD --pretty=format:%s");

            if (!success)
            {
                Debug.LogError($"Failed to get commit messages: {error}");
            }

            return output;
        }

        public static string GetLatestSemanticVersionTag()
        {
            // Primeiro sincroniza as tags do remote
            var (fetchOutput, fetchError, fetchSuccess) = ExecuteGitCommand("fetch --tags --prune");
            if (!fetchSuccess)
            {
                Debug.LogError($"Failed to fetch tags from remote: {fetchError}");
                // Continua mesmo com erro (pode ser offline)
            }

            // Agora lista tags (já incluindo as do remote)
            var (tags, error, success) = ExecuteGitCommand("tag --sort=-creatordate");
            if (!success)
            {
                Debug.LogError($"Failed to get tags: {error}");
                return string.Empty;
            }

            if (string.IsNullOrEmpty(tags)) return string.Empty;

            Regex semVerRegex = new(@"^v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+(?<build>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$");

            foreach (string tag in tags.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (semVerRegex.IsMatch(tag))
                {
                    return tag;
                }
            }

            return string.Empty;
        }

        public static bool CreateAndPushVersionTag(string version)
        {
            if (!IsValidVersion(version))
            {
                Debug.LogError($"Invalid version format: {version}. Expected format: X.Y.Z");
                return false;
            }

            string tagName = $"v{version}";

            // Create tag locally
            var (createOutput, createError, createSuccess) = ExecuteGitCommand($"tag -a {tagName} -m \"Version {version}\"");
            if (!createSuccess)
            {
                Debug.LogError($"Failed to create tag {tagName}: {createError}");
                return false;
            }

            // Push tag to remote
            var (pushOutput, pushError, pushSuccess) = ExecuteGitCommand($"push origin {tagName}");
            if (!pushSuccess)
            {
                Debug.LogError($"Failed to push tag {tagName}: {pushError}");
                return false;
            }

            Debug.Log($"Successfully created and pushed tag {tagName}");
            return true;
        }

        public static bool CommitChanges(string message)
        {
            // Stage all changes
            var (addOutput, addError, addSuccess) = ExecuteGitCommand("add .");
            if (!addSuccess)
            {
                Debug.LogError($"Failed to stage changes: {addError}");
                return false;
            }

            // Create commit
            var (commitOutput, commitError, commitSuccess) = ExecuteGitCommand($"commit -m \"{message}\"");
            if (!commitSuccess)
            {
                Debug.LogError($"Failed to create commit: {commitError}");
                return false;
            }

            Debug.Log($"Changes committed: {message}");
            return true;
        }

        /// <summary>
        /// Stashes all current uncommitted changes (staged and unstaged) in the working directory.
        /// This effectively "removes" them from the working directory by saving them to a stash entry,
        /// allowing you to temporarily clean your working directory.
        /// </summary>
        /// <param name="message">An optional message for the stash entry.</param>
        /// <returns>True if the changes were successfully stashed or if there were no changes to stash, false otherwise.</returns>
        public static bool StashCurrentChanges(string message = "")
        {
            // Construct the git command arguments for stashing changes.
            // If a message is provided, it's included with the -m flag.
            string arguments = string.IsNullOrEmpty(message) ? "stash push" : $"stash push -m \"{message}\"";

            // Execute the git command and capture its output, error, and success status.
            var (output, error, success) = ExecuteGitCommand(arguments);

            // Check if the command was not successful.
            if (!success)
            {
                // If the error message indicates "No local changes to save", it means the working directory
                // was already clean, which can be considered a successful outcome for the user's intent
                // of "removing" (i.e., cleaning) changes from the working directory.
                if (error.Contains("No local changes to save"))
                {
                    Debug.Log("No local changes to stash. Working directory is already clean.");
                    return true; // Indicate success as there was nothing to do.
                }

                // For any other error, log it as a failure to stash changes.
                Debug.LogError($"Failed to stash current changes: {error}");
                return false;
            }

            // If the command was successful, log a success message along with the output from git.
            Debug.Log($"Successfully stashed current changes.\nOutput: {output}");
            return true;
        }

        /// <summary>
        /// Discards all current uncommitted changes (staged and unstaged) and removes
        /// untracked files and directories from the working directory.
        /// This operation is irreversible and will permanently delete local modifications.
        /// </summary>
        /// <returns>True if all changes were successfully discarded, false otherwise.</returns>
        public static bool DiscardAllChanges()
        {
            // First, discard changes to tracked files (staged and unstaged).
            // 'git reset --hard HEAD' redefines the HEAD to the last commit, discarding any changes.
            var (resetOutput, resetError, resetSuccess) = ExecuteGitCommand("reset --hard HEAD");
            if (!resetSuccess)
            {
                Debug.LogError($"Failed to discard tracked changes: {resetError}");
                return false;
            }
            Debug.Log($"Tracked changes discarded successfully.\nOutput: {resetOutput}");

            // Then, remove untracked files and directories.
            // 'git clean -fd' removes untracked files (-f to force, -d to include directories).
            var (cleanOutput, cleanError, cleanSuccess) = ExecuteGitCommand("clean -fd");
            if (!cleanSuccess)
            {
                Debug.LogError($"Failed to remove untracked files: {cleanError}");
                return false;
            }
            Debug.Log($"Untracked files removed successfully.\nOutput: {cleanOutput}");

            Debug.Log("All changes in the file system have been completely discarded.");
            return true;
        }

        private static bool IsValidVersion(string version)
        {
            return Regex.IsMatch(version, @"^\d+\.\d+\.\d+$");
        }
    }
}