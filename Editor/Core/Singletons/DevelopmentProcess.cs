using System.Collections.Generic;
using System.IO;
using SGUnitySDK.Editor.Versioning;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using SGUnitySDK.Editor.Core.Entities;

namespace SGUnitySDK.Editor.Core.Singletons
{
    /// <summary>
    /// Manages the development process for game versions in the Unity Editor.
    /// Tracks the current step, target version, remote state, and build entries.
    /// Persists data as a ScriptableSingleton for session continuity.
    /// </summary>
    [FilePath("SGUnitySDK/SGDevelopmentProcess", FilePathAttribute.Location.ProjectFolder)]
    public class DevelopmentProcess : ScriptableSingleton<DevelopmentProcess>
    {
        [SerializeField]
        private DevelopmentStep _currentStep = DevelopmentStep.AcceptVersion;

        [SerializeField]
        private string _targetVersionString = null;

        [SerializeField]
        private bool _startedInRemote = false;

        // NEW: semver da versão aberta no backend (Start Version In Remote)
        [SerializeField]
        private string _remoteSemver = null;

        // Current version in development
        [SerializeField]
        private string _currentVersionJson = null;

        // Metadata for the current version
        [SerializeField]
        private string _currentVersionMetadataJson = null;

        [SerializeField]
        private List<SGVersionBuildEntry> _versionBuilds = new();

        public event UnityAction<DevelopmentStep> StepChanged;
        public event UnityAction<SemVerType> TargetVersionDefined;
        public event UnityAction ProcessEnded;

        /// <summary>
        /// Event triggered when local build entries change.
        /// Kept for compatibility: signifies 'VersionBuilds changed'.
        /// </summary>
        public event UnityAction LocalBuildsChanged;

        /// <summary>
        /// Gets the current step in the development process.
        /// </summary>
        public DevelopmentStep CurrentStep => _currentStep;

        /// <summary>
        /// Gets or sets whether the version has been started in the remote system.
        /// </summary>
        public bool StartedInRemote
        {
            get => _startedInRemote;
            set { _startedInRemote = value; Persist(); }
        }

        /// <summary>
        /// Gets or sets the semantic version string from the remote system.
        /// </summary>
        public string RemoteSemver
        {
            get => _remoteSemver;
            set { _remoteSemver = value; Persist(); }
        }

        /// <summary>
        /// Gets or sets the current version in development.
        /// </summary>
        public VersionDTO CurrentVersion
        {
            get => string.IsNullOrEmpty(_currentVersionJson) ? null : JsonConvert.DeserializeObject<VersionDTO>(_currentVersionJson);
            set
            {
                _currentVersionJson = value == null ? null : JsonConvert.SerializeObject(value);
                Persist();
            }
        }

        /// <summary>
        /// Gets or sets the metadata for the current version.
        /// </summary>
        public VersionMetadata CurrentVersionMetadata
        {
            get => string.IsNullOrEmpty(_currentVersionMetadataJson) ? null : JsonConvert.DeserializeObject<VersionMetadata>(_currentVersionMetadataJson);
            set
            {
                _currentVersionMetadataJson = value == null ? null : JsonConvert.SerializeObject(value);
                Persist();
            }
        }

        /// <summary>
        /// Gets or sets the target semantic version for the development process.
        /// </summary>
        public SemVerType TargetVersion
        {
            get => string.IsNullOrEmpty(_targetVersionString)
                ? null
                : SemVerType.From(_targetVersionString);
            set
            {
                if (value == null)
                {
                    _targetVersionString = null;
                    TargetVersionDefined?.Invoke(null);
                    Persist();
                    return;
                }
                _targetVersionString = value.Raw;
                TargetVersionDefined?.Invoke(value);
                Persist();
            }
        }

        /// <summary>
        /// Gets or sets the list of version build entries for the current process.
        /// </summary>
        public List<SGVersionBuildEntry> VersionBuilds
        {
            get => _versionBuilds;
            set
            {
                _versionBuilds = value ?? new List<SGVersionBuildEntry>();
                LocalBuildsChanged?.Invoke();
                Persist();
            }
        }

