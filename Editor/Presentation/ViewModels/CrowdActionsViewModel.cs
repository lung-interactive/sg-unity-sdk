using System.Collections.Generic;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.UseCases;
using UnityEditor;

namespace SGUnitySDK.Editor.Presentation.ViewModels
{
    /// <summary>
    /// ViewModel responsible for CRUD operations over editor crowd actions
    /// registry entries used by runtime panel presentation elements.
    /// </summary>
    public sealed class CrowdActionsViewModel
    {
        private readonly ManageCrowdActionsUseCase _useCase;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrowdActionsViewModel"/> class.
        /// </summary>
        public CrowdActionsViewModel(ManageCrowdActionsUseCase useCase)
        {
            _useCase = useCase;
        }

        /// <summary>
        /// Gets all registered crowd action entries.
        /// </summary>
        /// <returns>Read-only collection of registry entries.</returns>
        public IReadOnlyList<CrowdActionRegistryEntry> GetEntries()
        {
            return _useCase.GetEntries();
        }

        /// <summary>
        /// Creates and persists a new default crowd action entry.
        /// </summary>
        /// <returns>The created registry entry.</returns>
        public CrowdActionRegistryEntry CreateNewEntry()
        {
            return _useCase.CreateDefaultEntry();
        }

        /// <summary>
        /// Saves an updated crowd action entry.
        /// </summary>
        /// <param name="entryGuid">Entry GUID key.</param>
        /// <param name="updatedAction">Updated crowd action payload.</param>
        public void SaveEntry(GUID entryGuid, CrowdAction updatedAction)
        {
            _useCase.SaveEntry(entryGuid, updatedAction);
        }

        /// <summary>
        /// Removes a crowd action entry by GUID.
        /// </summary>
        /// <param name="guid">Entry GUID to remove.</param>
        public void RemoveEntry(GUID guid)
        {
            _useCase.RemoveEntry(guid);
        }
    }
}