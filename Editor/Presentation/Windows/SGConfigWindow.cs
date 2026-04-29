using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using System;
using HMSUnitySDK;
using SGUnitySDK.Editor.Infrastructure;
using SGUnitySDK.Editor.Presentation.ViewModels;

namespace SGUnitySDK.Editor.Presentation.Windows
{
    public class SGConfigWindow : EditorWindow
    {
        #region Static

        private static readonly string TemplateName = "SGConfigWindow";
        private static SGConfigWindow _window;

        [MenuItem("SGUnitySDK/Config/Panel", false, 0)]
        public static void ShowWindow()
        {
            if (_window == null)
            {
                _window = GetWindow<SGConfigWindow>();
                _window.titleContent = new GUIContent("Streaming Games Configuration");
                _window.minSize = new Vector2(400, 300);
            }
            else
            {
                _window.Show();
            }
        }

        #endregion

        #region Fields

        private SGConfigWindowViewModel _configViewModel;
        private SerializedObject _serializedConfig;

        // Elements
        private TemplateContainer _containerMain;

        // CONFIG FIELDS
        private TextField _fieldGameDevelopmentToken;
        private Toggle _fieldShouldOverrideBaseURL;
        private TextField _fieldBaseURlOverride;
        private ObjectField _fieldRuntimeProfile;
        private TextField _fieldBuildsDirectory;
        private Button _buttonDefineBuildsDirectory;
        private ListView _listServerBuildProfiles;
        private ListView _listClientBuildProfiles;

        #endregion

        #region Editor window Cycle

        public void CreateGUI()
        {
            _configViewModel = EditorServiceProvider.Instance
                .GetService<SGConfigWindowViewModel>();

            _serializedConfig = _configViewModel.CreateSerializedConfig();
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            // Config
            _fieldGameDevelopmentToken = _containerMain.Q<TextField>("field-game-management-token");
            _fieldGameDevelopmentToken.SetValueWithoutNotify(_configViewModel.GameDevelopmentToken);
            _fieldGameDevelopmentToken.RegisterValueChangedCallback(OnGameDevelopmentTokenValueChanged);

            _fieldShouldOverrideBaseURL = _containerMain.Q<Toggle>("field-should-override-base-url");
            _fieldShouldOverrideBaseURL.SetValueWithoutNotify(_configViewModel.ShouldOverrideBaseURL);
            _fieldShouldOverrideBaseURL.RegisterValueChangedCallback(OnOverrideBaseURLValueChanged);

            _fieldRuntimeProfile = _containerMain.Q<ObjectField>("field-runtime-profile");
            _fieldRuntimeProfile.SetValueWithoutNotify(_configViewModel.RuntimeProfile);
            _fieldRuntimeProfile.RegisterValueChangedCallback(OnRuntimeProfileValueChanged);

            _fieldBaseURlOverride = _containerMain.Q<TextField>("field-base-url-override");
            _fieldBaseURlOverride.SetValueWithoutNotify(_configViewModel.BaseURLOverride);
            _fieldBaseURlOverride.RegisterValueChangedCallback(OnOverrideBaseURLValueChanged);

            _fieldBuildsDirectory = _containerMain.Q<TextField>("field-builds-directory");
            _fieldBuildsDirectory.SetValueWithoutNotify(_configViewModel.BuildsDirectory);
            _fieldBuildsDirectory.SetEnabled(false);

            _listServerBuildProfiles =
                _containerMain.Q<ListView>("list-server-build-profiles");
            _listClientBuildProfiles =
                _containerMain.Q<ListView>("list-client-build-profiles");
            SetupBuildProfilesLists();

            _buttonDefineBuildsDirectory = _containerMain.Q<Button>("button-define-builds-directory");
            _buttonDefineBuildsDirectory.clicked += OnButtonDefineBuildsDirectoryClicked;


            EvaluateBaseURLDisplay(_configViewModel.ShouldOverrideBaseURL);

            rootVisualElement.Add(_containerMain);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            _listServerBuildProfiles?.Unbind();
            _listClientBuildProfiles?.Unbind();
            _configViewModel?.Persist();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void SetupBuildProfilesLists()
        {
            SetupBuildProfilesList(
                _listServerBuildProfiles,
                "_serverBuildSetups",
                "SGConfigWindow server");

            SetupBuildProfilesList(
                _listClientBuildProfiles,
                "_clientBuildSetups",
                "SGConfigWindow client");
        }

        private void SetupBuildProfilesList(
            ListView listView,
            string propertyName,
            string contextName)
        {
            if (_serializedConfig == null || listView == null)
            {
                return;
            }

            _serializedConfig.UpdateIfRequiredOrScript();
            var property = _serializedConfig.FindProperty(propertyName);

            if (property == null || !property.isArray)
            {
                Debug.LogWarning(
                    $"{contextName} could not bind {propertyName} property.");
                return;
            }

            listView.Unbind();
            listView.BindProperty(property);
            listView.reorderable = true;
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.selectionType = SelectionType.Single;
            listView.showAddRemoveFooter = true;
            listView.showBorder = true;
            listView.showFoldoutHeader = false;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        }

        #endregion

        #region Display Evaluations

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

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
        }

        private void OnGameDevelopmentTokenValueChanged(ChangeEvent<string> evt)
        {
            _configViewModel.SetGameDevelopmentToken(evt.newValue);
        }

        private void OnOverrideBaseURLValueChanged(ChangeEvent<bool> evt)
        {
            _configViewModel.SetShouldOverrideBaseURL(evt.newValue);
            EvaluateBaseURLDisplay(evt.newValue);
        }

        private void OnOverrideBaseURLValueChanged(ChangeEvent<string> evt)
        {
            _configViewModel.SetBaseURLOverride(evt.newValue);
        }

        private void OnRuntimeProfileValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            _configViewModel.SetRuntimeProfile(evt.newValue as HMSRuntimeProfile);
        }

        private void OnButtonDefineBuildsDirectoryClicked()
        {
            var currentDirectory = _configViewModel.BuildsDirectory;
            if (!Directory.Exists(currentDirectory))
            {
                currentDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            }
            string buildsDirectory = EditorUtility.OpenFolderPanel("Select builds directory", currentDirectory, "");
            if (!string.IsNullOrEmpty(buildsDirectory))
            {
                _fieldBuildsDirectory.SetValueWithoutNotify(buildsDirectory);
                _configViewModel.SetBuildsDirectory(buildsDirectory);
            }
        }

        #endregion

        #region Versioning Process

        #endregion

        #region Config Builds

        private void SelectBuildsDirectory(ChangeEvent<string> evt)
        {
            _configViewModel.SetBuildsDirectory(evt.newValue);
        }

        #endregion
    }
}