using System.Threading.Tasks;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.Utils;
using SGUnitySDK.Utils;
using UnityEngine;

namespace SGUnitySDK.Editor.Core.UseCases
{
    /// <summary>
    /// Use case for retrieving the next actionable version for the
    /// development pipeline. Prioritizes a version in preparation and
    /// falls back to under-development when needed.
    /// </summary>
    public class FetchUnderDevelopmentVersionUseCase
    {
        private readonly IRemoteVersionService _remoteVersionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FetchUnderDevelopmentVersionUseCase"/> class.
        /// </summary>
        /// <param name="remoteVersionService">Remote version service abstraction.</param>
        public FetchUnderDevelopmentVersionUseCase(
            IRemoteVersionService remoteVersionService)
        {
            _remoteVersionService = remoteVersionService ??
                throw new System.ArgumentNullException(nameof(remoteVersionService));
        }

        /// <summary>
        /// Retrieves the first actionable version from the remote server.
        /// </summary>
        /// <returns>The version entity if found; otherwise, null.</returns>
        public async Task<VersionDTO> ExecuteAsync()
        {
            var inPreparation = await _remoteVersionService.GetVersionInPreparationAsync();
            if (inPreparation != null)
            {
                return inPreparation;
            }

            var underDevelopment = await _remoteVersionService.GetFirstUnderDevelopmentVersionAsync();
            return underDevelopment;
        }

        /// <summary>
        /// Awaitable wrapper for callers that use the project's Awaitable abstraction.
        /// </summary>
        public Awaitable<VersionDTO> ExecuteAwaitable()
        {
            return TaskAwaitableAdapter.FromTask(ExecuteAsync());
        }
    }
}
