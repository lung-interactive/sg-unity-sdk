using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.Singletons;

namespace SGUnitySDK.Editor.Core.UseCases
{
    /// <summary>
    /// Use case responsible for generating the project builds for the
    /// currently active development version. This use case does not perform
    /// any upload or network operations — generated builds must be uploaded
    /// using the dedicated upload use cases.
    /// </summary>
    public sealed class GenerateBuildsUseCase
    {
        private readonly IBuildGenerationService _buildGenerationService;

        /// <summary>
        /// Creates a new instance of the use case.
        /// </summary>
        /// <param name="buildGenerationService">
        /// Build generation abstraction implemented by infrastructure.
        /// </param>
        public GenerateBuildsUseCase(IBuildGenerationService buildGenerationService)
        {
            _buildGenerationService = buildGenerationService ??
                throw new ArgumentNullException(nameof(buildGenerationService));
        }

        /// <summary>
        /// Generates builds using the project's configured build setups and
        /// returns a list of `SGVersionBuildEntry` ready for upload.
        /// This method validates that the DevelopmentProcess is in the
        /// Development step and will throw if no current development version
        /// is available.
        /// </summary>
        public List<SGVersionBuildEntry> Execute()
        {
            if (DevelopmentProcess.instance.CurrentStep != DevelopmentStep.Development)
                throw new InvalidOperationException("Current development step is not 'Development'. Cannot generate builds.");

            var current = DevelopmentProcess.instance.CurrentVersion;
            if (current == null || current.Semver == null)
                throw new InvalidOperationException("No current development version available in DevelopmentProcess.");

            var target = current.Semver.Raw;

            var setups = SGEditorConfig.instance.BuildSetups;
            var commonBuildPath = SGEditorConfig.instance.BuildsDirectory;

            var results = _buildGenerationService.GenerateBuilds(
                setups,
                commonBuildPath,
                target);

            var entries = new List<SGVersionBuildEntry>();
            foreach (var r in results)
            {
                entries.Add(new SGVersionBuildEntry { build = r });
            }

            return entries;
        }

        /// <summary>
        /// Async wrapper around <see cref="Execute"/> kept for backward
        /// compatibility with callers that expect an asynchronous API.
        ///
        /// This method executes the synchronous generation on the caller
        /// context and wraps the result in a completed Task. Generation
        /// itself may require running on the Unity main thread; callers
        /// should ensure they invoke this from an appropriate context.
        /// </summary>
        public Task<List<SGVersionBuildEntry>> ExecuteAsync()
        {
            return Task.FromResult(Execute());
        }
    }
}
