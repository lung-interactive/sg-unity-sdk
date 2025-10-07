using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SGUnitySDK.Editor.Versioning;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public class GenerateBuildsStepElement : VersioningStepElement
    {
        private static readonly string TemplateName = "SGProcessStep3_GenerateBuilds";
        public override VersioningStep Step => VersioningStep.Builds;

        private readonly TemplateContainer _containerMain;
        private readonly Button _buttonGenerateBuilds;
        private readonly ScrollView _scrollBuildsList;

        private List<SGVersionBuildEntry> _cachedBuilds = new();
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
            _cachedBuilds = new List<SGVersionBuildEntry>(VersioningProcess.instance.VersionBuilds);
            UpdateBuildsList();
            UpdateReadyStatus();
        }

        private void UpdateReadyStatus()
        {
            bool ready =
                _cachedBuilds.Count > 0 &&
                _cachedBuilds.All(b =>
                    b.build.success &&
                    !string.IsNullOrEmpty(b.build.path) &&
                    File.Exists(b.build.path) &&
                    b.uploaded &&
                    string.IsNullOrEmpty(b.uploadError)
                );

            SetReadyStatus(ready);
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

            for (int i = 0; i < _cachedBuilds.Count; i++)
            {
                int index = i; // capture
                var buildItem = VersionStepElementsHelpers.CreateBuildItemElement(
                    _cachedBuilds[i],
                    onUploadClicked: entry => { _ = OnUploadBuildClicked(index, entry); }
                );
                _scrollBuildsList.Add(buildItem);
            }
        }

        private void OnButtonGenerateBuildsClicked()
        {
            GenerateBuilds();
        }

        private void GenerateBuilds()
        {
            VersioningProcess.instance.ClearVersionBuilds();

            var setups = Config.BuildSetups;
            var commonBuildPath = Path.Combine(Config.BuildsDirectory);
            string targetVersion = VersioningProcess.instance.TargetVersion?.Raw ?? "0.0.0";

            Debug.Log($"-SG Generating builds for version {targetVersion}...");
            Debug.Log($"-SG Common build path: {commonBuildPath}");

            _buttonGenerateBuilds.SetEnabled(false);

            try
            {
                var localResults = SGPlayerBuilder.PerformMultipleBuilds(
                    setups,
                    commonBuildPath,
                    targetVersion
                );

                var entries = localResults.Select(r => new SGVersionBuildEntry
                {
                    build = r,
                    uploaded = false,
                    remoteUrl = null,
                    sha256 = null,
                    uploadError = null,
                    uploadUnixTimestamp = 0
                }).ToList();

                VersioningProcess.instance.VersionBuilds = entries;
            }
            finally
            {
                _buttonGenerateBuilds.SetEnabled(true);
            }
        }

        /// <summary>
        /// Upload handler (async), atualiza o entry no VersioningProcess.
        /// </summary>
        private async Awaitable OnUploadBuildClicked(int index, SGVersionBuildEntry entry)
        {
            // Checa se já existe semver remoto
            var semver = VersioningProcess.instance.TargetVersion.Raw;
            if (string.IsNullOrEmpty(semver))
            {
                EditorUtility.DisplayDialog(
                    "Start version first",
                    "No remote version is active. Please run 'Start Version In Remote' step before uploading.",
                    "OK"
                );
                return;
            }

            if (!entry.IsBuildOkAndZipExists())
            {
                EditorUtility.DisplayDialog(
                    "Upload",
                    "Build file is missing or invalid.",
                    "OK"
                );
                return;
            }

            try
            {
                using var cts = new CancellationTokenSource();
                var updated = await SGBuildUploader.UploadBuildZipAsync(entry, semver, cts.Token);

                VersioningProcess.instance.ReplaceVersionBuild(index, updated);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Upload failed: {ex.Message}");
                entry.MarkUploadFailed(ex.Message);
                VersioningProcess.instance.ReplaceVersionBuild(index, entry);

                EditorUtility.DisplayDialog(
                    "Upload failed",
                    ex.Message,
                    "OK"
                );
            }
        }
    }
}
