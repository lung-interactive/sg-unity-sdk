namespace SGUnitySDK.Editor.Core.Repositories
{
    /// <summary>
    /// Factory abstraction for creating progress reporters used in build flows.
    /// </summary>
    public interface IBuildProgressFactory
    {
        /// <summary>
        /// Creates a progress reporter instance.
        /// </summary>
        /// <param name="title">Progress title.</param>
        /// <param name="initialMessage">Initial progress message.</param>
        /// <param name="initialProgress">Initial progress value in range 0..1.</param>
        /// <returns>Progress reporter instance.</returns>
        IBuildProgressReporter Create(
            string title,
            string initialMessage,
            float initialProgress);
    }
}