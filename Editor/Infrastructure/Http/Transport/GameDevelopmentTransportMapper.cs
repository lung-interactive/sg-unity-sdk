using SGUnitySDK.Editor.Core.Entities;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Infrastructure.Http.Transport
{
    /// <summary>
    /// Maps between core entities and HTTP transport payloads.
    /// Keeps transport concerns isolated in infrastructure.
    /// </summary>
    public static class GameDevelopmentTransportMapper
    {
        /// <summary>
        /// Converts core upload-start payload to transport payload.
        /// </summary>
        /// <param name="source">Core payload.</param>
        /// <returns>Transport payload.</returns>
        public static TransportStartBuildUploadRequest ToTransport(
            StartBuildUploadDTO source)
        {
            return new TransportStartBuildUploadRequest
            {
                Semver = source?.Semver,
                Platform = (int)(source?.Platform ?? 0),
                ExecutableName = source?.ExecutableName,
                Filename = source?.Filename,
                DownloadSize = source?.DownloadSize ?? 0,
                InstalledSize = source?.InstalledSize ?? 0,
                Host = (int)(source?.Host ?? 0),
                OverrideExisting = source?.OverrideExisting
            };
        }

        /// <summary>
        /// Converts transport upload-start response to core entity.
        /// </summary>
        /// <param name="source">Transport response.</param>
        /// <returns>Core response entity.</returns>
        public static StartBuildUploadResponseDTO ToDomain(
            TransportStartBuildUploadResponse source)
        {
            return new StartBuildUploadResponseDTO
            {
                UploadToken = source?.UploadToken,
                SignedUrl = source?.SignedUrl == null
                    ? null
                    : new PresignedURLDTO
                    {
                        Url = source.SignedUrl.Url,
                        ExpiresAt = source.SignedUrl.ExpiresAt,
                        Method = source.SignedUrl.Method,
                        FileKey = source.SignedUrl.FileKey,
                        Bucket = source.SignedUrl.Bucket,
                        ContentType = source.SignedUrl.ContentType,
                        SizeLimit = source.SignedUrl.SizeLimit,
                        Checksum = source.SignedUrl.Checksum
                    }
            };
        }

        /// <summary>
        /// Converts core upload-confirm payload to transport payload.
        /// </summary>
        /// <param name="source">Core payload.</param>
        /// <returns>Transport payload.</returns>
        public static TransportConfirmUploadRequest ToTransport(
            ConfirmUploadDTO source)
        {
            return new TransportConfirmUploadRequest
            {
                UploadToken = source?.UploadToken,
                Semver = source?.Semver,
                Platform = (int)(source?.Platform ?? 0)
            };
        }

        /// <summary>
        /// Converts core send-to-homologation payload to transport payload.
        /// </summary>
        /// <param name="source">Core payload.</param>
        /// <returns>Transport payload.</returns>
        public static TransportSendToHomologationRequest ToTransport(
            SendToHomologationDTO source)
        {
            return new TransportSendToHomologationRequest
            {
                Semver = source?.Semver
            };
        }

        /// <summary>
        /// Converts core acknowledge payload to transport payload.
        /// </summary>
        /// <param name="source">Core payload.</param>
        /// <returns>Transport payload.</returns>
        public static TransportAcknowledgeVersionRequest ToTransport(
            AcknowledgeVersionDTO source)
        {
            return new TransportAcknowledgeVersionRequest
            {
                Notes = source?.Notes
            };
        }

        /// <summary>
        /// Converts core versions-filter query to transport query.
        /// </summary>
        /// <param name="source">Core filter query.</param>
        /// <returns>Transport filter query.</returns>
        public static TransportVersionFilterQuery ToTransport(
            FilterVersionsDTO source)
        {
            if (source == null)
            {
                return null;
            }

            return new TransportVersionFilterQuery
            {
                State = source.State,
                IsCurrent = source.IsCurrent,
                IsPrerelease = source.IsPrerelease,
                CreatedAfter = source.CreatedAfter,
                CreatedBefore = source.CreatedBefore,
                SemverRaw = source.SemverRaw
            };
        }

        /// <summary>
        /// Converts transport version response into core version entity.
        /// </summary>
        /// <param name="source">Transport version response.</param>
        /// <returns>Core version entity.</returns>
        public static VersionDTO ToDomain(TransportVersionResponse source)
        {
            if (source == null)
            {
                return null;
            }

            var json = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<VersionDTO>(json);
        }

        /// <summary>
        /// Converts transport version response array into core version entities.
        /// </summary>
        /// <param name="source">Transport version response array.</param>
        /// <returns>Core version entities array.</returns>
        public static VersionDTO[] ToDomain(TransportVersionResponse[] source)
        {
            if (source == null)
            {
                return null;
            }

            var json = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<VersionDTO[]>(json);
        }

        /// <summary>
        /// Converts transport metadata response into core metadata entity.
        /// </summary>
        /// <param name="source">Transport metadata response.</param>
        /// <returns>Core metadata entity.</returns>
        public static VersionMetadata ToDomain(TransportVersionMetadataResponse source)
        {
            if (source == null)
            {
                return null;
            }

            var json = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<VersionMetadata>(json);
        }
    }
}
