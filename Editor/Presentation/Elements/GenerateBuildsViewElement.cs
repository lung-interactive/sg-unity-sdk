using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Collections.Generic;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Singletons;
using UnityEditor;
using SGUnitySDK.Editor.Infrastructure;
using SGUnitySDK.Editor.Presentation.ViewModels;

namespace SGUnitySDK.Editor.Presentation.Elements
{
    /// <summary>
    /// VisualElement that loads the `UXML/GenerateBuildsView` template and
    /// encapsulates the generate-builds UI logic: generate button, builds
    /// list rendering and Upload All button (via BuildsListElement).
    /// </summary>
    public class GenerateBuildsViewElement : VisualElement
    {
        private readonly DevelopmentStepViewModel _viewModel;
        private readonly DevelopmentProcessStateViewModel _processState;
        private BuildsListElement _serverBuildsList;
        private BuildsListElement _clientBuildsList;
        private Button _buttonGenerateBuilds;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateBuildsViewElement"/> class.
        /// </summary>
        /// <param name="viewModel">View model used for generation/upload operations.</param>
        /// <param name="processState">Process state view model for UI state queries.</param>
        public GenerateBuildsViewElement(
            DevelopmentStepViewModel viewModel,
            DevelopmentProcessStateViewModel processState = null)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _processState = processState ?? EditorServiceProvider.Instance
                .GetService<DevelopmentProcessStateViewModel>();

            var visualTree = Resources.Load<VisualTreeAsset>("UXML/GenerateBuildsView");
            if (visualTree == null)
            {
                Debug.LogWarning("GenerateBuildsView UXML not found at 'UXML/GenerateBuildsView'. Ensure asset is present.");
                return;
            }

            visualTree.CloneTree(this);

            var serverGroup = this.Q<VisualElement>("server-builds-list-group");
            if (serverGroup != null)
            {
                var serverScroll = serverGroup.Q<ScrollView>("scroll-builds-list");
                if (serverScroll != null)
                {
                    _serverBuildsList = new BuildsListElement(
                        serverScroll,
                        serverGroup,
                        _viewModel,
                        _processState,
                        BuildType.Server);
                }
            }

            var clientGroup = this.Q<VisualElement>("client-builds-list-group");
            if (clientGroup != null)
            {
                var clientScroll = clientGroup.Q<ScrollView>("scroll-builds-list");
                if (clientScroll != null)
                {
                    _clientBuildsList = new BuildsListElement(
                        clientScroll,
                        clientGroup,
                        _viewModel,
                        _processState,
                        BuildType.Client);
                }
            }

            this.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                _processState.StepChanged += OnStepChanged;
                RefreshBuildSections();
            });

            this.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                try
                {
                    _processState.StepChanged -= OnStepChanged;
                }
                catch (Exception)
                {
                    // ignore - safe cleanup
                }
            });

            _buttonGenerateBuilds = this.Q<Button>("button-generate-builds");
            if (_buttonGenerateBuilds != null)
            {
                _buttonGenerateBuilds.clicked += OnGenerateBuildsClicked;
            }
        }

        public void SetBuilds(List<SGVersionBuildEntry> entries)
        {
            var source = entries ?? new List<SGVersionBuildEntry>();

            if (_serverBuildsList != null)
            {
                _serverBuildsList.SetBuilds(source);
            }

            if (_clientBuildsList != null)
            {
                _clientBuildsList.SetBuilds(source);
            }
        }

        private void OnGenerateBuildsClicked()
        {
            if (_buttonGenerateBuilds == null) return;
            _buttonGenerateBuilds.SetEnabled(false);

            try
            {
                if (!_processState.IsDevelopmentStep())
                {
                    throw new InvalidOperationException("Cannot generate builds: current development step is not 'Development'.");
                }

                _viewModel.GenerateBuilds();
                RefreshBuildSections();

                EditorUtility.DisplayDialog("Generate Builds", "Build generation completed. Use the upload buttons to upload each build.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to generate builds: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed: {ex.Message}", "OK");
            }
            finally
            {
                _buttonGenerateBuilds.SetEnabled(true);
            }
        }

        private void OnStepChanged(DevelopmentStep step)
        {
            if (step == DevelopmentStep.Development)
            {
                RefreshBuildSections();
            }
        }

        private void RefreshBuildSections()
        {
            if (_serverBuildsList != null)
            {
                _serverBuildsList.SetBuilds(
                    _processState.GetServerVersionBuildsOrEmpty());
            }

            if (_clientBuildsList != null)
            {
                _clientBuildsList.SetBuilds(
                    _processState.GetClientVersionBuildsOrEmpty());
            }
        }
    }
}
