using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public class DefineTargetVersionStepElement : VersioningStepElement
    {
        private static readonly string TemplateName = "SGProcessStep1_DefineTargetVersion";
        public override VersioningStep Step => VersioningStep.DefineTargetVersion;

        private readonly TemplateContainer _containerMain;
        private readonly VisualElement _containerSateDefining;
        private readonly EnumField _fieldIncrementVersionType;
        private readonly TextField _fieldCustomVersion;
        private readonly Button _buttonDefine;
        private readonly VisualElement _containerSateResult;
        private readonly Label _labelTargetVersion;
        private readonly Button _buttonRollback;

        public DefineTargetVersionStepElement()
        {
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            _containerSateDefining = _containerMain.Q<VisualElement>("container-state-defining");

            int incrementTypeInt = EditorPrefs.GetInt("DefineTargetVersionStepElement_IncrementType", 1);
            _fieldIncrementVersionType = _containerMain.Q<EnumField>("field-increment-version-type");
            _fieldIncrementVersionType.SetValueWithoutNotify((VersionUpdateType)incrementTypeInt);
            _fieldIncrementVersionType.RegisterValueChangedCallback(OnIncrementVersionTypeChanged);

            _fieldCustomVersion = _containerMain.Q<TextField>("field-custom-version");

            _buttonDefine = _containerMain.Q<Button>("button-define");
            _buttonDefine.clicked += OnDefineButtonClicked;

            _containerSateResult = _containerMain.Q<VisualElement>("container-state-result");
            _labelTargetVersion = _containerMain.Q<Label>("label-target-version");

            _buttonRollback = _containerMain.Q<Button>("button-rollback");
            _buttonRollback.clicked += OnButtonRollbackClicked;

            Add(_containerMain);
        }

        public override void Activate(VersioningProcess process)
        {
            base.Activate(process);
            var targetVersion = _process.TargetVersion;
            var targetVersionIsDefined = targetVersion != null;
            if (targetVersionIsDefined)
            {
                _labelTargetVersion.text = _process.TargetVersion.Raw;
            }
            ArrangeFields(targetVersionIsDefined);
            SetReadyStatus(targetVersionIsDefined);
        }

        public override void Deactivate()
        {
            base.Deactivate();
        }

        private void OnIncrementVersionTypeChanged(ChangeEvent<Enum> evt)
        {
            var incrementType = (VersionUpdateType)evt.newValue;
            EditorPrefs.SetInt("DefineTargetVersionStepElement_IncrementType", (int)incrementType);
            var isSpecific = incrementType == VersionUpdateType.Specific;
            _fieldCustomVersion.style.display = isSpecific ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnDefineButtonClicked()
        {
            var updateType = (VersionUpdateType)_fieldIncrementVersionType.value;
            switch (updateType)
            {
                case VersionUpdateType.Patch:
                case VersionUpdateType.Minor:
                case VersionUpdateType.Major:
                    IncrementVersion();
                    break;
                case VersionUpdateType.Specific:
                    DefineCustomVersion();
                    break;
            }
        }

        private void IncrementVersion()
        {
            VersionUpdateType updateType = (VersionUpdateType)_fieldIncrementVersionType.value;
            var version = SemVerType.From(PlayerSettings.bundleVersion);
            version.Increment(updateType);
            _process.TargetVersion = version;
            _labelTargetVersion.text = version.Raw;
            ArrangeFields(true);
            SetReadyStatus(true);
        }

        private void DefineCustomVersion()
        {
            var versionString = _fieldCustomVersion.value;

            if (string.IsNullOrEmpty(versionString))
            {
                Debug.LogError("Version cannot be empty.");
                return;
            }

            if (!SemVerType.SemVerValid(versionString))
            {
                Debug.LogError("Invalid semver version format.");
                return;
            }

            var version = SemVerType.From(versionString);
            _process.TargetVersion = version;

            _labelTargetVersion.text = version.Raw;
            ArrangeFields(true);
            SetReadyStatus(true);
        }

        private void OnButtonRollbackClicked()
        {
            _process.TargetVersion = null;
            ArrangeFields(false);
            SetReadyStatus(false);
        }

        private void ArrangeFields(bool isVersionDefined)
        {
            _containerSateResult.style.display = isVersionDefined ? DisplayStyle.Flex : DisplayStyle.None;
            _containerSateDefining.style.display = isVersionDefined ? DisplayStyle.None : DisplayStyle.Flex;

            if (!isVersionDefined)
            {
                var isSpecific = (VersionUpdateType)_fieldIncrementVersionType.value == VersionUpdateType.Specific;
                _fieldCustomVersion.style.display = isSpecific ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}