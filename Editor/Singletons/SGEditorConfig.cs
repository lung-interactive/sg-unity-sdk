using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.IO;

namespace SGUnitySDK.Editor
{
    [FilePath("SGUnitySDK/SGEditorConfig", FilePathAttribute.Location.PreferencesFolder)]
    public partial class SGEditorConfig : ScriptableSingleton<SGEditorConfig>
    {
        private static readonly string API_BASE_URL = "https://streaminggames.io/v1";

        [SerializeField] private string _gmt;
        [SerializeField] private bool _shouldOverrideBaseUrl = false;
        [SerializeField] private string _baseUrlOverride;
        [SerializeField] private string _buildsDirectory = DefaultBuildsDirectory();
        [SerializeField] private List<SGBuildSetup> _buildSetups;

        public string ApiBaseURL => !_shouldOverrideBaseUrl ? API_BASE_URL : _baseUrlOverride;
        public bool ShouldOverrideBaseURL => _shouldOverrideBaseUrl;
        public string BaseURLOverride => _baseUrlOverride;
        public string GMT => _gmt;
        public string BuildsDirectory => _buildsDirectory;
        public List<SGBuildSetup> BuildSetups => _buildSetups;

        public bool IsGMTValid => !string.IsNullOrEmpty(_gmt);

        #region Editor Methods

        // Mantém os demais métodos inalterados
        public void SetShouldOverrideBaseURL(bool value)
        {
            if (_shouldOverrideBaseUrl != value)
            {
                _shouldOverrideBaseUrl = value;
                Persist();
            }
        }

        public void SetGTM(string value)
        {
            if (_gmt != value)
            {
                _gmt = value;
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
            _gmt = string.Empty;
            _buildsDirectory = DefaultBuildsDirectory();
            _buildSetups = new List<SGBuildSetup>();
            Persist();
        }

        public void ResetVersioningProcess()
        {
            Persist();
        }

        private static string DefaultBuildsDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SGUnitySDKBuilds"));
        }

        #endregion

        [MenuItem("Tools/SGUnitySDK/Config/Reset Config")]
        private static void StaticResetConfig()
        {
            instance.ResetConfig();
        }

        // [MenuItem("Tools/SGUnitySDK/Config/Reset Versioning Process")]
        private static void StaticResetVersioningProcess()
        {
            instance.ResetVersioningProcess();
        }
    }
}