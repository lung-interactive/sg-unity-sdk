using System.Collections.Generic;
using System.IO;
using SGUnitySDK.Editor.Versioning;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace SGUnitySDK.Editor
{
    [FilePath("SGUnitySDK/SGVersioningProcess", FilePathAttribute.Location.ProjectFolder)]
    public class VersioningProcess : ScriptableSingleton<VersioningProcess>
    {
        [SerializeField]
        private VersioningStep _currentStep = VersioningStep.DefineTargetVersion;

        [SerializeField]
        private string _targetVersionString = null;

        [SerializeField]
        private bool _startedInRemote = false;

        // NEW: semver da versão aberta no backend (Start Version In Remote)
        [SerializeField]
        private string _remoteSemver = null;

        // Agora guardamos (build + upload state)
        [SerializeField]
        private List<SGVersionBuildEntry> _versionBuilds = new();

        public event UnityAction<VersioningStep> StepChanged;
        public event UnityAction<SemVerType> TargetVersionDefined;
        public event UnityAction ProcessEnded;

        /// Mantemos o nome do evento por compat: significa "VersionBuilds changed".
        public event UnityAction LocalBuildsChanged;

        public VersioningStep CurrentStep => _currentStep;

        public bool StartedInRemote
        {
            get => _startedInRemote;
            set { _startedInRemote = value; Persist(); }
        }

        public string RemoteSemver
        {
            get => _remoteSemver;
            set { _remoteSemver = value; Persist(); }
        }

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

        public void ReplaceVersionBuild(int index, SGVersionBuildEntry entry)
        {
            if (index < 0 || index >= _versionBuilds.Count) return;
            _versionBuilds[index] = entry;
            LocalBuildsChanged?.Invoke();
            Persist();
        }

        public void Persist() => Save(true);

        public void AdvanceStep()
        {
            if (_currentStep == VersioningStep.CloseVersion)
            {
                Debug.LogWarning("Já estamos no último passo do processo (Deploy). Não é possível avançar mais.");
                return;
            }
            _currentStep = (VersioningStep)((int)_currentStep + 1);
            StepChanged?.Invoke(_currentStep);
            Persist();
        }

        public void RetrocedeStep()
        {
            if (_currentStep == VersioningStep.DefineTargetVersion)
            {
                Debug.LogWarning("Já estamos no primeiro passo do processo (Define Target Version). Não é possível retroceder mais.");
                return;
            }
            _currentStep = (VersioningStep)((int)_currentStep - 1);
            StepChanged?.Invoke(_currentStep);
            Persist();
        }

        public void EndProcess()
        {
            ResetProcess();
            ProcessEnded?.Invoke();
        }

        public void ResetProcess()
        {
            _currentStep = VersioningStep.DefineTargetVersion;
            StepChanged?.Invoke(_currentStep);
            _targetVersionString = null;
            _remoteSemver = null;
            _startedInRemote = false;
            _versionBuilds.Clear();
            Persist();
        }
    }

    public enum VersioningStep
    {
        DefineTargetVersion = 0,
        StartVersionInRemote = 1,
        Builds = 2,
        CloseVersion = 3,
    }
}
