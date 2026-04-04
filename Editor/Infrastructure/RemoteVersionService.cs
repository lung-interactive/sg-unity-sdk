using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Infrastructure.Http;
using SGUnitySDK.Editor.Infrastructure.Http.Transport;
using UnityEngine;

namespace SGUnitySDK.Editor.Infrastructure
{
    /// <summary>
    /// Concrete implementation of IRemoteVersionService using GameDevelopmentRequest for HTTP operations.
    /// </summary>
    public class RemoteVersionService : IRemoteVersionService
    {
        /// <inheritdoc />
        public async Awaitable<VersionDTO> GetCurrentVersionAsync()
        {
            var response = await GameDevelopmentRequest.GetCurrentVersion();
            return GameDevelopmentTransportMapper.ToDomain(response);
        }

        /// <inheritdoc />
        public async Awaitable<VersionDTO> GetFirstUnderDevelopmentVersionAsync()
        {
            var response = await GameDevelopmentRequest.GetVersionUnderDevelopment();
            return GameDevelopmentTransportMapper.ToDomain(response);
        }

        /// <inheritdoc />
        public async Awaitable<bool> AcknowledgeVersionAsync(string versionId, string notes = null)
        {
            var payload = new AcknowledgeVersionDTO { Notes = notes };
            var transportPayload = GameDevelopmentTransportMapper.ToTransport(payload);
            var transportMetadata = await GameDevelopmentRequest.AcknowledgeVersion(
                versionId,
                transportPayload);
            var metadata = GameDevelopmentTransportMapper.ToDomain(transportMetadata);
            return metadata?.Acknowledgment?.Acknowledged == true;
        }

        /// <inheritdoc />
        public async Awaitable<bool> SendToHomologationAsync(SendToHomologationDTO payload)
        {
            var transportPayload = GameDevelopmentTransportMapper.ToTransport(payload);
            return await GameDevelopmentRequest.SendToHomologation(transportPayload);
        }

        /// <inheritdoc />
        public async Awaitable<VersionDTO[]> FilterVersionsAsync(FilterVersionsDTO filter)
        {
            var transportFilter = GameDevelopmentTransportMapper.ToTransport(filter);
            var response = await GameDevelopmentRequest.FilterVersions(transportFilter);
            return GameDevelopmentTransportMapper.ToDomain(response);
        }

        /// <inheritdoc />
        public async Awaitable<VersionDTO> GetVersionUnderDevelopmentAsync()
        {
            var response = await GameDevelopmentRequest.GetVersionUnderDevelopment();
            return GameDevelopmentTransportMapper.ToDomain(response);
        }

        /// <inheritdoc />
        public async Awaitable<VersionMetadata> GetVersionMetadataAsync(string versionId)
        {
            var response = await GameDevelopmentRequest.GetVersionMetadata(versionId);
            return GameDevelopmentTransportMapper.ToDomain(response);
        }
    }
}
