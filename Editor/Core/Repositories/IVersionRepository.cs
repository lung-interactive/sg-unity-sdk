using SGUnitySDK.Editor.Core.Entities;

namespace SGUnitySDK.Editor.Core.Repositories
{
    /// <summary>
    /// Repository interface for version management and queries.
    /// </summary>
    public interface IVersionRepository
    {
        /// <summary>
        /// Retrieves a version entity by its identifier.
        /// </summary>
        /// <param name="versionId">The version identifier.</param>
        /// <returns>The version entity.</returns>
        VersionDTO GetVersion(string versionId);

        /// <summary>
        /// Persists a version entity.
        /// </summary>
        /// <param name="version">The version entity to persist.</param>
        void SaveVersion(VersionDTO version);
    }
}
