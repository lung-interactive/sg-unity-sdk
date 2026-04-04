using System;
using System.Threading.Tasks;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.UseCases;
using UnityEngine;

namespace SGUnitySDK.Editor.Presentation.ViewModels
{
    /// <summary>
    /// ViewModel for SGPanelWindow orchestration concerns.
    /// Encapsulates window-level workflows such as synchronization,
    /// server-status fetch and post-reset auto-advance behavior.
    /// </summary>
    public class SGPanelWindowViewModel
    {
        private readonly SyncDevelopmentProcessWithServerUseCase _syncUseCase;
        private readonly IRemoteVersionService _remoteVersionService;
        private readonly DevelopmentProcessStateViewModel _processState;

        /// <summary>
        /// Initializes a new instance of the <see cref="SGPanelWindowViewModel"/> class.
        /// </summary>
        /// <param name="syncUseCase">Use case for local/remote sync.</param>
        /// <param name="remoteVersionService">Remote version service abstraction.</param>
        /// <param name="processState">Process state view model abstraction.</param>
        public SGPanelWindowViewModel(
            SyncDevelopmentProcessWithServerUseCase syncUseCase,
            IRemoteVersionService remoteVersionService,
            DevelopmentProcessStateViewModel processState)
        {
            _syncUseCase = syncUseCase ??
                throw new ArgumentNullException(nameof(syncUseCase));
            _remoteVersionService = remoteVersionService ??
                throw new ArgumentNullException(nameof(remoteVersionService));
            _processState = processState ??
                throw new ArgumentNullException(nameof(processState));
        }

        /// <summary>
        /// Synchronizes local DevelopmentProcess state with backend state.
        /// </summary>
        /// <returns>True if local state changed; otherwise false.</returns>
        public async Task<bool> SyncDevelopmentProcessAsync()
        {
            return await _syncUseCase.ExecuteAsync();
        }

        /// <summary>
        /// Retrieves the backend current version label text.
        /// </summary>
        /// <returns>Current version semver string, or "-" when unavailable.</returns>
        public async Task<string> GetServerCurrentVersionLabelAsync()
        {
            var currentVersion = await _remoteVersionService.GetCurrentVersionAsync();
            return currentVersion?.Semver?.Raw ?? "-";
        }

        /// <summary>
        /// After a process reset, checks server state and advances the
        /// local process to Development when a version is already
        /// acknowledged and under development.
        /// </summary>
        /// <returns>True when local state was advanced; otherwise false.</returns>
        public async Task<bool> AdvanceToDevelopmentIfAcceptedAsync()
        {
            try
            {
                var versions = await _remoteVersionService.FilterVersionsAsync(
                    new FilterVersionsDTO
                    {
                        State = (int)GameVersionState.UnderDevelopment,
                    });

                if (versions == null || versions.Length == 0)
                {
                    return false;
                }

                var version = versions[0];
                var metadata = await _remoteVersionService.GetVersionMetadataAsync(version.Id);

                if (metadata != null &&
                    metadata.Acknowledgment != null &&
                    metadata.Acknowledgment.Acknowledged)
                {
                    _processState.SetCurrentVersion(version);
                    _processState.SetCurrentVersionMetadata(metadata);
                    _processState.MoveToDevelopmentStep();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"Failed to auto-advance to development step: {ex.Message}");
            }

            return false;
        }
    }
}
