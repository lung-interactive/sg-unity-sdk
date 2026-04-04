using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Versioning;

namespace SGUnitySDK.Editor.Infrastructure.UnityAdapters
{
    /// <summary>
    /// Unity adapter implementation that bridges core progress abstractions
    /// to SGBuildProgress.
    /// </summary>
    public sealed class BuildProgressService : IBuildProgressFactory
    {
        /// <inheritdoc />
        public IBuildProgressReporter Create(
            string title,
            string initialMessage,
            float initialProgress)
        {
            return new BuildProgressReporterAdapter(
                new SGBuildProgress(title, initialMessage, initialProgress));
        }

        /// <summary>
        /// Adapter that wraps SGBuildProgress and exposes IBuildProgressReporter.
        /// </summary>
        private sealed class BuildProgressReporterAdapter : IBuildProgressReporter
        {
            private readonly SGBuildProgress _progress;

            /// <summary>
            /// Initializes a new adapter instance.
            /// </summary>
            /// <param name="progress">Concrete Unity progress reporter instance.</param>
            public BuildProgressReporterAdapter(SGBuildProgress progress)
            {
                _progress = progress;
            }

            /// <inheritdoc />
            public void Report(string message, float overall)
            {
                _progress.Report(message, overall);
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _progress.Dispose();
            }
        }
    }
}