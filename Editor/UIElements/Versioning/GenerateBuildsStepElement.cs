using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SGUnitySDK.Editor.Versioning;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public class GenerateBuildsStepElement : VersioningStepElement
    {
        private static readonly string TemplateName = "SGProcessStep3_GenerateBuilds";
        public override VersioningStep Step => VersioningStep.GenerateBuilds;

        private readonly TemplateContainer _containerMain;
        private readonly Button _buttonGenerateBuilds;
        private readonly ScrollView _scrollBuildsList;
        private List<SGLocalBuildResult> _cachedBuilds = new();
        private SGEditorConfig Config => SGEditorConfig.instance;

        public GenerateBuildsStepElement()
        {
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            _buttonGenerateBuilds = _containerMain.Q<Button>("button-generate-builds");
            _scrollBuildsList = _containerMain.Q<ScrollView>("scroll-builds-list");

            _buttonGenerateBuilds.clicked += OnButtonGenerateBuildsClicked;

            Add(_containerMain);
        }

        public override void Activate(VersioningProcess process)
        {
            base.Activate(process);
            RefreshBuildsCache();
            VersioningProcess.instance.LocalBuildsChanged += OnLocalBuildsChanged;
        }

        public override void Deactivate()
        {
            VersioningProcess.instance.LocalBuildsChanged -= OnLocalBuildsChanged;
            base.Deactivate();
        }

        private void OnLocalBuildsChanged()
        {
            RefreshBuildsCache();
        }

        private void RefreshBuildsCache()
        {
            _cachedBuilds = new List<SGLocalBuildResult>(VersioningProcess.instance.LocalBuilds);
            UpdateBuildsList();
            UpdateReadyStatus();
        }

        private void UpdateReadyStatus()
        {
            bool hasValidSuccessfulBuild = _cachedBuilds.Any(b =>
                b.success
                && !string.IsNullOrEmpty(b.path)
                && File.Exists(b.path)
            );

            SetReadyStatus(hasValidSuccessfulBuild);
        }

        private void UpdateBuildsList()
        {
            _scrollBuildsList.Clear();

            if (_cachedBuilds.Count == 0)
            {
                var emptyLabel = new Label("No builds generated yet");
                emptyLabel.AddToClassList("empty-builds-label");
                _scrollBuildsList.Add(emptyLabel);
                return;
            }

            foreach (var build in _cachedBuilds)
            {
                var buildItem = VersionStepElementsHelpers.CreateBuildItemElement(build);
                _scrollBuildsList.Add(buildItem);
            }
        }

        private void OnButtonGenerateBuildsClicked()
        {
            _ = GenerateBuilds();
        }

        private async Awaitable GenerateBuilds()
        {
            VersioningProcess.instance.ClearLocalBuilds();
            var setups = Config.BuildSetups;
            var commonBuildPath = Path.Combine(Config.BuildsDirectory);
            string targetVersion = VersioningProcess.instance.TargetVersion.Raw;
            _buttonGenerateBuilds.SetEnabled(false);

            try
            {
                var buildResults = await SGPlayerBuilder.PerformMultipleBuilds(
                    setups,
                    commonBuildPath,
                    targetVersion);

                VersioningProcess.instance.LocalBuilds = buildResults;
            }
            finally
            {
                _buttonGenerateBuilds.SetEnabled(true);
            }
        }
    }
}