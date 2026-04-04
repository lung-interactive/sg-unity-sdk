using UnityEngine.UIElements;
using UnityEngine;
using System;
using SGUnitySDK.Editor.Core.Singletons;
using SGUnitySDK.Editor.Presentation.Elements;
using UnityEditor;
using SGUnitySDK.Editor.Presentation.Controllers;
using SGUnitySDK.Editor.Presentation.ViewModels;
using SGUnitySDK.Editor.Infrastructure;

namespace SGUnitySDK.Editor.Presentation.Elements
{
    /// <summary>
    /// View for the Development Step in the SG development process.
    /// Only handles UI display and user interaction.
    /// </summary>
    public class DevProcessDevelopmentStepElement : DevelopmentStepElement
    {
        private readonly DevelopmentStepViewModel _viewModel;
        private readonly DevelopmentProcessStateViewModel _processState;
        private readonly DevProcessDevelopmentStepController _controller;

        public override DevelopmentStep Step => DevelopmentStep.Development;
        /// <summary>
        /// Label displaying the version currently in development.
        /// </summary>
        public Label LabelDevelopmentVersion { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DevProcessDevelopmentStepElement"/> class.
        /// Loads the UXML and binds UI elements.
        /// </summary>
        private Button _buttonAccept;
        private Button _buttonSendToHomologation;

        // Generate builds UI
        private GenerateBuildsViewElement _generateBuildsView;
        private VisualElement _placeholder;
        private Label _placeholderMessageLabel;
        private VisualElement _approvedUxmlContainer;
        private VisualElement _canceledUxmlContainer;

        /// <param name="viewModel">View model used for step actions.</param>
        /// <param name="controller">Controller used for step interactions.</param>
        /// <param name="processState">Process state view model used for UI state.</param>
        public DevProcessDevelopmentStepElement(
            DevelopmentStepViewModel viewModel,
            DevProcessDevelopmentStepController controller,
            DevelopmentProcessStateViewModel processState = null)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _processState = processState ?? EditorServiceProvider.Instance
                .GetService<DevelopmentProcessStateViewModel>();

            var visualTree = Resources.Load<VisualTreeAsset>("UXML/DevProcess_DevelopmentStep");
            visualTree.CloneTree(this);
            LabelDevelopmentVersion = this.Q<Label>("label-development-version");

            // Ensure the element reacts to the presence/absence of a development version.
            _controller.OnVersionChanged += version =>
            {
                var hasRemoteVersion = !string.IsNullOrEmpty(version) && version != "-";
                if (hasRemoteVersion)
                    Activate(_processState.Process);
                else
                    Deactivate();

                LabelDevelopmentVersion.text = version ?? "-";
            };

            _buttonAccept = new Button(async () =>
            {
                try
                {
                    await _controller.AcceptDevelopmentVersionAwaitable();
                    _processState.SetStep(DevelopmentStep.Development);
                    UpdateStepDisplay();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error accepting version: {ex.Message}");
                    EditorUtility.DisplayDialog("Error", ex.Message, "OK");
                }
            })
            {
                text = "Accept Version"
            };
            _buttonAccept.style.marginTop = 8;
            // Place accept button inside the placeholder area so it is visually inside
            _placeholder = this.Q<VisualElement>("container-step-placeholder");
            if (_placeholder != null)
            {
                // keep any existing UXML message; find first label to toggle visibility later
                _placeholderMessageLabel = _placeholder.Q<Label>();
                _placeholder.Add(_buttonAccept);

                // Load approved/canceled informational UXML into placeholder so
                // messages are present in the UXML and can be toggled by step.
                try
                {
                    var approvedTree = Resources.Load<VisualTreeAsset>("UXML/DevProcess_ApprovedStep");
                    if (approvedTree != null)
                    {
                        _approvedUxmlContainer = new VisualElement();
                        approvedTree.CloneTree(_approvedUxmlContainer);
                        _placeholder.Add(_approvedUxmlContainer);
                    }

                    var canceledTree = Resources.Load<VisualTreeAsset>("UXML/DevProcess_CanceledStep");
                    if (canceledTree != null)
                    {
                        _canceledUxmlContainer = new VisualElement();
                        canceledTree.CloneTree(_canceledUxmlContainer);
                        _placeholder.Add(_canceledUxmlContainer);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load approved/canceled UXML into placeholder: {ex.Message}");
                }
            }
            else
            {
                this.Add(_buttonAccept);
            }

            // Create Generate Builds view (kept hidden until Development step)
            _generateBuildsView = new GenerateBuildsViewElement(_viewModel, _processState);
            if (_generateBuildsView != null)
            {
                _generateBuildsView.style.display = DisplayStyle.None;
                if (_placeholder != null)
                    _placeholder.Add(_generateBuildsView);
                else
                    this.Add(_generateBuildsView);
            }

            // Wire 'Send to homologation' button if present in the UXML
            _buttonSendToHomologation = this.Q<Button>("button-send-to-homologation");
            if (_buttonSendToHomologation != null)
            {
                _buttonSendToHomologation.clicked += async () =>
                {
                    _buttonSendToHomologation.SetEnabled(false);
                    try
                    {
                        var version = _processState.CurrentVersion;
                        if (version == null)
                        {
                            EditorUtility.DisplayDialog("Send to homologation", "No version selected.", "OK");
                            return;
                        }

                        bool ok = await _viewModel.SendCurrentVersionToHomologationAsync();
                        if (ok)
                        {
                            _processState.SetStep(DevelopmentStep.Homologation);
                            EditorUtility.DisplayDialog("Send to homologation", "Version sent to homologation.", "OK");
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Send to homologation", "Failed to send version to homologation.", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error sending to homologation: {ex.Message}");
                        EditorUtility.DisplayDialog("Error", ex.Message, "OK");
                    }
                    finally
                    {
                        _buttonSendToHomologation.SetEnabled(true);
                    }
                };

                // Update visibility when local builds list changes
                _processState.LocalBuildsChanged += UpdateStepDisplay;
            }

            UpdateStepDisplay();
            _processState.StepChanged += OnProcessStepChanged;

            // Initialize visibility according to whether there's a local version
            if (!_processState.HasCurrentVersion())
            {
                Deactivate();
            }

            this.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                try
                {
                    _processState.StepChanged -= OnProcessStepChanged;
                    _processState.LocalBuildsChanged -= UpdateStepDisplay;
                }
                catch (Exception)
                {
                    // ignore - safe cleanup
                }
            });
        }

