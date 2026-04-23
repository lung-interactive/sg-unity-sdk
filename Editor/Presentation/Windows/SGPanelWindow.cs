
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System;
using SGUnitySDK.Editor.Presentation.Controllers;
using SGUnitySDK.Editor.Infrastructure;
using SGUnitySDK.Editor.Presentation.Elements;
using System.Threading.Tasks;
using SGUnitySDK.Editor.Presentation.ViewModels;

namespace SGUnitySDK.Editor.Presentation.Windows
{
    /// <summary>
    /// Main Unity Editor window for SGUnitySDK.
    /// Manages development/config tabs, version status, and configuration.
    /// </summary>
    public class SGPanelWindow : EditorWindow
    {
        private static readonly string TemplateName = "SGPanelWindow";
        private static string LastActiveTabPrefKey =>
            $"SGUnitySDK.SGBuilderWindow_LastActiveTab::{Application.dataPath}";
        private static SGPanelWindow _window;

        /// <summary>
        /// Shows the main SGUnitySDK window in the Unity Editor.
        /// Always synchronizes local/remote state before showing content.
        /// </summary>
        [MenuItem("SGUnitySDK/Main Panel", false, 0)]
        public static void ShowWindow()
        {
            _ = ShowWindowWithSyncAsync();
        }

        /// <summary>
        /// Executes the sync use case before showing the panel.
        /// Displays a loading indicator until sync completes.
        /// </summary>
        private static async Task ShowWindowWithSyncAsync()
        {
            // Ensure window instance exists
            if (_window == null)
            {
                _window = GetWindow<SGPanelWindow>();
                _window.titleContent = new GUIContent("SGUnitySDK");
                _window.minSize = new Vector2(400, 300);
            }
            else
            {
                _window.Show();
            }

            // Show loading indicator (if UI already built)
            _window?.Show();
            var loading = _window?._containerMain?.Q<VisualElement>("container-loading");
            var content = _window?._containerMain?.Q<VisualElement>("container-content");
            if (loading != null)
            {
                loading.SetEnabled(true);
                loading.style.display = DisplayStyle.Flex;
            }
            if (content != null)
            {
                // hide the content while syncing
                content.style.display = DisplayStyle.None;
            }

            // Resolve and execute the sync use case
            var panelViewModel = EditorServiceProvider.Instance
                .GetService<SGPanelWindowViewModel>();
            if (panelViewModel != null)
            {
                await panelViewModel.SyncDevelopmentProcessAsync();
            }

            // Hide loading indicator and show content
            if (loading != null)
            {
                loading.SetEnabled(false);
                loading.style.display = DisplayStyle.None;
            }
            if (content != null)
            {
                content.style.display = DisplayStyle.Flex;
            }
        }

        #region Development Process Reset & Auto-Advance

        /// <summary>
        /// Resets the development process and opens the main window.
        /// Clears state and reloads status to reflect initial state.
        /// </summary>
        public static void ResetDevelopmentProcess()
        {
            ResetDevelopmentProcessAsync();
        }

        /// <summary>
        /// Async handler for resetting the development process and updating the UI only after data is ready.
        /// </summary>
        private static async void ResetDevelopmentProcessAsync()
        {
            var processStateViewModel = EditorServiceProvider.Instance
                .GetService<DevelopmentProcessStateViewModel>();
            processStateViewModel.Process.ResetProcess();

            var panelViewModel = EditorServiceProvider.Instance
                .GetService<SGPanelWindowViewModel>();
            if (panelViewModel != null)
            {
                await panelViewModel.AdvanceToDevelopmentIfAcceptedAsync();
            }

            ShowWindow();
        }

        /// <summary>
        /// Checks if there is a version on the server marked as 'under development' and already accepted.
        /// If so, advances the process to the development step automatically.
        /// </summary>
        public static async Task AdvanceToDevelopmentIfAcceptedAsync()
        {
            var panelViewModel = EditorServiceProvider.Instance
                .GetService<SGPanelWindowViewModel>();
            if (panelViewModel == null)
            {
                return;
            }

            await panelViewModel.AdvanceToDevelopmentIfAcceptedAsync();
        }

        #endregion

        #region Fields

