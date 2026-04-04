using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SGUnitySDK.Editor
{
    /// <summary>
    /// Registry for managing dummy crowd actions in the editor.
    /// Provides functionality to load, add, update, and remove crowd action entries.
    /// Acts as a ScriptableObject that persists crowd action configurations.
    /// </summary>
    public class DummyCrowdActionsRegistry : ScriptableObject
    {
        [SerializeField] private List<CrowdActionEntry> _entries = new();

        /// <summary>
        /// Read-only access to the list of crowd action entries.
        /// </summary>
        public IReadOnlyList<CrowdActionEntry> Entries => _entries;

        /// <summary>
        /// Loads the DummyCrowdActionsRegistry from resources.
        /// If the registry doesn't exist, creates a new instance and saves it as an asset.
        /// Ensures the registry is always available for crowd action management.
        /// </summary>
        /// <returns>The loaded or newly created registry instance.</returns>
        public static DummyCrowdActionsRegistry Load()
        {
            var registry = Resources.Load<DummyCrowdActionsRegistry>(
                "SGUnitySDK/DummyCrowdActionsRegistry");

            if (registry == null)
            {
                var directory = new System.IO.DirectoryInfo(
                    Application.dataPath + "/SGUnitySDK/Editor/Resources/SGUnitySDK");
                if (!directory.Exists) directory.Create();

                registry = ScriptableObject.CreateInstance<DummyCrowdActionsRegistry>();
                AssetDatabase.CreateAsset(
                    registry,
                    "Assets/SGUnitySDK/Editor/Resources/SGUnitySDK/DummyCrowdActionsRegistry.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return registry;
        }

        /// <summary>
        /// Adds a new crowd action entry or updates an existing one with the same GUID.
        /// Maintains uniqueness by GUID and marks the registry as dirty for saving.
        /// </summary>
        /// <param name="entry">The crowd action entry to add or update.</param>
        public void AddOrUpdateEntry(CrowdActionEntry entry)
        {
            int index = _entries.FindIndex(e => e.guid == entry.guid);

            if (index >= 0)
            {
                _entries[index] = entry;
            }
            else
            {
                _entries.Add(entry);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Removes a crowd action entry by its GUID.
        /// Removes all entries matching the specified GUID and saves changes.
        /// </summary>
        /// <param name="guid">The GUID of the entry to remove.</param>
        public void RemoveAction(GUID guid)
        {
            _entries.RemoveAll(e => e.guid == guid);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Serializable data structure representing a crowd action entry.
        /// Contains the unique identifier, save status, and the associated crowd action.
        /// </summary>
        [System.Serializable]
        public class CrowdActionEntry
        {
            /// <summary>
            /// Unique GUID identifier for this crowd action entry.
            /// </summary>
            public GUID guid;

            /// <summary>
            /// Flag indicating if this action has never been saved.
            /// Used to track new or unsaved entries.
            /// </summary>
            public bool neverSaved;

            /// <summary>
            /// The actual crowd action object associated with this entry.
            /// </summary>
            public CrowdAction crowdAction;
        }
    }
}