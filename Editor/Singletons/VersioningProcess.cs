using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace SGUnitySDK.Editor
{
    [FilePath("SGUnitySDK/SGVersioningProcess", FilePathAttribute.Location.PreferencesFolder)]
    public class VersioningProcess : ScriptableSingleton<VersioningProcess>
    {
        [SerializeField]
        private VersioningStep _currentStep = VersioningStep.DefineTargetVersion;

        [SerializeField]
        private string _targetVersionString = null;

        [SerializeField]
        private bool _startedInRemote = false;

        [SerializeField]
        private List<SGLocalBuildResult> _buildResults = new();

        public event UnityAction<VersioningStep> StepChanged;
        public event UnityAction<SemVerType> TargetVersionDefined;
        public event UnityAction LocalBuildsChanged;

        public VersioningStep CurrentStep => _currentStep;

        public bool StartedInRemote
        {
            get => _startedInRemote;
            set
            {
                _startedInRemote = value;
            }
        }

        public SemVerType TargetVersion
        {
            get => string.IsNullOrEmpty(_targetVersionString) ? null : SemVerType.From(_targetVersionString);
            set
            {
                if (value == null)
                {
                    _targetVersionString = null;
                    TargetVersionDefined?.Invoke(null);
                    return;
                }
                _targetVersionString = value.Raw;
                TargetVersionDefined?.Invoke(value);
                Persist();
            }
        }

        public List<SGLocalBuildResult> LocalBuilds
        {
            get => _buildResults;
            set
            {
                _buildResults = value;
                LocalBuildsChanged?.Invoke();
                Persist();
            }
        }

        public void ClearLocalBuilds()
        {
            _buildResults.Clear();
            Persist();
        }

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
            Persist();
        }

        public void ResetProcess()
        {
            _currentStep = VersioningStep.DefineTargetVersion;
            StepChanged?.Invoke(_currentStep);

            _targetVersionString = null;
            _startedInRemote = false;
            _buildResults.Clear();
            Persist();
        }

        public void Persist()
        {
            Save(true);
        }
    }

    public enum VersioningStep
    {
        DefineTargetVersion = 0,
        StartVersionInRemote = 1,
        GenerateBuilds = 2,
        SendBuildsToRemote = 3,
        CloseVersion = 4,
    }
}