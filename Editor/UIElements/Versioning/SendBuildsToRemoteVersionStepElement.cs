using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public class SendBuildsToRemoteVersionStepElement : VersioningStepElement
    {
        private static readonly string TemplateName = "SGProcessStep4_SendBuildsToRemote";
        public override VersioningStep Step => VersioningStep.SendBuildsToRemote;

        private readonly TemplateContainer _containerMain;
        private Button _buttonGenerateBuilds;
        private ScrollView _scrollBuildsList;
        private List<SGLocalBuildResult> _cachedBuilds = new();
        private SGEditorConfig Config => SGEditorConfig.instance;

        public SendBuildsToRemoteVersionStepElement()
        {
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            _buttonGenerateBuilds = _containerMain.Q<Button>("button-send");
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
            _cachedBuilds = VersioningProcess.instance.LocalBuilds.Where(b => b.success).ToList();
            UpdateBuildsList();
            UpdateReadyStatus();
        }

        private void UpdateReadyStatus()
        {
            bool hasSuccessfulBuilds = _cachedBuilds.Any(b => b.success);
            SetReadyStatus(hasSuccessfulBuilds);
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
                _scrollBuildsList.Add(VersionStepElementsHelpers.CreateBuildItemElement(build));
            }
        }

        private void OnButtonGenerateBuildsClicked()
        {

        }
    }
}