        private SGConfigWindowViewModel _configViewModel;
        private DevelopmentProcessStateViewModel _processStateViewModel;
        private SerializedObject _serializedConfig;
        private TemplateContainer _containerMain;
        private TabView _tabView;
        private Label _labelServerCurrentVersion;
        private Label _labelCurrentlyDeveloping;
        private VisualElement _containerCurrentlyDeveloping;
        private VisualElement _containerCurrentStep;
        private VisualElement _containerLoading;
        private TextField _fieldGameDevelopmentToken;
        private Toggle _fieldShouldOverrideBaseURL;
        private TextField _fieldBaseURlOverride;
        private TextField _fieldBuildsDirectory;
        private Button _buttonDefineBuildsDirectory;
        private ListView _listBuildProfiles;

        #endregion

        #region Editor Window Lifecycle

        /// <summary>
        /// Called by Unity to create the window UI.
        /// </summary>
        public void CreateGUI()
        {
            _configViewModel = EditorServiceProvider.Instance
                .GetService<SGConfigWindowViewModel>();
            _processStateViewModel = EditorServiceProvider.Instance
                .GetService<DevelopmentProcessStateViewModel>();
            _serializedConfig = _configViewModel.CreateSerializedConfig();
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            _tabView = _containerMain.Q<TabView>("container-main");
            int lastActiveTabIndex = EditorPrefs.GetInt(LastActiveTabPrefKey, 0);
            _tabView.selectedTabIndex = lastActiveTabIndex;
            _tabView.activeTabChanged += (old, current) =>
            {
                EditorPrefs.SetInt(LastActiveTabPrefKey, _tabView.selectedTabIndex);
            };

            // Development Tab Elements
            _labelServerCurrentVersion = _containerMain.Q<Label>("label-server-current-version");
            _labelCurrentlyDeveloping = _containerMain.Q<Label>("label-currently-developing");
            _containerCurrentlyDeveloping = _containerMain.Q<VisualElement>("container-currently-developing");
            _containerLoading = _containerMain.Q<VisualElement>("container-loading");
            var containerVersionStatus = _containerMain.Q<VisualElement>("container-version-status");

            // Resolve content wrapper which holds version-status and will contain the development step
            var containerContent = _containerMain.Q<VisualElement>("container-content");
            if (containerContent == null)
                containerContent = _containerMain.Q<VisualElement>("container-development");
            if (containerContent == null)
            {
                containerContent = new VisualElement();
                containerContent.name = "container-content-fallback";
                _containerMain.Add(containerContent);
            }
            _containerCurrentStep = containerContent;

            // Ensure initial visibility: content visible, loading hidden, 'currently developing' hidden
            if (_containerLoading != null) _containerLoading.style.display = DisplayStyle.None;
            if (_containerCurrentlyDeveloping != null) _containerCurrentlyDeveloping.style.display = DisplayStyle.None;
            if (containerVersionStatus != null) containerVersionStatus.style.display = DisplayStyle.Flex;

            // MVC: Controller + View
            var developmentStepViewModel = EditorServiceProvider.Instance
                .GetService<DevelopmentStepViewModel>();
            var devStepController = new DevProcessDevelopmentStepController(
                developmentStepViewModel);
            var devStepView = new DevProcessDevelopmentStepElement(
                developmentStepViewModel,
                devStepController);


            // Insert dev step under content (after version-status) when possible
            if (_containerCurrentStep != null)
            {
                var versionChild = _containerCurrentStep.Q<VisualElement>("container-version-status");
                int insertIndex = (versionChild != null) ? _containerCurrentStep.IndexOf(versionChild) + 1 : _containerCurrentStep.childCount;
                try
                {
                    _containerCurrentStep.Insert(insertIndex, devStepView);
                }
                catch
                {
                    _containerCurrentStep.Add(devStepView);
                }

                // The development UXML contains the homologation message in the
                // placeholder, so no runtime injection of homologation UXML is required.
            }

            // Updates loading status (server version via use case)
            async void UpdateStatus()
            {
                if (_containerLoading != null) _containerLoading.style.display = DisplayStyle.Flex;
                if (_containerCurrentStep != null) _containerCurrentStep.style.display = DisplayStyle.None;
                try
                {
                    if (devStepController != null)
                        await devStepController.FetchAndNotifyDevelopmentVersionAwaitable();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to update development status: {ex.Message}");
                }
                finally
                {
                    if (_containerLoading != null) _containerLoading.style.display = DisplayStyle.None;
                    if (_containerCurrentStep != null) _containerCurrentStep.style.display = DisplayStyle.Flex;
                    if (containerVersionStatus != null) containerVersionStatus.style.display = DisplayStyle.Flex;
                }
            }

            if (devStepController != null)
            {
                devStepController.OnVersionChanged += version =>
                {
                    // Always update the visual label when controller notifies.
                    if (_labelCurrentlyDeveloping != null) _labelCurrentlyDeveloping.text = version;

                    // Show the 'currently developing' container when there is a local
                    // current version in the DevelopmentProcess, even when the server
                    // reports no under-development version (e.g. when in Homologation).
                    var hasLocalVersion = _processStateViewModel.HasCurrentVersion();
                    if (_containerCurrentlyDeveloping != null)
                        _containerCurrentlyDeveloping.style.display = hasLocalVersion ? DisplayStyle.Flex : DisplayStyle.None;
                };
                // Initialize label from local state to ensure Homologation state shows up
                devStepController.LoadDevelopmentVersion();
                devStepController.OnError += msg => Debug.LogError(msg);
                devStepController.OnInfo += msg => Debug.Log(msg);
            }

            UpdateStatus();

            // Server current version label
            async void UpdateServerCurrentVersion()
            {
                try
                {
                    var panelViewModel = EditorServiceProvider.Instance
                        .GetService<SGPanelWindowViewModel>();
                    var versionString = panelViewModel == null
                        ? "-"
                        : await panelViewModel.GetServerCurrentVersionLabelAsync();
                    if (_labelServerCurrentVersion != null) _labelServerCurrentVersion.text = versionString;
                }
                catch (Exception ex)
                {
                    if (_labelServerCurrentVersion != null) _labelServerCurrentVersion.text = "Error";
                    Debug.LogError($"Failed to fetch server version: {ex.Message}");
                }
            }
            UpdateServerCurrentVersion();

            // Config Tab Elements
            _fieldGameDevelopmentToken = _containerMain.Q<TextField>("field-game-management-token");
            if (_fieldGameDevelopmentToken != null)
            {
                _fieldGameDevelopmentToken.SetValueWithoutNotify(_configViewModel.GameDevelopmentToken);
                _fieldGameDevelopmentToken.RegisterValueChangedCallback(OnGameDevelopmentTokenValueChanged);
            }

            _fieldShouldOverrideBaseURL = _containerMain.Q<Toggle>("field-should-override-base-url");
            if (_fieldShouldOverrideBaseURL != null)
            {
                _fieldShouldOverrideBaseURL.SetValueWithoutNotify(_configViewModel.ShouldOverrideBaseURL);
                _fieldShouldOverrideBaseURL.RegisterValueChangedCallback(OnOverrideBaseURLValueChanged);
            }

            _fieldBaseURlOverride = _containerMain.Q<TextField>("field-base-url-override");
            if (_fieldBaseURlOverride != null)
            {
                _fieldBaseURlOverride.SetValueWithoutNotify(_configViewModel.BaseURLOverride);
                _fieldBaseURlOverride.RegisterValueChangedCallback(OnOverrideBaseURLValueChanged);
            }

            _fieldBuildsDirectory = _containerMain.Q<TextField>("field-builds-directory");
            if (_fieldBuildsDirectory != null)
            {
                _fieldBuildsDirectory.SetValueWithoutNotify(_configViewModel.BuildsDirectory);
                _fieldBuildsDirectory.SetEnabled(false);
            }

            _listBuildProfiles = _containerMain.Q<ListView>("list-build-profiles");
            SetupBuildProfilesList();

            _buttonDefineBuildsDirectory = _containerMain.Q<Button>("button-define-builds-directory");
            if (_buttonDefineBuildsDirectory != null) _buttonDefineBuildsDirectory.clicked += OnButtonDefineBuildsDirectoryClicked;

            EvaluateBaseURLDisplay(_configViewModel.ShouldOverrideBaseURL);
            rootVisualElement.Add(_containerMain);
        }

