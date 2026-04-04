using System.Threading.Tasks;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.Utils;
using SGUnitySDK.Utils;
using UnityEngine;

namespace SGUnitySDK.Editor.Core.UseCases
{
    /// <summary>
    /// Use case for retrieving the first version in the 'UnderDevelopment' state from the remote server.
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
        /// Retrieves the first version in the 'UnderDevelopment' state from the remote server.
        /// </summary>
        /// <returns>The version entity if found; otherwise, null.</returns>
        public async Task<VersionDTO> ExecuteAsync()
        {
            // Fetches the first version in 'UnderDevelopment' state from the remote service.
            var version = await _remoteVersionService.GetFirstUnderDevelopmentVersionAsync();
            return version;
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
