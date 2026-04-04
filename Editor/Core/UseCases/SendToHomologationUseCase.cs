using System;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.Singletons;
using UnityEngine;

namespace SGUnitySDK.Editor.Core.UseCases
{
    /// <summary>
    /// Use case for sending a version to homologation (admin review).
    /// </summary>
    public class SendToHomologationUseCase
    {
        private readonly IRemoteVersionService _remoteVersionService;
        private readonly IVersionRepository _versionRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendToHomologationUseCase"/> class.
        /// </summary>
        /// <param name="remoteVersionService">Remote version service abstraction.</param>
        /// <param name="versionRepository">Version repository abstraction.</param>
        public SendToHomologationUseCase(
            IRemoteVersionService remoteVersionService,
            IVersionRepository versionRepository)
        {
            _remoteVersionService = remoteVersionService ??
                throw new ArgumentNullException(nameof(remoteVersionService));
            _versionRepository = versionRepository ??
                throw new ArgumentNullException(nameof(versionRepository));
        }

        /// <summary>
        /// Executes the logic to send a version to homologation.
        /// Calls remote API to end preparation and persists state locally when successful.
        /// </summary>
        /// <param name="version">The version entity to send.</param>
        /// <returns>True if succeeded; otherwise false.</returns>
        public async Awaitable<bool> ExecuteAsync(VersionDTO version)
        {
            if (version == null || string.IsNullOrEmpty(version.Semver?.Raw))
                throw new ArgumentException("Version or semver is null.");

            var payload = new SendToHomologationDTO
            {
                Semver = version.Semver.Raw
            };

            var result = await _remoteVersionService.SendToHomologationAsync(payload);
            if (result)
            {
                // Persist state locally if needed (repository may implement additional logic)
                _versionRepository.SaveVersion(version);

                // Update the local development process to reflect that the
                // version was started/confirmed in the remote system.
                // This ensures UI and other workflows read the correct state.
                try
                {
                    var process = DevelopmentProcess.instance;
                    if (process != null)
                    {
                        process.StartedInRemote = true;
                        process.RemoteSemver = version.Semver.Raw;
                        process.CurrentVersion = version;

                        // Advance the development step to Homologation so the
                        // UI and other systems reflect the new state.
                        process.SetStep(DevelopmentStep.Homologation);
                    }
                }
                catch (Exception ex)
                {
                    SGLogger.LogError($"Failed to update DevelopmentProcess after send-to-homologation: {ex.Message}");
                }
            }

            return result;
        }
    }
}