        /// <summary>
        /// Registers play mode state change callback.
        /// </summary>
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged +=
                OnPlayModeStateChanged;
        }

        /// <summary>
        /// Persists config and unregisters play mode callback.
        /// </summary>
        private void OnDisable()
        {
            _listBuildProfiles?.Unbind();
            _configViewModel?.Persist();
            EditorApplication.playModeStateChanged -=
                OnPlayModeStateChanged;
        }

        /// <summary>
        /// Configures the build profiles list view.
        /// </summary>
        private void SetupBuildProfilesList()
        {
            if (_serializedConfig == null || _listBuildProfiles == null)
            {
                return;
            }

            _serializedConfig.UpdateIfRequiredOrScript();
            var property = _serializedConfig.FindProperty("_buildSetups");

            if (property == null || !property.isArray)
            {
                Debug.LogWarning("SGPanelWindow could not bind _buildSetups property.");
                return;
            }

            _listBuildProfiles.Unbind();
            _listBuildProfiles.BindProperty(property);
            _listBuildProfiles.reorderable = true;
            _listBuildProfiles.reorderMode = ListViewReorderMode.Animated;
            _listBuildProfiles.selectionType = SelectionType.Single;
            _listBuildProfiles.showAddRemoveFooter = true;
            _listBuildProfiles.showBorder = true;
            _listBuildProfiles.showFoldoutHeader = false;
            _listBuildProfiles.virtualizationMethod =
                CollectionVirtualizationMethod.DynamicHeight;
        }

