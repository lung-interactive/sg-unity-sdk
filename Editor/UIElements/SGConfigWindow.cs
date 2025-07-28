using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.IO;
using SGUnitySDK.Editor.Http;
using System;

namespace SGUnitySDK.Editor
{
    public class SGConfigWindow : EditorWindow
    {
        #region Static

        private static readonly string TemplateName = "SGConfigWindow";
        private static SGConfigWindow _window;

        [MenuItem("Tools/SGUnitySDK/Config/Config Panel", false, 0)]
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

        private SerializedObject _serializedConfig;

        // Elements
        private TemplateContainer _containerMain;

        // CONFIG FIELDS
        private TextField _fieldGameManagementToken;
        private Toggle _fieldShouldOverrideBaseURL;
        private TextField _fieldBaseURlOverride;
        private TextField _fieldBuildsDirectory;
        private Button _buttonDefineBuildsDirectory;
        private ListView _listBuildProfiles;

        #endregion

        #region Properties

        private SGEditorConfig Config => SGEditorConfig.instance;

        #endregion

        #region Editor window Cycle

        public void CreateGUI()
        {

            _serializedConfig = new SerializedObject(SGEditorConfig.instance);
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            // Config
            _fieldGameManagementToken = _containerMain.Q<TextField>("field-game-management-token");
            _fieldGameManagementToken.SetValueWithoutNotify(Config.GMT);
            _fieldGameManagementToken.RegisterValueChangedCallback(OnGameManagementTokenValueChanged);

            _fieldShouldOverrideBaseURL = _containerMain.Q<Toggle>("field-should-override-base-url");
            _fieldShouldOverrideBaseURL.SetValueWithoutNotify(Config.ShouldOverrideBaseURL);
            _fieldShouldOverrideBaseURL.RegisterValueChangedCallback(OnOverrideBaseURLValueChanged);

            _fieldBaseURlOverride = _containerMain.Q<TextField>("field-base-url-override");
            _fieldBaseURlOverride.SetValueWithoutNotify(Config.BaseURLOverride);
            _fieldBaseURlOverride.RegisterValueChangedCallback(OnOverrideBaseURLValueChanged);

            _fieldBuildsDirectory = _containerMain.Q<TextField>("field-builds-directory");
            _fieldBuildsDirectory.SetValueWithoutNotify(Config.BuildsDirectory);
            _fieldBuildsDirectory.SetEnabled(false);

            _listBuildProfiles = _containerMain.Q<ListView>("list-build-profiles");
            SetupBuildProfilesList();

            _buttonDefineBuildsDirectory = _containerMain.Q<Button>("button-define-builds-directory");
            _buttonDefineBuildsDirectory.clicked += OnButtonDefineBuildsDirectoryClicked;


            EvaluateBaseURLDisplay(Config.ShouldOverrideBaseURL);

            rootVisualElement.Add(_containerMain);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            Config.Persist();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void SetupBuildProfilesList()
        {
            var property = _serializedConfig.FindProperty("_buildSetups");
            _listBuildProfiles.BindProperty(property);
            _listBuildProfiles.reorderable = true;
            _listBuildProfiles.reorderMode = ListViewReorderMode.Animated;
            _listBuildProfiles.selectionType = SelectionType.Single;
            _listBuildProfiles.showAddRemoveFooter = true;
            _listBuildProfiles.showBorder = true;
            _listBuildProfiles.showFoldoutHeader = false;
            _listBuildProfiles.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
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

        private void OnGameManagementTokenValueChanged(ChangeEvent<string> evt)
        {
            Config.SetGTM(evt.newValue);
        }

        private void OnOverrideBaseURLValueChanged(ChangeEvent<bool> evt)
        {
            Config.SetShouldOverrideBaseURL(evt.newValue);
            EvaluateBaseURLDisplay(evt.newValue);
        }

        private void OnOverrideBaseURLValueChanged(ChangeEvent<string> evt)
        {
            Config.SetBaseUrlOverride(evt.newValue);
        }

        private void OnButtonDefineBuildsDirectoryClicked()
        {
            var currentDirectory = Config.BuildsDirectory;
            if (!Directory.Exists(currentDirectory))
            {
                currentDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            }
            string buildsDirectory = EditorUtility.OpenFolderPanel("Select builds directory", currentDirectory, "");
            if (!string.IsNullOrEmpty(buildsDirectory))
            {
                _fieldBuildsDirectory.SetValueWithoutNotify(buildsDirectory);
                Config.SetBuildsDirectory(buildsDirectory);
            }
        }

        #endregion

        #region Versioning Process

        #endregion

        #region Config Builds

        private void SelectBuildsDirectory(ChangeEvent<string> evt)
        {
            Config.SetBuildsDirectory(evt.newValue);
        }

        #endregion
    }
}