        private void UpdateStepDisplay()
        {
            var step = _processState.CurrentStep;
            _buttonAccept.style.display = step == DevelopmentStep.AcceptVersion ? DisplayStyle.Flex : DisplayStyle.None;

            if (_generateBuildsView != null)
            {
                _generateBuildsView.style.display = step == DevelopmentStep.Development ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Show 'Send to homologation' button only when current version has at
            // least one generated build and all generated builds were uploaded.
            if (_buttonSendToHomologation != null)
            {
                bool canShow = false;
                try
                {
                    canShow = _processState.AreAllBuildsUploaded();
                }
                catch (Exception)
                {
                    canShow = false;
                }

                // Only show the 'Send to homologation' button when all builds
                // were uploaded AND the current step is Development.
                _buttonSendToHomologation.style.display = (canShow && step == DevelopmentStep.Development)
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Update the visible label that shows current step text
            var label = this.Q<Label>("label-current-step");
            if (label != null)
            {
                label.text = step switch
                {
                    DevelopmentStep.AcceptVersion => "Acceptance",
                    DevelopmentStep.Development => "Developing",
                    DevelopmentStep.Homologation => "Homologation",
                    DevelopmentStep.Approved => "Approved",
                    DevelopmentStep.Canceled => "Canceled",
                    _ => step.ToString()
                };
            }

            // Toggle the placeholder message visibility based on current step
            if (_placeholderMessageLabel != null)
            {
                _placeholderMessageLabel.style.display = step == DevelopmentStep.Homologation
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            // Toggle our injected approved/canceled UXML visibility
            if (_approvedUxmlContainer != null)
            {
                _approvedUxmlContainer.style.display = step == DevelopmentStep.Approved ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_canceledUxmlContainer != null)
            {
                _canceledUxmlContainer.style.display = step == DevelopmentStep.Canceled ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Ensure the current step header inside this element is visible
            // when the process is in Homologation so the user sees the step label.
            var currentStepContainer = this.Q<VisualElement>("current-step-container");
            if (currentStepContainer != null)
            {
                currentStepContainer.style.display = step == DevelopmentStep.Homologation ||
                                                      step == DevelopmentStep.Development ||
                                                      step == DevelopmentStep.AcceptVersion
                    ? DisplayStyle.Flex
                    : DisplayStyle.Flex; // keep visible by default
            }

            // Ensure the sibling 'container-version-status' (version status section)
            // is visible when Homologation is active.
            if (step == DevelopmentStep.Homologation)
            {
                VisualElement root = this;
                while (root.parent != null)
                    root = root.parent;

                var versionStatus = root.Q<VisualElement>("container-version-status");
                if (versionStatus != null)
                    versionStatus.style.display = DisplayStyle.Flex;
            }

            // Visibility of the development element itself is controlled elsewhere
            // (presence of a current version). Here we only toggle internal controls.
        }

        private void OnProcessStepChanged(DevelopmentStep step)
        {
            UpdateStepDisplay();
        }
    }
}
