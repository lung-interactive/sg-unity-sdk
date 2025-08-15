using System;
using UnityEditor;
using UnityEngine;

namespace SGUnitySDK.Editor.Versioning
{
    /// <summary>
    /// Unified progress reporter.
    /// - Uses UnityEditor.Progress when available (2020.1+).
    /// - Falls back to EditorUtility.DisplayProgressBar otherwise.
    /// </summary>
    public sealed class SGBuildProgress : IDisposable
    {
#if UNITY_2020_1_OR_NEWER
        private int _progressId = -1;
#endif
        private readonly string _title;
        private string _message = "";
        private float _overall = 0f;
        private bool _disposed;

        public SGBuildProgress(string title, string initialMessage = "", float initialProgress = 0f)
        {
            _title = title ?? "Building";
            _message = initialMessage ?? "";
            _overall = Mathf.Clamp01(initialProgress);

#if UNITY_2020_1_OR_NEWER
            _progressId = Progress.Start(_title, _message, Progress.Options.Unmanaged);
            Progress.Report(_progressId, _overall);
#else
            EditorUtility.DisplayProgressBar(_title, _message, _overall);
#endif
        }

        /// <summary>
        /// Update the message and the overall 0..1 fraction.
        /// </summary>
        public void Report(string message, float overall)
        {
            _message = message ?? _message;
            _overall = Mathf.Clamp01(overall);

#if UNITY_2020_1_OR_NEWER
            if (_progressId >= 0)
            {
                Progress.SetDescription(_progressId, _message);
                Progress.Report(_progressId, _overall);
            }
#else
            EditorUtility.DisplayProgressBar(_title, _message, _overall);
#endif
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

#if UNITY_2020_1_OR_NEWER
            if (_progressId >= 0)
            {
                Progress.Finish(_progressId, Progress.Status.Succeeded);
                _progressId = -1;
            }
#else
            EditorUtility.ClearProgressBar();
#endif
        }
    }
}
