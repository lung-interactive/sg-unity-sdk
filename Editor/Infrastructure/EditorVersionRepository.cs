using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.Singletons;
using UnityEngine;

namespace SGUnitySDK.Editor.Infrastructure
{
    /// <summary>
    /// Concrete implementation of IVersionRepository for Editor local persistence.
    /// Uses DevelopmentProcess singleton to store and retrieve version data.
    /// </summary>
    public class EditorVersionRepository : IVersionRepository
    {
        /// <inheritdoc />
        public VersionDTO GetVersion(string versionId)
        {
            var current = DevelopmentProcess.instance.CurrentVersion;
            if (current != null && current.Id == versionId)
                return current;
            // Extend here for more advanced lookups if needed
            return null;
        }

        /// <inheritdoc />
        public void SaveVersion(VersionDTO version)
        {
            DevelopmentProcess.instance.CurrentVersion = version;
            DevelopmentProcess.instance.Persist();
        }
    }
}
