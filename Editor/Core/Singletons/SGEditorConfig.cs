using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.IO;
using HMSUnitySDK;
using SGUnitySDK.Editor.Core.Entities;
using UnityEngine.Serialization;

namespace SGUnitySDK.Editor.Core.Singletons
{
    [FilePath("UserSettings/SGUnitySDK/SGEditorConfig.asset", FilePathAttribute.Location.ProjectFolder)]
    public partial class SGEditorConfig : ScriptableSingleton<SGEditorConfig>
    {
        private static readonly string API_BASE_URL = "https://api.lung-sg.com/v1";

        [SerializeField] private string _gameDevelopmentToken;
        [SerializeField] private bool _shouldOverrideBaseUrl = false;
        [SerializeField] private string _baseUrlOverride;
        [SerializeField] private HMSRuntimeProfile _runtimeProfile;
        [SerializeField] private string _buildsDirectory = DefaultBuildsDirectory();
        [SerializeField, FormerlySerializedAs("_buildSetups")]
        private List<SGBuildSetup> _clientBuildSetups = new();
        [SerializeField] private List<SGBuildSetup> _serverBuildSetups = new();

        public string ApiBaseURL => !_shouldOverrideBaseUrl ? API_BASE_URL : _baseUrlOverride;
        public bool ShouldOverrideBaseURL => _shouldOverrideBaseUrl;
        public string BaseURLOverride => _baseUrlOverride;
        public HMSRuntimeProfile RuntimeProfile => _runtimeProfile;
        public string GameDevelopmentToken => _gameDevelopmentToken;
        public string BuildsDirectory => _buildsDirectory;
        public List<SGBuildSetup> ClientBuildSetups =>
            _clientBuildSetups ??= new List<SGBuildSetup>();
        public List<SGBuildSetup> ServerBuildSetups =>
            _serverBuildSetups ??= new List<SGBuildSetup>();
        public List<SGBuildSetup> BuildSetups =>
            ServerBuildSetups.Concat(ClientBuildSetups).ToList();

        public bool IsGMTValid => !string.IsNullOrEmpty(_gameDevelopmentToken);

        #region Editor Methods

        public void SetShouldOverrideBaseURL(bool value)
        {
            if (_shouldOverrideBaseUrl != value)
            {
                _shouldOverrideBaseUrl = value;
                Persist();
            }
        }

        public void SetGameDevelopmentToken(string value)
        {
            if (_gameDevelopmentToken != value)
            {
                _gameDevelopmentToken = value;
                Persist();
            }
        }

        public void SetBaseUrlOverride(string value)
        {
            if (_baseUrlOverride != value)
            {
                _baseUrlOverride = value;
                Persist();
            }
        }

        public void SetRuntimeProfile(HMSRuntimeProfile value)
        {
            if (_runtimeProfile != value)
            {
                _runtimeProfile = value;
                Persist();
            }
        }

        public void SetBuildsDirectory(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                _buildsDirectory = DefaultBuildsDirectory();
                Persist();
            }

            if (_buildsDirectory != value)
            {
                _buildsDirectory = value;
                Persist();
            }
        }

        public void Persist()
        {
            Save(true);
        }

        public void ResetConfig()
        {
            _shouldOverrideBaseUrl = false;
            _baseUrlOverride = string.Empty;
            _gameDevelopmentToken = string.Empty;
            _buildsDirectory = DefaultBuildsDirectory();
            _clientBuildSetups = new List<SGBuildSetup>();
            _serverBuildSetups = new List<SGBuildSetup>();
            Persist();
        }

        public void ResetVersioningProcess()
        {
            Persist();
        }

        private static string DefaultBuildsDirectory()
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "SGUnitySDK",
                "Builds"
            ));
        }

        #endregion

        [MenuItem("SGUnitySDK/Config/Reset")]
        private static void StaticResetConfig()
        {
            instance.ResetConfig();
        }

        // [MenuItem("SGUnitySDK/Config/Reset Versioning Process")]
        private static void StaticResetVersioningProcess()
        {
            instance.ResetVersioningProcess();
        }
    }
}