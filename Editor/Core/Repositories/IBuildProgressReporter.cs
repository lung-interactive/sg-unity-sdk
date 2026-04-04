using System;

namespace SGUnitySDK.Editor.Core.Repositories
{
    /// <summary>
    /// Runtime-agnostic progress reporter abstraction for build workflows.
    /// </summary>
    public interface IBuildProgressReporter : IDisposable
    {
        /// <summary>
        /// Reports the current progress state.
        /// </summary>
        /// <param name="message">Human-readable progress message.</param>
        /// <param name="overall">Overall progress value in range 0..1.</param>
        void Report(string message, float overall);
    }
}