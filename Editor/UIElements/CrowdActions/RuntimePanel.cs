using System;
using System.Collections.Generic;
using HMSUnitySDK.LauncherInteroperations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public class RuntimePanel : EditorWindow
    {
        private static readonly string TemplatePath = "RuntimePanel";
        [MenuItem("Tools/SGUnitySDK/RuntimePanel", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<RuntimePanel>();
            window.titleContent = new GUIContent("RuntimePanel");
            window.minSize = new Vector2(400, 500);
        }

        private TemplateContainer _containerMain;

        void CreateGUI()
        {
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplatePath}").CloneTree();

            rootVisualElement.Add(_containerMain);
        }
    }
}