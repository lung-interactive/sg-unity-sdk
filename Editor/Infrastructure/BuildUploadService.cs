using System.Threading.Tasks;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Infrastructure.Http;
using SGUnitySDK.Editor.Infrastructure.Http.Transport;
using UnityEngine;

namespace SGUnitySDK.Editor.Infrastructure
{
    /// <summary>
    /// Infrastructure implementation for build upload operations.
    /// Wraps API calls and presigned URL file transfer details.
    /// </summary>
    public class BuildUploadService : IBuildUploadService
    {
        /// <inheritdoc />
        public async Awaitable<StartBuildUploadResponseDTO> StartBuildUploadAsync(
            StartBuildUploadDTO payload)
        {
            var transportPayload = GameDevelopmentTransportMapper.ToTransport(payload);
            var transportResponse = await GameDevelopmentRequest.StartBuildUpload(
                transportPayload);
            return GameDevelopmentTransportMapper.ToDomain(transportResponse);
        }

        /// <inheritdoc />
        public async Task<bool> UploadFileToPresignedUrlAsync(
            string filePath,
            string presignedUrl)
        {
            return await S3Uploader.UploadFileToPresignedUrl(filePath, presignedUrl);
        }

        /// <inheritdoc />
        public async Awaitable<bool> ConfirmBuildUploadAsync(
            ConfirmUploadDTO payload)
        {
            var transportPayload = GameDevelopmentTransportMapper.ToTransport(payload);
            return await GameDevelopmentRequest.ConfirmBuildUpload(transportPayload);
        }
    }
}
