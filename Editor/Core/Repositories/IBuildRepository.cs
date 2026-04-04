namespace SGUnitySDK.Editor.Core.Repositories
{
    /// <summary>
    /// Repository interface for build operations and persistence.
    /// </summary>
    public interface IBuildRepository
    {
        /// <summary>
        /// Persists a local build result.
        /// </summary>
        /// <param name="buildResult">The build result to persist.</param>
        void SaveBuildResult(Core.Entities.SGLocalBuildResult buildResult);

        /// <summary>
        /// Retrieves a build result by version.
        /// </summary>
        /// <param name="version">The version identifier.</param>
        /// <returns>The build result entity.</returns>
        Core.Entities.SGLocalBuildResult GetBuildResult(string version);
    }
}
