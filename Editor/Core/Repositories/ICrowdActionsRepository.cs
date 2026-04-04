using System.Collections.Generic;
using SGUnitySDK.Editor.Core.Entities;
using UnityEditor;

namespace SGUnitySDK.Editor.Core.Repositories
{
    /// <summary>
    /// Repository abstraction for editor crowd actions registry persistence.
    /// </summary>
    public interface ICrowdActionsRepository
    {
        /// <summary>
        /// Gets all persisted crowd action registry entries.
        /// </summary>
        /// <returns>Read-only collection of entries.</returns>
        IReadOnlyList<CrowdActionRegistryEntry> GetEntries();

        /// <summary>
        /// Persists a new or existing entry.
        /// </summary>
        /// <param name="entry">Entry to save.</param>
        void AddOrUpdateEntry(CrowdActionRegistryEntry entry);

        /// <summary>
        /// Removes an entry by GUID.
        /// </summary>
        /// <param name="guid">Entry GUID.</param>
        void RemoveEntry(GUID guid);
    }
}
