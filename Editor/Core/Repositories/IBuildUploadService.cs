using System.Threading.Tasks;
using SGUnitySDK.Editor.Core.Entities;
using UnityEngine;

namespace SGUnitySDK.Editor.Core.Repositories
{
    /// <summary>
    /// Service abstraction for the build upload flow.
    /// Provides operations to start, transfer, and confirm build uploads.
    /// </summary>
    public interface IBuildUploadService
    {
        /// <summary>
        /// Starts a build upload session in the backend.
        /// </summary>
        /// <param name="payload">Upload session initialization payload.</param>
        /// <returns>Session data including upload token and presigned URL.</returns>
        Awaitable<StartBuildUploadResponseDTO> StartBuildUploadAsync(
            StartBuildUploadDTO payload);

        /// <summary>
        /// Uploads a local file to the provided presigned URL.
        /// </summary>
        /// <param name="filePath">Absolute local path of the file to upload.</param>
        /// <param name="presignedUrl">Presigned URL for file transfer.</param>
        /// <param name="contentType">Expected content type contract from signed URL.</param>
        /// <returns>True if upload succeeded; otherwise false.</returns>
        Task<bool> UploadFileToPresignedUrlAsync(
            string filePath,
            string presignedUrl,
            string contentType = null);

        /// <summary>
        /// Confirms upload completion in the backend.
        /// </summary>
        /// <param name="payload">Upload confirmation payload.</param>
        /// <returns>True if confirmation succeeded; otherwise false.</returns>
        Awaitable<bool> ConfirmBuildUploadAsync(ConfirmUploadDTO payload);
    }
}
