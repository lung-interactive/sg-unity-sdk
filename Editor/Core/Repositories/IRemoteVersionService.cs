using SGUnitySDK.Editor.Core.Entities;
using UnityEngine;

namespace SGUnitySDK.Editor.Core.Repositories
{
    /// <summary>
    /// Service interface for remote version management and operations.
    /// Handles HTTP/API communication for version-related actions.
    /// </summary>
    public interface IRemoteVersionService
    {
        /// <summary>
        /// Retrieves the version currently in preparation
        /// (awaiting development acknowledgment).
        /// </summary>
        /// <returns>The version in preparation when available; otherwise null.</returns>
        Awaitable<VersionDTO> GetVersionInPreparationAsync();

        /// <summary>
        /// Retrieves the version currently marked as current in the backend.
        /// </summary>
        /// <returns>Current version when available; otherwise null.</returns>
        Awaitable<VersionDTO> GetCurrentVersionAsync();

        /// <summary>
        /// Retrieves the first version in the 'UnderDevelopment' state from the remote server.
        /// </summary>
        /// <returns>The version entity if found; otherwise, null.</returns>
        Awaitable<VersionDTO> GetFirstUnderDevelopmentVersionAsync();

        /// <summary>
        /// Sends an acknowledgment to accept a version remotely.
        /// </summary>
        /// <param name="versionId">The unique identifier of the version to acknowledge.</param>
        /// <param name="notes">Optional notes or reason for acknowledgment.</param>
        /// <returns>True if the operation succeeded; otherwise, false.</returns>
        Awaitable<bool> AcknowledgeVersionAsync(string versionId, string notes = null);

        /// <summary>
        /// Sends the version to homologation on the remote server using the
        /// newer `send-to-homologation` endpoint.
        /// </summary>
        /// <param name="payload">Payload containing semver to send to homologation.</param>
        /// <returns>True if the operation succeeded; otherwise false.</returns>
        Awaitable<bool> SendToHomologationAsync(SendToHomologationDTO payload);

        /// <summary>
        /// Filters versions using optional criteria provided by the caller.
        /// </summary>
        /// <param name="filter">Filter payload for version queries.</param>
        /// <returns>Array of versions that match the filter.</returns>
        Awaitable<VersionDTO[]> FilterVersionsAsync(FilterVersionsDTO filter);

        /// <summary>
        /// Retrieves the version that is currently under development.
        /// </summary>
        /// <returns>The under-development version when available; otherwise null.</returns>
        Awaitable<VersionDTO> GetVersionUnderDevelopmentAsync();

        /// <summary>
        /// Retrieves metadata for the provided version identifier.
        /// </summary>
        /// <param name="versionId">Target version identifier.</param>
        /// <returns>Version metadata when available; otherwise null.</returns>
        Awaitable<VersionMetadata> GetVersionMetadataAsync(string versionId);
    }
}
