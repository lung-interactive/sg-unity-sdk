using System.Collections.Generic;
using System.Linq;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using UnityEditor;

namespace SGUnitySDK.Editor.Infrastructure
{
    /// <summary>
    /// Infrastructure repository implementation backed by DummyCrowdActionsRegistry asset.
    /// </summary>
    public sealed class EditorCrowdActionsRepository : ICrowdActionsRepository
    {
        private readonly DummyCrowdActionsRegistry _registry;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorCrowdActionsRepository"/> class.
        /// </summary>
        public EditorCrowdActionsRepository()
        {
            _registry = DummyCrowdActionsRegistry.Load();
        }

        /// <inheritdoc />
        public IReadOnlyList<CrowdActionRegistryEntry> GetEntries()
        {
            return _registry.Entries
                .Select(ToEntity)
                .ToList();
        }

        /// <inheritdoc />
        public void AddOrUpdateEntry(CrowdActionRegistryEntry entry)
        {
            _registry.AddOrUpdateEntry(new DummyCrowdActionsRegistry.CrowdActionEntry
            {
                guid = entry.Guid,
                neverSaved = entry.NeverSaved,
                crowdAction = entry.CrowdAction
            });
        }

        /// <inheritdoc />
        public void RemoveEntry(GUID guid)
        {
            _registry.RemoveAction(guid);
        }

        private static CrowdActionRegistryEntry ToEntity(
            DummyCrowdActionsRegistry.CrowdActionEntry source)
        {
            return new CrowdActionRegistryEntry
            {
                Guid = source.guid,
                NeverSaved = source.neverSaved,
                CrowdAction = source.crowdAction
            };
        }
    }
}
