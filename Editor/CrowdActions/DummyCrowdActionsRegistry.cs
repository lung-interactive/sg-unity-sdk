using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SGUnitySDK.Editor
{
    public class DummyCrowdActionsRegistry : ScriptableObject
    {
        [SerializeField] private List<CrowdActionEntry> _entries = new();

        public IReadOnlyList<CrowdActionEntry> Entries => _entries;

        public static DummyCrowdActionsRegistry Load()
        {
            var registry = Resources.Load<DummyCrowdActionsRegistry>("SGUnitySDK/DummyCrowdActionsRegistry");

            if (registry == null)
            {
                var directory = new System.IO.DirectoryInfo(Application.dataPath + "/Resources/SGUnitySDK");
                if (!directory.Exists) directory.Create();

                registry = ScriptableObject.CreateInstance<DummyCrowdActionsRegistry>();
                AssetDatabase.CreateAsset(registry, "Assets/Resources/SGUnitySDK/DummyCrowdActionsRegistry.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return registry;
        }

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

        public void RemoveAction(GUID guid)
        {
            _entries.RemoveAll(e => e.guid == guid);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        [System.Serializable]
        public class CrowdActionEntry
        {
            public GUID guid;
            public bool neverSaved;
            public CrowdAction crowdAction;
        }
    }
}