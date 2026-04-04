using System.IO;
using UnityEngine.UIElements;
using UnityEngine;
using System;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Versioning;
using UnityEditor;
using SGUnitySDK.Editor.Presentation.ViewModels;
using SGUnitySDK.Editor.Infrastructure;

namespace SGUnitySDK.Editor.Presentation.Elements
{
    /// <summary>
    /// VisualElement that represents a single build entry in the
    /// Development step builds list. Encapsulates all UI and user
    /// interactions for a build item (status, upload, error display).
    /// </summary>
    public class BuildItemElement : VisualElement
    {
        private SGVersionBuildEntry _entry;
        private readonly DevelopmentStepViewModel _viewModel;
        private readonly DevelopmentProcessStateViewModel _processState;

        // UI parts
        private VisualElement _statusIndicator;
        private Label _pathLabel;
        private VisualElement _errorContainer;
        private Button _uploadButton;

        /// <summary>
        /// Creates a new build item element bound to the provided entry.
        /// The element performs uploads through the provided view model.
        /// </summary>
        /// <param name="entry">The build entry to represent.</param>
        /// <param name="viewModel">View model for development step operations.</param>
        public BuildItemElement(
            SGVersionBuildEntry entry,
            DevelopmentStepViewModel viewModel,
            DevelopmentProcessStateViewModel processState = null)
        {
            _entry = entry;
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _processState = processState ?? EditorServiceProvider.Instance
                .GetService<DevelopmentProcessStateViewModel>();

            AddToClassList("build-item");

            _statusIndicator = new VisualElement();
            _statusIndicator.AddToClassList("build-status");
            Add(_statusIndicator);

            var platformIcon = new VisualElement();
            platformIcon.AddToClassList("platform-icon");
            platformIcon.AddToClassList(_entry.build.platform.ToString().ToLower());
            Add(platformIcon);

            var infoContainer = new VisualElement();
            infoContainer.AddToClassList("build-info");

            var platformDate = new Label($"{_entry.build.platform} - {_entry.build.BuiltAt:g}");
            platformDate.AddToClassList("build-platform-date");
            infoContainer.Add(platformDate);

            var pathContainer = new VisualElement();
            pathContainer.AddToClassList("path-container");
            _pathLabel = new Label(_entry.build.path);
            _pathLabel.AddToClassList("build-path");
            pathContainer.Add(_pathLabel);
            infoContainer.Add(pathContainer);

            _errorContainer = new VisualElement();
            _errorContainer.style.display = DisplayStyle.None;
            _errorContainer.AddToClassList("error-container");
            infoContainer.Add(_errorContainer);

            // Uploaded status line
            if (_entry.uploaded)
            {
                var uploadedLine = new VisualElement();
                uploadedLine.AddToClassList("uploaded-line");
                var uploadedLabel = new Label("Uploaded");
                uploadedLabel.AddToClassList("uploaded-label");
                uploadedLine.Add(uploadedLabel);
                if (!string.IsNullOrEmpty(_entry.remoteUrl))
                {
                    var urlLabel = new Label(_entry.remoteUrl);
                    urlLabel.AddToClassList("uploaded-url");
                    urlLabel.style.whiteSpace = WhiteSpace.Normal;
                    urlLabel.tooltip = _entry.remoteUrl;
                    uploadedLine.Add(urlLabel);
                }
                infoContainer.Add(uploadedLine);
            }

            Add(infoContainer);

            var actions = new VisualElement();
            actions.AddToClassList("build-actions");

            bool isZipValid = !string.IsNullOrEmpty(_entry.build.path) && File.Exists(_entry.build.path);
            bool showAsFailed = !_entry.build.success || !isZipValid;

            if (!showAsFailed && !_entry.uploaded)
            {
                _uploadButton = new Button();
                _uploadButton.text = "Upload";
                _uploadButton.AddToClassList("upload-button");
                _uploadButton.clicked += async () =>
                {
                    _uploadButton.SetEnabled(false);
                    try
                    {
                        if (!_processState.IsDevelopmentStep())
                        {
                            EditorUtility.DisplayDialog("Upload", "Cannot upload: development process is not in Development step.", "OK");
                            return;
                        }

                        var updated = await _viewModel.UploadBuildAsync(_entry);
                        RefreshFromEntry(updated);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Upload failed: {ex.Message}");
                        EditorUtility.DisplayDialog("Upload failed", ex.Message, "OK");
                    }
                    finally
                    {
                        _uploadButton.SetEnabled(true);
                    }
                };
                actions.Add(_uploadButton);
            }

            Add(actions);

            // Display errors if present
            if (!_entry.build.success && !string.IsNullOrEmpty(_entry.build.errorMessage))
            {
                var errorLabel = new Label(_entry.build.errorMessage);
                errorLabel.AddToClassList("build-error");
                _errorContainer.Add(errorLabel);
                _errorContainer.style.display = DisplayStyle.Flex;
            }

            if (!isZipValid)
            {
                var zipError = new Label("Zip file is missing or corrupted");
                zipError.AddToClassList("build-error");
                _errorContainer.Add(zipError);
                _errorContainer.style.display = DisplayStyle.Flex;

                var warningIcon = new VisualElement();
                warningIcon.AddToClassList("warning-icon");
                warningIcon.tooltip = "Zip file is missing or corrupted";
                pathContainer.Add(warningIcon);
            }
        }

        /// <summary>
        /// Refresh the element's UI from the provided (possibly updated)
        /// build entry.
        /// </summary>
        /// <param name="entry">Updated build entry.</param>
        public void RefreshFromEntry(SGVersionBuildEntry entry)
        {
            _entry = entry;
            _pathLabel.text = _entry.build.path;
            // If uploaded, hide upload button and show uploaded line (simpler: reload parent list will reflect changes)
            if (_uploadButton != null && _entry.uploaded)
            {
                _uploadButton.RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// Indicates whether this entry is marked as uploaded.
        /// </summary>
        public bool IsUploaded => _entry.uploaded;

        /// <summary>
        /// Enable or disable the upload button for this item. If the
        /// entry is already uploaded, the button will remain disabled/hidden.
        /// </summary>
        /// <param name="enabled">Whether the upload button should be enabled.</param>
        public void SetUploadEnabled(bool enabled)
        {
            if (_uploadButton == null) return;
            if (_entry.uploaded)
            {
                _uploadButton.SetEnabled(false);
                return;
            }
            _uploadButton.SetEnabled(enabled);
        }
    }
}
