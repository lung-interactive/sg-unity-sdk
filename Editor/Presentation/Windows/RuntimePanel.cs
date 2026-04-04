using System;
using System.Collections.Generic;
using HMSUnitySDK.LauncherInteroperations;
using SGUnitySDK.Editor.Infrastructure;
using SGUnitySDK.Editor.Presentation.ViewModels;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor.Presentation.Windows
{
    public class RuntimePanel : EditorWindow
    {
        private RuntimePanelViewModel _viewModel;

        [MenuItem("SGUnitySDK/Runtime Panel", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<RuntimePanel>();
            var viewModel = EditorServiceProvider.Instance
                .GetService<RuntimePanelViewModel>();
            window.titleContent = new GUIContent(viewModel.WindowTitle);
            window.minSize = new Vector2(400, 500);
        }

        private TemplateContainer _containerMain;

        void CreateGUI()
        {
            _viewModel = EditorServiceProvider.Instance
                .GetService<RuntimePanelViewModel>();

            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{_viewModel.TemplatePath}")
                .CloneTree();

            rootVisualElement.Add(_containerMain);
        }
    }
}