        /// <summary>
        /// Clears the list of version builds and optionally deletes associated files.
        /// </summary>
        /// <param name="deleteFiles">If true, deletes the builds directory and its contents.</param>
        public void ClearVersionBuilds(bool deleteFiles = true)
        {
            _versionBuilds.Clear();
            if (deleteFiles)
            {
                string path = SGEditorConfig.instance.BuildsDirectory;
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            LocalBuildsChanged?.Invoke();
            Persist();
        }

        /// <summary>
        /// Replaces a build entry at the specified index.
        /// </summary>
        /// <param name="index">The index of the build entry to replace.</param>
        /// <param name="entry">The new build entry to set.</param>
        public void ReplaceVersionBuild(int index, SGVersionBuildEntry entry)
        {
            if (index < 0 || index >= _versionBuilds.Count) return;
            _versionBuilds[index] = entry;
            LocalBuildsChanged?.Invoke();
            Persist();
        }

        /// <summary>
        /// Persists the current state to disk.
        /// </summary>
        public void Persist() => Save(true);

        /// <summary>
        /// Sets the current step to a specific value.
        /// </summary>
        /// <param name="step">The step to set.</param>
        public void SetStep(DevelopmentStep step)
        {
            _currentStep = step;

            // When entering active development, clear any previous build records
            // to ensure the build registry reflects the new development lifecycle.
            if (_currentStep == DevelopmentStep.Development)
            {
                ClearVersionBuilds(true);
            }

            StepChanged?.Invoke(_currentStep);
            Persist();
        }

        /// <summary>
        /// Advances to the next step in the development process.
        /// </summary>
        public void AdvanceStep()
        {
            if (_currentStep == DevelopmentStep.Homologation)
            {
                Debug.LogWarning("Already at the last step of the process (Homologation). Cannot advance further.");
                return;
            }
            _currentStep = (DevelopmentStep)((int)_currentStep + 1);
            StepChanged?.Invoke(_currentStep);
            Persist();
        }

        /// <summary>
        /// Goes back to the previous step in the development process.
        /// </summary>
        public void RetrocedeStep()
        {
            if (_currentStep == DevelopmentStep.AcceptVersion)
            {
                Debug.LogWarning("Already at the first step of the process (Accept Version). Cannot go back further.");
                return;
            }
            _currentStep = (DevelopmentStep)((int)_currentStep - 1);
            StepChanged?.Invoke(_currentStep);
            Persist();
        }

        /// <summary>
        /// Ends the current development process and resets to the initial state.
        /// </summary>
        public void EndProcess()
        {
            ResetProcess();
            ProcessEnded?.Invoke();
        }

        /// <summary>
        /// Resets the development process to its initial state.
        /// </summary>
        public void ResetProcess()
        {
            _currentStep = DevelopmentStep.AcceptVersion;
            StepChanged?.Invoke(_currentStep);
            _targetVersionString = null;
            _remoteSemver = null;
            _startedInRemote = false;
            _currentVersionJson = null;
            _currentVersionMetadataJson = null;
            // Clear build entries and delete associated files when resetting the process.
            ClearVersionBuilds(true);
        }
    }

    /// <summary>
    /// Defines the steps in the game development process.
    /// </summary>
    public enum DevelopmentStep
    {
        /// <summary>
        /// Step to accept the version released by the admin team.
        /// </summary>
        AcceptVersion = 0,

        /// <summary>
        /// Step for active development work, including generating and uploading builds.
        /// </summary>
        Development = 1,

        /// <summary>
        /// Step to send the version to homologation for admin review and approval/rejection.
        /// </summary>
        Homologation = 2,

        /// <summary>
        /// Version has been approved by the release authority and is
        /// considered finalized for release preparations or deployment.
        /// This step typically represents a post-homologation acceptance.
        /// </summary>
        Approved = 3,

        /// <summary>
        /// Version (or process) was canceled and no further work will be
        /// performed on this version. Use to signal termination of the flow
        /// without release.
        /// </summary>
        Canceled = 4,
    }
}
