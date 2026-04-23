using System;
using System.Collections.Generic;
using System.IO;
using HMSUnitySDK;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SGUnitySDK.Initialization
{
    /// <summary>
    /// HMS service responsible for loading launcher initialization settings
    /// and exposing pages through the global locator.
    /// </summary>
    [HMSBuildRoles(HMSRuntimeRole.Client, HMSRuntimeRole.LaunchedClient)]
    public sealed class InitSettingsService : MonoBehaviour, IHMSService
    {
        private readonly Dictionary<string, InitSettingsPage> _pages =
            new Dictionary<string, InitSettingsPage>(
                StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the display name used for the service GameObject.
        /// </summary>
        public string ServiceObjectName => "SG InitSettings";

        /// <summary>
        /// Validates whether this service should be registered.
        /// </summary>
        /// <returns>Always true for this service.</returns>
        public bool ValidateService()
        {
            return true;
        }

        /// <summary>
        /// Initializes the service by loading launcher.config data.
        /// </summary>
        public void InitializeService()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// Checks whether a root page exists in loaded settings.
        /// </summary>
        /// <param name="pageName">Root page name.</param>
        /// <returns>True when the page is present.</returns>
        public bool HasPage(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName))
            {
                return false;
            }

            return _pages.ContainsKey(pageName);
        }

        /// <summary>
        /// Gets a snapshot of all loaded root page names.
        /// </summary>
        /// <returns>Collection containing loaded page names.</returns>
        public IReadOnlyCollection<string> GetPageNames()
        {
            return new List<string>(_pages.Keys);
        }

        /// <summary>
        /// Gets a page wrapper by root page name.
        /// </summary>
        /// <param name="pageName">Root page name.</param>
        /// <returns>
        /// Matching page wrapper, or an empty wrapper when page does not
        /// exist.
        /// </returns>
        public InitSettingsPage GetPage(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName))
            {
                return InitSettingsPage.Empty(pageName);
            }

            return _pages.TryGetValue(pageName, out var page)
                ? page
                : InitSettingsPage.Empty(pageName);
        }

        /// <summary>
        /// Loads launcher.config from disk and stores each root object as
        /// a configuration page.
        /// </summary>
        private void LoadConfiguration()
        {
            _pages.Clear();

            var configPath = ResolveConfigPath();
            if (string.IsNullOrWhiteSpace(configPath))
            {
                SGLogger.LogWarning(S.Warnings.ConfigPathMissing);
                return;
            }

            if (!File.Exists(configPath))
            {
                SGLogger.LogWarning(string.Format(
                    S.Warnings.ConfigNotFound,
                    configPath));
                return;
            }

            try
            {
                var json = File.ReadAllText(configPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    SGLogger.LogWarning(string.Format(
                        S.Warnings.ConfigEmpty,
                        configPath));
                    return;
                }

                var root = JObject.Parse(json);
                foreach (var property in root.Properties())
                {
                    if (property.Value is JObject pageObject)
                    {
                        _pages[property.Name] = new InitSettingsPage(
                            property.Name,
                            pageObject);
                    }
                }

                SGLogger.Log(string.Format(
                    S.Logs.ConfigLoaded,
                    _pages.Count,
                    configPath));
            }
            catch (Exception ex)
            {
                SGLogger.LogWarning(string.Format(
                    S.Warnings.ConfigParseFailed,
                    configPath,
                    ex.Message));
            }
        }

        /// <summary>
        /// Resolves launcher.config path for current runtime mode.
        /// </summary>
        /// <returns>Absolute path to launcher.config.</returns>
        private static string ResolveConfigPath()
        {
            var runtimeInfo = HMSRuntimeInfo.Get();
            if (runtimeInfo != null && runtimeInfo.IsEditorRuntime)
            {
                return ResolveEditorConfigPath();
            }

            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                S.Files.LauncherConfigName);
        }

        /// <summary>
        /// Resolves launcher.config path for editor runtime based on
        /// SGUnitySDK project root directory.
        /// </summary>
        /// <returns>Absolute path to launcher.config in SGUnitySDK root.</returns>
        private static string ResolveEditorConfigPath()
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                S.Files.EditorConfigDirectory,
                S.Files.LauncherConfigName
            ));
        }

        /// <summary>
        /// Constants used by service logging and file resolution.
        /// </summary>
        private static class S
        {
            internal static class Files
            {
                internal const string LauncherConfigName = "launcher.config";
                internal const string EditorConfigDirectory = "SGUnitySDK";
            }

            internal static class Warnings
            {
                internal const string ConfigPathMissing =
                    "Failed to resolve launcher.config path. Continuing with " +
                    "empty initialization settings.";

                internal const string ConfigNotFound =
                    "launcher.config was not found at '{0}'. Continuing with " +
                    "empty initialization settings.";

                internal const string ConfigEmpty =
                    "launcher.config at '{0}' is empty. Continuing with empty " +
                    "initialization settings.";

                internal const string ConfigParseFailed =
                    "Failed to parse launcher.config at '{0}': {1}. Continuing " +
                    "with empty initialization settings.";
            }

            internal static class Logs
            {
                internal const string ConfigLoaded =
                    "Loaded launcher.config with {0} page(s) from '{1}'.";
            }
        }
    }
}