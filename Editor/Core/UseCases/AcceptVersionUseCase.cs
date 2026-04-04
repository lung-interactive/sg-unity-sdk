using System;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using UnityEngine;

namespace SGUnitySDK.Editor.Core.UseCases
{
    /// <summary>
    /// Use case for accepting a version for development.
    /// Orchestrates remote acknowledgment and local persistence.
    /// </summary>
    public class AcceptVersionUseCase
    {
        private readonly IRemoteVersionService _remoteVersionService;
        private readonly IVersionRepository _versionRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptVersionUseCase"/> class.
        /// </summary>
        /// <param name="remoteVersionService">Remote version service abstraction.</param>
        /// <param name="versionRepository">Version repository abstraction.</param>
        public AcceptVersionUseCase(
            IRemoteVersionService remoteVersionService,
            IVersionRepository versionRepository)
        {
            _remoteVersionService = remoteVersionService ??
                throw new ArgumentNullException(nameof(remoteVersionService));
            _versionRepository = versionRepository ??
                throw new ArgumentNullException(nameof(versionRepository));
        }

        /// <summary>
        /// Accepts the specified version by acknowledging it remotely and updating local state.
        /// </summary>
        /// <param name="version">The version entity to accept.</param>
        /// <param name="notes">Optional notes for the acknowledgment.</param>
        /// <returns>True if the operation succeeded; otherwise, false.</returns>
        public async Awaitable<bool> ExecuteAsync(VersionDTO version, string notes = null)
        {
            if (version == null || string.IsNullOrEmpty(version.Id))
                throw new ArgumentException("Version or version ID is null.");

            // Step 1: Acknowledge version remotely
            var acknowledged = await _remoteVersionService.AcknowledgeVersionAsync(version.Id, notes);
            if (!acknowledged)
                return false;

            // Step 2: Persist accepted version locally
            _versionRepository.SaveVersion(version);
            return true;
        }
    }
}
