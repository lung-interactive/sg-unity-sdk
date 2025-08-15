using System;
using UnityEditor;
using UnityEngine;

namespace SGUnitySDK.Editor.Utils
{
    /// <summary>
    /// Thread-safe bridge to update EditorUtility progress bar
    /// from background threads. Only touches Editor API on main thread.
    /// </summary>
    public static class EditorProgressBridge
    {
        private static bool _active;
        private static float _progress;      // 0..1
        private static string _title = "";
        private static string _message = "";
        private static bool _dirty;

        /// <summary>
        /// Start the polling loop on the main thread.
        /// </summary>
        public static void Start(string title, string message, float initial = 0f)
        {
            _title = title ?? "";
            _message = message ?? "";
            _progress = Mathf.Clamp01(initial);
            _dirty = true;

            if (_active) return;
            _active = true;
            EditorApplication.update += OnUpdate;
        }

        /// <summary>
        /// Report new values from any thread.
        /// </summary>
        public static void Report(string message, float progress)
        {
            _message = message ?? "";
            _progress = Mathf.Clamp01(progress);
            _dirty = true;
        }

        /// <summary>
        /// Stop the polling loop and clear the bar.
        /// </summary>
        public static void Stop()
        {
            if (!_active) return;
            _active = false;
            EditorApplication.update -= OnUpdate;
            EditorUtility.ClearProgressBar();
        }

        private static void OnUpdate()
        {
            if (!_dirty) return;
            _dirty = false;
            EditorUtility.DisplayProgressBar(_title, _message, _progress);
        }
    }
}