        #endregion

        #region Display Evaluation

        /// <summary>
        /// Shows or hides the base URL override field.
        /// </summary>
        /// <param name="shouldOverrideBaseURL">If true, shows the field.</param>
        private void EvaluateBaseURLDisplay(bool shouldOverrideBaseURL)
        {
            if (shouldOverrideBaseURL)
            {
                _fieldBaseURlOverride.style.display = DisplayStyle.Flex;
            }
            else
            {
                _fieldBaseURlOverride.style.display = DisplayStyle.None;
            }
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Handles play mode state changes.
        /// </summary>
        /// <param name="state">The play mode state.</param>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // No-op, reserved for future use.
        }

        /// <summary>
        /// Handles changes to the game development token field.
        /// </summary>
        /// <param name="evt">Change event.</param>
        private void OnGameDevelopmentTokenValueChanged(
            ChangeEvent<string> evt)
        {
            _configViewModel.SetGameDevelopmentToken(evt.newValue);
        }

        /// <summary>
        /// Handles changes to the override base URL toggle.
        /// </summary>
        /// <param name="evt">Change event.</param>
        private void OnOverrideBaseURLValueChanged(
            ChangeEvent<bool> evt)
        {
            _configViewModel.SetShouldOverrideBaseURL(evt.newValue);
            EvaluateBaseURLDisplay(evt.newValue);
        }

        /// <summary>
        /// Handles changes to the base URL override field.
        /// </summary>
        /// <param name="evt">Change event.</param>
        private void OnOverrideBaseURLValueChanged(
            ChangeEvent<string> evt)
        {
            _configViewModel.SetBaseURLOverride(evt.newValue);
        }

        /// <summary>
        /// Handles the click event for the define builds directory button.
        /// </summary>
        private void OnButtonDefineBuildsDirectoryClicked()
        {
            var currentDirectory = _configViewModel.BuildsDirectory;
            if (!Directory.Exists(currentDirectory))
            {
                currentDirectory = Path.GetFullPath(
                    Path.Combine(Application.dataPath, ".."));
            }
            string buildsDirectory = EditorUtility.OpenFolderPanel(
                "Select builds directory", currentDirectory, "");
            if (!string.IsNullOrEmpty(buildsDirectory))
            {
                _fieldBuildsDirectory.SetValueWithoutNotify(buildsDirectory);
                _configViewModel.SetBuildsDirectory(buildsDirectory);
            }
        }

        #endregion

        #region Development Tab Logic

        /// <summary>
        /// Instantiates and delegates the development logic to the DevProcessDevelopmentStepElement.
        /// </summary>
        private void CheckDevelopmentStatus()
        {
            _containerCurrentStep.Clear();
            var developmentStepViewModel = EditorServiceProvider.Instance
                .GetService<DevelopmentStepViewModel>();
            var devStepController = new DevProcessDevelopmentStepController(
                developmentStepViewModel);
            var devStepElement = new DevProcessDevelopmentStepElement(
                developmentStepViewModel,
                devStepController);
            _containerCurrentStep.Add(devStepElement);
        }

        #endregion
    }
}