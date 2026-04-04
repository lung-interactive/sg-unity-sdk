using System;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.Entities;

namespace SGUnitySDK.Editor.Core.UseCases
{
    /// <summary>
    /// Use case for updating the metadata of a game version.
    /// </summary>
    public class UpdateVersionMetadataUseCase
    {
        private readonly IMetadataRepository _metadataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateVersionMetadataUseCase"/> class.
        /// </summary>
        /// <param name="metadataRepository">Metadata repository abstraction.</param>
        public UpdateVersionMetadataUseCase(IMetadataRepository metadataRepository)
        {
            _metadataRepository = metadataRepository ??
                throw new ArgumentNullException(nameof(metadataRepository));
        }

        /// <summary>
        /// Executes the logic to update version metadata.
        /// </summary>
        /// <param name="version">The version entity to update.</param>
        /// <param name="metadata">The new metadata to apply.</param>
        public void Execute(VersionDTO version, VersionMetadata metadata)
        {
            if (version == null || string.IsNullOrEmpty(version.Id))
                throw new ArgumentException("Version or version ID is null.");

            _metadataRepository.UpdateMetadata(version.Id, metadata);
        }
    }
}
