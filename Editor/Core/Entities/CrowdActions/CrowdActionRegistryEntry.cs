using SGUnitySDK;
using UnityEditor;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Domain entity representing one crowd action registry entry.
    /// </summary>
    public sealed class CrowdActionRegistryEntry
    {
        /// <summary>
        /// Unique identifier for the entry.
        /// </summary>
        public GUID Guid;

        /// <summary>
        /// Indicates if this entry was never persisted before.
        /// </summary>
        public bool NeverSaved;

        /// <summary>
        /// Crowd action payload associated with this entry.
        /// </summary>
        public CrowdAction CrowdAction;
    }
}
