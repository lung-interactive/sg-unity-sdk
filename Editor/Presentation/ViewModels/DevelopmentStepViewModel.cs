using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.UseCases;

namespace SGUnitySDK.Editor.Presentation.ViewModels
{
    /// <summary>
    /// ViewModel for the Development step presentation flow.
    /// Aggregates use-case execution and exposes UI-friendly operations.
    /// </summary>
    public class DevelopmentStepViewModel
    {
        private readonly FetchUnderDevelopmentVersionUseCase _fetchUnderDevelopmentVersionUseCase;
        private readonly AcceptVersionUseCase _acceptVersionUseCase;
        private readonly GenerateBuildsUseCase _generateBuildsUseCase;
        private readonly UploadBuildUseCase _uploadBuildUseCase;
        private readonly UploadMultipleBuildsUseCase _uploadMultipleBuildsUseCase;
        private readonly SendToHomologationUseCase _sendToHomologationUseCase;
        private readonly DevelopmentProcessStateViewModel _processState;

        /// <summary>
        /// Initializes a new instance of the <see cref="DevelopmentStepViewModel"/> class.
        /// </summary>
        /// <param name="fetchUnderDevelopmentVersionUseCase">Use case for fetching under-development version.</param>
        /// <param name="acceptVersionUseCase">Use case for accepting a version.</param>
        /// <param name="generateBuildsUseCase">Use case for build generation.</param>
        /// <param name="uploadBuildUseCase">Use case for single build upload.</param>
        /// <param name="uploadMultipleBuildsUseCase">Use case for batch build upload.</param>
        /// <param name="sendToHomologationUseCase">Use case for send-to-homologation flow.</param>
        /// <param name="processState">Process state view model abstraction.</param>
        public DevelopmentStepViewModel(
            FetchUnderDevelopmentVersionUseCase fetchUnderDevelopmentVersionUseCase,
            AcceptVersionUseCase acceptVersionUseCase,
            GenerateBuildsUseCase generateBuildsUseCase,
            UploadBuildUseCase uploadBuildUseCase,
            UploadMultipleBuildsUseCase uploadMultipleBuildsUseCase,
            SendToHomologationUseCase sendToHomologationUseCase,
            DevelopmentProcessStateViewModel processState)
        {
            _fetchUnderDevelopmentVersionUseCase = fetchUnderDevelopmentVersionUseCase ??
                throw new ArgumentNullException(nameof(fetchUnderDevelopmentVersionUseCase));
            _acceptVersionUseCase = acceptVersionUseCase ??
                throw new ArgumentNullException(nameof(acceptVersionUseCase));
            _generateBuildsUseCase = generateBuildsUseCase ??
                throw new ArgumentNullException(nameof(generateBuildsUseCase));
            _uploadBuildUseCase = uploadBuildUseCase ??
                throw new ArgumentNullException(nameof(uploadBuildUseCase));
            _uploadMultipleBuildsUseCase = uploadMultipleBuildsUseCase ??
                throw new ArgumentNullException(nameof(uploadMultipleBuildsUseCase));
            _sendToHomologationUseCase = sendToHomologationUseCase ??
                throw new ArgumentNullException(nameof(sendToHomologationUseCase));
            _processState = processState ??
                throw new ArgumentNullException(nameof(processState));
        }

        /// <summary>
        /// Reads the current locally persisted development version string.
        /// </summary>
        /// <returns>Semantic version string when available; otherwise "-".</returns>
        public string GetLocalDevelopmentVersionString()
        {
            return _processState.CurrentVersion?.Semver?.Raw ?? "-";
        }

        /// <summary>
        /// Fetches the current under-development version from remote services.
        /// </summary>
        /// <returns>Remote under-development version when available; otherwise null.</returns>
        public Task<VersionDTO> FetchUnderDevelopmentVersionAsync()
        {
            return _fetchUnderDevelopmentVersionUseCase.ExecuteAsync();
        }

        /// <summary>
        /// Accepts the current remote under-development version and persists it locally.
        /// </summary>
        /// <param name="notes">Optional acknowledgment notes.</param>
        /// <returns>True when acceptance succeeds; otherwise false.</returns>
        public async Task<bool> AcceptDevelopmentVersionAsync(string notes)
        {
            var version = await _fetchUnderDevelopmentVersionUseCase.ExecuteAsync();
            if (version == null)
            {
                return false;
            }

            return await _acceptVersionUseCase.ExecuteAsync(version, notes);
        }

        /// <summary>
        /// Generates local build entries and stores them in DevelopmentProcess.
        /// </summary>
        /// <returns>Generated build entries.</returns>
        public List<SGVersionBuildEntry> GenerateBuilds()
        {
            var entries = _generateBuildsUseCase.Execute();
            _processState.SetVersionBuilds(entries);
            return entries;
        }

        /// <summary>
        /// Uploads a single build entry and persists the updated entry in DevelopmentProcess.
        /// </summary>
        /// <param name="entry">Build entry to upload.</param>
        /// <param name="ct">Cancellation token for upload operation.</param>
        /// <returns>The updated build entry after upload flow.</returns>
        public async Task<SGVersionBuildEntry> UploadBuildAsync(
            SGVersionBuildEntry entry,
            CancellationToken ct = default)
        {
            var updated = await _uploadBuildUseCase.ExecuteAsync(entry, ct);
            var list = _processState.GetVersionBuildsOrEmpty();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].build.path == updated.build.path)
                {
                    _processState.ReplaceVersionBuild(i, updated);
                    break;
                }
            }

            return updated;
        }

        /// <summary>
        /// Uploads all unuploaded entries and persists the resulting entries.
        /// </summary>
        /// <param name="entries">Candidate entries for upload.</param>
        /// <param name="ct">Cancellation token for batch operation.</param>
        /// <returns>Updated entries from batch upload operation.</returns>
        public async Task<List<SGVersionBuildEntry>> UploadAllBuildsAsync(
            IEnumerable<SGVersionBuildEntry> entries,
            CancellationToken ct = default)
        {
            var results = await _uploadMultipleBuildsUseCase.ExecuteAsync(entries, ct);
            var currentBuilds = _processState.GetVersionBuildsOrEmpty();
            foreach (var res in results)
            {
                for (int i = 0; i < currentBuilds.Count; i++)
                {
                    if (currentBuilds[i].build.path == res.build.path)
                    {
                        _processState.ReplaceVersionBuild(i, res);
                        break;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Sends the current development version to homologation.
        /// </summary>
        /// <returns>True when operation succeeds; otherwise false.</returns>
        public async Task<bool> SendCurrentVersionToHomologationAsync()
        {
            var version = _processState.CurrentVersion;
            if (version == null)
            {
                return false;
            }

            return await _sendToHomologationUseCase.ExecuteAsync(version);
        }
    }
}
