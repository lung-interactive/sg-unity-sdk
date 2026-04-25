using SGUnitySDK.Http;
using UnityEngine;
using System;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Singletons;
using SGUnitySDK.Editor.Infrastructure.Http.Transport;

namespace SGUnitySDK.Editor.Infrastructure.Http
{
    /// <summary>
    /// Provides static methods for game development API requests.
    /// All methods require a valid Game Development Token configured in SGEditorConfig.
    /// </summary>
    public static class GameDevelopmentRequest
    {
        #region Base Request Creation

        /// <summary>
        /// Creates and returns a new request setting its base url and HttpMethod.
        /// </summary>
        /// <param name="endpoint">API endpoint (e.g., "validate-token")</param>
        /// <param name="method">HTTP method to use</param>
        /// <returns>Configured SGHttpRequest</returns>
        /// <exception cref="InvalidGMTException">Thrown when GMT is not configured</exception>
        public static SGHttpRequest To(string endpoint, HttpMethod method = HttpMethod.Get)
        {
            if (!SGEditorConfig.instance.IsGMTValid)
            {
                throw new InvalidGMTException(endpoint);
            }

            if (endpoint.StartsWith("/"))
            {
                endpoint = endpoint[1..];
            }

            string url = $"{SGEditorConfig.instance.ApiBaseURL}/game-development/{endpoint}";
            SGHttpRequest request = new();

            request.SetUrl(url);
            request.SetMethod(method);
            request.AddHeader("Content-Type", "application/json");
            request.SetBearerAuth(SGEditorConfig.instance.GameDevelopmentToken);

            return request;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates that the provided management token is valid and active.
        /// </summary>
        /// <returns>True if token is valid, false otherwise</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails</exception>
        public static async Awaitable<bool> ValidateToken()
        {
            try
            {
                var response = await To("validate-token", HttpMethod.Get).SendAsync();

                if (!response.Success)
                {
                    var errorBody = response.ReadErrorBody();
                    SGLogger.LogError($"Token validation failed: {string.Join(", ", errorBody.Messages)}");
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                return response.ReadBodyData<bool>();
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error validating token: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Version Queries

        /// <summary>
        /// Returns the version marked as current (is_current = true) for the game.
        /// </summary>
        /// <returns>Current VersionDTO or null if not found</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails</exception>
        public static async Awaitable<TransportVersionResponse> GetCurrentVersion()
        {
            try
            {
                var response = await To("versions", HttpMethod.Get).SendAsync();

                if (!response.Success)
                {
                    var errorBody = response.ReadErrorBody();
                    SGLogger.LogError($"Failed to get current version: {string.Join(", ", errorBody.Messages)}");
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                return response.ReadBodyData<TransportVersionResponse>();
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error getting current version: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns the version with the highest semantic version number.
        /// </summary>
        /// <returns>Latest VersionDTO or null if not found</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails</exception>
        public static async Awaitable<TransportVersionResponse> GetLatestVersion()
        {
            try
            {
                var response = await To("versions/latest", HttpMethod.Get).SendAsync();

                if (!response.Success)
                {
                    var errorBody = response.ReadErrorBody();
                    SGLogger.LogError($"Failed to get latest version: {string.Join(", ", errorBody.Messages)}");
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                return response.ReadBodyData<TransportVersionResponse>();
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error getting latest version: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns the version in AwaitingDevelopmentApproval state if one exists.
        /// </summary>
        /// <returns>VersionDTO in preparation or null if none exists</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails (except 404)</exception>
        public static async Awaitable<TransportVersionResponse> GetVersionInPreparation()
        {
            try
            {
                var response = await To("versions/in-preparation", HttpMethod.Get).SendAsync();

                if (!response.Success)
                {
                    // 404 is expected when no version is in preparation
                    if (response.ResponseCode == 404)
                    {
                        return null;
                    }

                    var errorBody = response.ReadErrorBody();
                    SGLogger.LogError($"Failed to get version in preparation: {string.Join(", ", errorBody.Messages)}");
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                return response.ReadBodyData<TransportVersionResponse>();
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error getting version in preparation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Filters game versions by multiple optional criteria.
        /// All parameters are combined using AND logic.
        /// </summary>
        /// <param name="filter">Filter criteria object with optional parameters</param>
        /// <returns>Array of filtered VersionDTO objects</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails</exception>
        public static async Awaitable<TransportVersionResponse[]> FilterVersions(
            TransportVersionFilterQuery filter)
        {
            try
            {
                var request = To("versions/filter", HttpMethod.Get);

                if (filter != null)
                {
                    if (filter.State.HasValue)
                        request.AddQueryEntry("state", filter.State.Value.ToString());

                    if (filter.IsCurrent.HasValue)
                        request.AddQueryEntry("is_current", filter.IsCurrent.Value.ToString().ToLower());

                    if (filter.IsPrerelease.HasValue)
                        request.AddQueryEntry("is_prerelease", filter.IsPrerelease.Value.ToString().ToLower());

                    if (!string.IsNullOrEmpty(filter.CreatedAfter))
                        request.AddQueryEntry("created_after", filter.CreatedAfter);

                    if (!string.IsNullOrEmpty(filter.CreatedBefore))
                        request.AddQueryEntry("created_before", filter.CreatedBefore);

                    if (!string.IsNullOrEmpty(filter.SemverRaw))
                        request.AddQueryEntry("semver_raw", filter.SemverRaw);
                }

                var response = await request.SendAsync();

                if (!response.Success)
                {
                    var errorBody = response.ReadErrorBody();
                    string message =
                        errorBody.Messages != null && errorBody.Messages.Length > 0
                            ? string.Join(", ", errorBody.Messages)
                            : response.HttpErrorMessage;
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        message = $"HTTP {response.ResponseCode}";
                    }
                    SGLogger.LogError(
                        $"Failed to filter versions: {message}"
                    );
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                return response.ReadBodyData<TransportVersionResponse[]>();
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error filtering versions: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns the version in UnderDevelopment state if one exists.
        /// </summary>
        /// <returns>VersionDTO under development or null if none exists</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails (except 404)</exception>
        public static async Awaitable<TransportVersionResponse> GetVersionUnderDevelopment()
        {
            try
            {
                var response = await To("versions/under-development", HttpMethod.Get).SendAsync();

                if (!response.Success)
                {
                    // 404 is expected when no version is under development
                    if (response.ResponseCode == 404)
                        return null;

                    var errorBody = response.ReadErrorBody();
                    SGLogger.LogError($"Failed to get version under development: {string.Join(", ", errorBody.Messages)}");
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                return response.ReadBodyData<TransportVersionResponse>();
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error getting version under development: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Version Management

        /// <summary>
        /// Cancels and deletes the version in preparation along with all associated builds.
        /// </summary>
        /// <returns>True if successfully canceled</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails</exception>
        public static async Awaitable<bool> CancelVersionInPreparation()
        {
            try
            {
                var response = await To("versions/cancel-in-preparation", HttpMethod.Delete).SendAsync();

                if (!response.Success)
                {
                    var errorBody = response.ReadErrorBody();
                    SGLogger.LogError($"Failed to cancel version in preparation: {string.Join(", ", errorBody.Messages)}");
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                SGLogger.Log("Version in preparation canceled successfully");
                return true;
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error canceling version: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the metadata structure containing acknowledgment, development 
        /// status, testing info, and deployment metadata for a version.
        /// </summary>
        /// <param name="versionId">Version UUID (v4)</param>
        /// <returns>Strongly-typed VersionMetadata object or null if not found</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails</exception>
        public static async Awaitable<TransportVersionMetadataResponse> GetVersionMetadata(
            string versionId)
        {
            try
            {
                var response = await To($"versions/{versionId}/metadata", HttpMethod.Get)
                    .SendAsync();

                if (!response.Success)
                {
                    var errorBody = response.ReadErrorBody();
                    SGLogger.LogError(
                        $"Failed to get version metadata: {string.Join(", ", errorBody.Messages)}"
                    );
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                return response.ReadBodyData<TransportVersionMetadataResponse>();
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error getting version metadata: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Marks a version as acknowledged by the development team. Updates 
        /// metadata to set acknowledged flag, record timestamp, and change 
        /// development status to InProgress.
        /// </summary>
        /// <param name="versionId">Version UUID (v4)</param>
        /// <param name="payload">Acknowledgment parameters including optional notes</param>
        /// <returns>Updated version metadata with acknowledgment information</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails</exception>
        public static async Awaitable<TransportVersionMetadataResponse> AcknowledgeVersion(
            string versionId,
            TransportAcknowledgeVersionRequest payload
        )
        {
            try
            {
                var response = await To($"versions/{versionId}/acknowledge", HttpMethod.Post)
                    .SetBody(payload)
                    .SendAsync();

                if (!response.Success)
                {
                    var errorBody = response.ReadErrorBody();
                    SGLogger.LogError(
                        $"Failed to acknowledge version: {string.Join(", ", errorBody.Messages)}"
                    );
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                SGLogger.Log("Version acknowledged successfully");
                return response.ReadBodyData<TransportVersionMetadataResponse>();
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error acknowledging version: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Finalizes version preparation and transitions to Homologation state.
        /// Requires all builds to be active. This endpoint was renamed on the server
        /// and now expects a `SendToHomologationDTO` payload.
        /// </summary>
        /// <param name="payload">Send-to-homologation payload containing semver.</param>
        /// <returns>True if successfully transitioned to homologation</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails</exception>
        public static async Awaitable<bool> SendToHomologation(
            TransportSendToHomologationRequest payload)
        {
            try
            {
                var response = await To("versions/send-to-homologation", HttpMethod.Post)
                    .SetBody(payload)
                    .SendAsync();

                if (!response.Success)
                {
                    var errorBody = response.ReadErrorBody();
                    SGLogger.LogError(
                        $"Failed to send version to homologation {payload.Semver}: {string.Join(", ", errorBody.Messages)}"
                    );
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                SGLogger.Log($"Version {payload.Semver} transitioned to homologation successfully");
                return true;
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error sending version to homologation: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Build Upload

        /// <summary>
        /// Initiates build upload process. Returns presigned S3 URL and upload token.
        /// </summary>
        /// <param name="payload">Build upload metadata</param>
        /// <returns>Upload response with presigned URL and token</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails</exception>
        public static async Awaitable<TransportStartBuildUploadResponse> StartBuildUpload(
            TransportStartBuildUploadRequest payload)
        {
            try
            {
                var response = await To("start-build-upload", HttpMethod.Post)
                    .SetBody(payload)
                    .SendAsync();

                if (!response.Success)
                {
                    var errorBody = response.ReadErrorBody();
                    SGLogger.LogError($"Failed to start build upload for {payload.Platform}: {string.Join(", ", errorBody.Messages)}");
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                var uploadResponse = response.ReadBodyData<TransportStartBuildUploadResponse>();
                SGLogger.Log($"Build upload started for {payload.Platform} - Token: {uploadResponse.UploadToken}");
                return uploadResponse;
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error starting build upload: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Confirms that the build was successfully uploaded to S3.
        /// Validates file existence and transitions build to active state.
        /// </summary>
        /// <param name="payload">Upload confirmation data</param>
        /// <returns>True if upload confirmed successfully</returns>
        /// <exception cref="RequestFailedException">Thrown when request fails</exception>
        public static async Awaitable<bool> ConfirmBuildUpload(
            TransportConfirmUploadRequest payload)
        {
            try
            {
                var response = await To("confirm-build-upload", HttpMethod.Post)
                    .SetBody(payload)
                    .SendAsync();

                if (!response.Success)
                {
                    var errorBody = response.ReadErrorBody();
                    SGLogger.LogError($"Failed to confirm build upload for {payload.Platform}: {string.Join(", ", errorBody.Messages)}");
                    throw new RequestFailedException(errorBody, response.ResponseCode);
                }

                SGLogger.Log($"Build upload confirmed for {payload.Platform} version {payload.Semver}");
                return true;
            }
            catch (RequestFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Unexpected error confirming build upload: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}