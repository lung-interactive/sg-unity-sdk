using System;
using System.Collections.Generic;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Singletons;

namespace SGUnitySDK.Editor.Presentation.ViewModels
{
    /// <summary>
    /// Read/write state facade for the development process singleton.
    /// Centralizes state access for presentation components.
    /// </summary>
    public class DevelopmentProcessStateViewModel
    {
        private readonly DevelopmentProcess _process;

        /// <summary>
        /// Raised when the development process step changes.
        /// </summary>
        public event Action<DevelopmentStep> StepChanged;

        /// <summary>
        /// Raised when local builds collection changes.
        /// </summary>
        public event Action LocalBuildsChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="DevelopmentProcessStateViewModel"/> class.
        /// </summary>
        public DevelopmentProcessStateViewModel()
        {
            _process = DevelopmentProcess.instance;
            _process.StepChanged += OnStepChanged;
            _process.LocalBuildsChanged += OnLocalBuildsChanged;
        }

        /// <summary>
        /// Gets the underlying process model used by legacy elements.
        /// </summary>
        public DevelopmentProcess Process => _process;

        /// <summary>
        /// Gets the current step.
        /// </summary>
        public DevelopmentStep CurrentStep => _process.CurrentStep;

        /// <summary>
        /// Gets the current version under development.
        /// </summary>
        public VersionDTO CurrentVersion => _process.CurrentVersion;

        /// <summary>
        /// Gets the current version metadata.
        /// </summary>
        public VersionMetadata CurrentVersionMetadata => _process.CurrentVersionMetadata;

        /// <summary>
        /// Gets whether there is a current version selected.
        /// </summary>
        /// <returns>True when a version exists; otherwise false.</returns>
        public bool HasCurrentVersion()
        {
            return _process.CurrentVersion != null;
        }

        /// <summary>
        /// Gets whether the process is currently in Development step.
        /// </summary>
        /// <returns>True when in Development step; otherwise false.</returns>
        public bool IsDevelopmentStep()
        {
            return _process.CurrentStep == DevelopmentStep.Development;
        }

        /// <summary>
        /// Sets the process step.
        /// </summary>
        /// <param name="step">Step to set.</param>
        public void SetStep(DevelopmentStep step)
        {
            _process.SetStep(step);
        }

        /// <summary>
        /// Sets current version data in process state.
        /// </summary>
        /// <param name="version">Version payload.</param>
        public void SetCurrentVersion(VersionDTO version)
        {
            _process.CurrentVersion = version;
        }

        /// <summary>
        /// Sets current version metadata in process state.
        /// </summary>
        /// <param name="metadata">Version metadata payload.</param>
        public void SetCurrentVersionMetadata(VersionMetadata metadata)
        {
            _process.CurrentVersionMetadata = metadata;
        }

        /// <summary>
        /// Replaces the full build entries list.
        /// </summary>
        /// <param name="entries">Build entries list.</param>
        public void SetVersionBuilds(List<SGVersionBuildEntry> entries)
        {
            _process.VersionBuilds = entries ?? new List<SGVersionBuildEntry>();
        }

        /// <summary>
        /// Replaces one build entry by index.
        /// </summary>
        /// <param name="index">Index of the entry to replace.</param>
        /// <param name="entry">Updated build entry.</param>
        public void ReplaceVersionBuild(int index, SGVersionBuildEntry entry)
        {
            _process.ReplaceVersionBuild(index, entry);
        }

        /// <summary>
        /// Moves process to Development step.
        /// </summary>
        public void MoveToDevelopmentStep()
        {
            _process.SetStep(DevelopmentStep.Development);
        }

        /// <summary>
        /// Gets current build entries, ensuring a non-null list.
        /// </summary>
        /// <returns>Current build entries list.</returns>
        public List<SGVersionBuildEntry> GetVersionBuildsOrEmpty()
        {
            return _process.VersionBuilds ?? new List<SGVersionBuildEntry>();
        }

        /// <summary>
        /// Checks whether all current builds are uploaded.
        /// </summary>
        /// <returns>True when there are builds and all are uploaded.</returns>
        public bool AreAllBuildsUploaded()
        {
            var builds = _process.VersionBuilds;
            if (builds == null || builds.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < builds.Count; i++)
            {
                if (!builds[i].uploaded)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether homologation was requested and the backend has not
        /// yet transitioned to a homologation/terminal step.
        /// </summary>
        /// <returns>True when homologation request is pending.</returns>
        public bool HasPendingHomologationRequest()
        {
            if (!_process.HomologationRequestedAt.HasValue)
            {
                return false;
            }

            return _process.CurrentStep != DevelopmentStep.Homologation &&
                   _process.CurrentStep != DevelopmentStep.Approved &&
                   _process.CurrentStep != DevelopmentStep.Canceled;
        }

        private void OnStepChanged(DevelopmentStep step)
        {
            StepChanged?.Invoke(step);
        }

        private void OnLocalBuildsChanged()
        {
            LocalBuildsChanged?.Invoke();
        }
    }
}