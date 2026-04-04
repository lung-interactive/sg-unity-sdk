using System;
using System.Collections.Generic;
using SGUnitySDK;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using UnityEditor;

namespace SGUnitySDK.Editor.Core.UseCases
{
    /// <summary>
    /// Use case that orchestrates crowd actions registry CRUD operations.
    /// </summary>
    public sealed class ManageCrowdActionsUseCase
    {
        private readonly ICrowdActionsRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManageCrowdActionsUseCase"/> class.
        /// </summary>
        /// <param name="repository">Crowd actions repository abstraction.</param>
        public ManageCrowdActionsUseCase(ICrowdActionsRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Gets all persisted crowd action entries.
        /// </summary>
        /// <returns>Read-only collection of entries.</returns>
        public IReadOnlyList<CrowdActionRegistryEntry> GetEntries()
        {
            return _repository.GetEntries();
        }

        /// <summary>
        /// Creates and persists a default crowd action entry.
        /// </summary>
        /// <returns>The created entry.</returns>
        public CrowdActionRegistryEntry CreateDefaultEntry()
        {
            var entry = new CrowdActionRegistryEntry
            {
                Guid = GUID.Generate(),
                NeverSaved = true,
                CrowdAction = new CrowdAction
                {
                    identifier = "new-action",
                    name = "New Action",
                    processed_arguments = Array.Empty<ProcessedArgument>(),
                    metadata = new CrowdActionMetadata()
                }
            };

            _repository.AddOrUpdateEntry(entry);
            return entry;
        }

        /// <summary>
        /// Saves an updated crowd action entry.
        /// </summary>
        /// <param name="guid">Entry GUID key.</param>
        /// <param name="crowdAction">Updated crowd action payload.</param>
        public void SaveEntry(GUID guid, CrowdAction crowdAction)
        {
            _repository.AddOrUpdateEntry(new CrowdActionRegistryEntry
            {
                Guid = guid,
                NeverSaved = false,
                CrowdAction = crowdAction
            });
        }

        /// <summary>
        /// Removes an entry by GUID.
        /// </summary>
        /// <param name="guid">Entry GUID key.</param>
        public void RemoveEntry(GUID guid)
        {
            _repository.RemoveEntry(guid);
        }
    }
}
