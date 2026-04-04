using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Singletons;
using SGUnitySDK.Editor.Versioning;

namespace SGUnitySDK.Editor.Core.UseCases
{
    /// <summary>
    /// Use case responsible for uploading multiple builds in parallel.
    /// It delegates individual uploads to `UploadBuildUseCase.ExecuteBackgroundAsync`
    /// and returns the collection of updated entries.
    /// </summary>
    public sealed class UploadMultipleBuildsUseCase
    {
        private readonly UploadBuildUseCase _single;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadMultipleBuildsUseCase"/> class.
        /// </summary>
        /// <param name="singleUploadUseCase">Single-build upload use case.</param>
        public UploadMultipleBuildsUseCase(UploadBuildUseCase singleUploadUseCase)
        {
            _single = singleUploadUseCase ??
                throw new ArgumentNullException(nameof(singleUploadUseCase));
        }

        /// <summary>
        /// Uploads the provided build entries in parallel. The semver used is
        /// taken from `DevelopmentProcess.instance.CurrentVersion` and the
        /// DevelopmentProcess must be in the Development step.
        /// </summary>
        public async Task<List<SGVersionBuildEntry>> ExecuteAsync(IEnumerable<SGVersionBuildEntry> entries, CancellationToken ct = default)
        {
            if (DevelopmentProcess.instance.CurrentStep != DevelopmentStep.Development)
                throw new InvalidOperationException("Current development step is not 'Development'. Aborting parallel upload.");

            var current = DevelopmentProcess.instance.CurrentVersion;
            if (current == null || current.Semver == null)
                throw new InvalidOperationException("No current development version available in DevelopmentProcess.");

            var semver = current.Semver.Raw;

            var list = entries.Where(e => e.CanUpload()).ToList();
            var tasks = list.Select(e => _single.ExecuteBackgroundAsync(e, semver, ct)).ToArray();

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            return results.ToList();
        }
    }
}
