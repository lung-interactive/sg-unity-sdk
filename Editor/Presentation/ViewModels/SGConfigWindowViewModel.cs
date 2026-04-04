using HMSUnitySDK;
using SGUnitySDK.Editor.Core.Singletons;
using UnityEditor;

namespace SGUnitySDK.Editor.Presentation.ViewModels
{
    /// <summary>
    /// ViewModel that exposes SG editor configuration values and mutations
    /// for config-related presentation windows.
    /// </summary>
    public sealed class SGConfigWindowViewModel
    {
        /// <summary>
        /// Gets the current game development token.
        /// </summary>
        public string GameDevelopmentToken => SGEditorConfig.instance.GameDevelopmentToken;

        /// <summary>
        /// Gets a value indicating whether base URL override is enabled.
        /// </summary>
        public bool ShouldOverrideBaseURL => SGEditorConfig.instance.ShouldOverrideBaseURL;

        /// <summary>
        /// Gets the configured base URL override string.
        /// </summary>
        public string BaseURLOverride => SGEditorConfig.instance.BaseURLOverride;

        /// <summary>
        /// Gets the configured runtime profile reference.
        /// </summary>
        public HMSRuntimeProfile RuntimeProfile => SGEditorConfig.instance.RuntimeProfile;

        /// <summary>
        /// Gets the configured builds output directory.
        /// </summary>
        public string BuildsDirectory => SGEditorConfig.instance.BuildsDirectory;

        /// <summary>
        /// Creates a serialized wrapper for config list binding.
        /// </summary>
        /// <returns>Serialized object for SGEditorConfig singleton instance.</returns>
        public SerializedObject CreateSerializedConfig()
        {
            return new SerializedObject(SGEditorConfig.instance);
        }

        /// <summary>
        /// Updates the game development token.
        /// </summary>
        /// <param name="value">New token value.</param>
        public void SetGameDevelopmentToken(string value)
        {
            SGEditorConfig.instance.SetGameDevelopmentToken(value);
        }

        /// <summary>
        /// Updates base URL override enabled state.
        /// </summary>
        /// <param name="value">Whether base URL override should be enabled.</param>
        public void SetShouldOverrideBaseURL(bool value)
        {
            SGEditorConfig.instance.SetShouldOverrideBaseURL(value);
        }

        /// <summary>
        /// Updates base URL override value.
        /// </summary>
        /// <param name="value">New base URL override string.</param>
        public void SetBaseURLOverride(string value)
        {
            SGEditorConfig.instance.SetBaseUrlOverride(value);
        }

        /// <summary>
        /// Updates configured runtime profile.
        /// </summary>
        /// <param name="runtimeProfile">Runtime profile reference.</param>
        public void SetRuntimeProfile(HMSRuntimeProfile runtimeProfile)
        {
            SGEditorConfig.instance.SetRuntimeProfile(runtimeProfile);
        }

        /// <summary>
        /// Updates configured builds directory path.
        /// </summary>
        /// <param name="buildsDirectory">Directory path.</param>
        public void SetBuildsDirectory(string buildsDirectory)
        {
            SGEditorConfig.instance.SetBuildsDirectory(buildsDirectory);
        }

        /// <summary>
        /// Persists current config values to disk.
        /// </summary>
        public void Persist()
        {
            SGEditorConfig.instance.Persist();
        }
    }
}