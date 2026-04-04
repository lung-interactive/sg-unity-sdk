namespace SGUnitySDK.Editor.Core.Repositories
{
    /// <summary>
    /// Repository interface for version metadata operations.
    /// </summary>
    public interface IMetadataRepository
    {
        /// <summary>
        /// Updates the metadata for a given version.
        /// </summary>
        /// <param name="versionId">The version identifier.</param>
        /// <param name="metadata">The new metadata to apply.</param>
        void UpdateMetadata(string versionId, Core.Entities.VersionMetadata metadata);

        /// <summary>
        /// Retrieves the metadata for a given version.
        /// </summary>
        /// <param name="versionId">The version identifier.</param>
        /// <returns>The version metadata entity.</returns>
        Core.Entities.VersionMetadata GetMetadata(string versionId);
    }
}
