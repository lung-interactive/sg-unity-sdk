using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.Singletons;

namespace SGUnitySDK.Editor.Infrastructure
{
    /// <summary>
    /// Editor persistence implementation for version metadata.
    /// Uses DevelopmentProcess singleton as local storage.
    /// </summary>
    public class EditorMetadataRepository : IMetadataRepository
    {
        /// <inheritdoc />
        public void UpdateMetadata(string versionId, VersionMetadata metadata)
        {
            var process = DevelopmentProcess.instance;
            if (process.CurrentVersion != null && process.CurrentVersion.Id == versionId)
            {
                process.CurrentVersionMetadata = metadata;
                process.Persist();
            }
        }

        /// <inheritdoc />
        public VersionMetadata GetMetadata(string versionId)
        {
            var process = DevelopmentProcess.instance;
            if (process.CurrentVersion != null && process.CurrentVersion.Id == versionId)
            {
                return process.CurrentVersionMetadata;
            }

            return null;
        }
    }
}
