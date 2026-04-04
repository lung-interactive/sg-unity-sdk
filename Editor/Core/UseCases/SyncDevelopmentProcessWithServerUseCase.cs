
using System;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Repositories;
using SGUnitySDK.Editor.Core.Singletons;
using UnityEngine;

namespace SGUnitySDK.Editor.Core.UseCases
{
    /// <summary>
    /// Use case responsible for synchronizing the local DevelopmentProcess
    /// with the server state for the current version. Ensures consistency
    /// between local and remote version status, cleaning or updating the
    /// local record as needed.
    /// </summary>
    public class SyncDevelopmentProcessWithServerUseCase
    {
        private readonly IRemoteVersionService _remoteVersionService;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="SyncDevelopmentProcessWithServerUseCase"/> class.
        /// </summary>
        /// <param name="remoteVersionService">Remote version service abstraction.</param>
        public SyncDevelopmentProcessWithServerUseCase(
            IRemoteVersionService remoteVersionService)
        {
            _remoteVersionService = remoteVersionService ??
                throw new ArgumentNullException(nameof(remoteVersionService));
        }

        /// <summary>
        /// Synchronizes the local DevelopmentProcess singleton with the server.
        /// </summary>
        /// <returns>
        /// True if the local state was changed (reset or updated),
        /// false if no action was needed.
        /// </returns>
        public async Awaitable<bool> ExecuteAsync()
        {
            // Local singleton state
            var dev = DevelopmentProcess.instance;
            var localVersion = dev.CurrentVersion;

            // If there is a local version registered, follow registered-version flow
            if (localVersion != null &&
                localVersion.Semver != null &&
                !string.IsNullOrEmpty(localVersion.Semver.Raw))
            {
                try
                {
                    var serverVersions = await _remoteVersionService.FilterVersionsAsync(
                        new FilterVersionsDTO { SemverRaw = localVersion.Semver.Raw }
                    );

                    var serverVersion = (serverVersions != null && serverVersions.Length > 0)
                        ? serverVersions[0]
                        : null;

                    // If server has no such version, go to discovery flow
                    if (serverVersion == null)
                    {
                        return await DiscoverAndAdoptVersion(dev);
                    }

                    // If there is a newer version under development on the server,
                    // adopt it even when the server contains the current local semver.
                    try
                    {
                        var underDevCandidate = await _remoteVersionService.GetVersionUnderDevelopmentAsync();
                        if (underDevCandidate != null &&
                            localVersion != null &&
                            !string.IsNullOrEmpty(localVersion.Semver?.Raw) &&
                            CompareSemver(underDevCandidate.Semver.Raw, localVersion.Semver.Raw) > 0)
                        {
                            var metadata = await SafeGetMetadata(underDevCandidate.Id);
                            dev.CurrentVersion = underDevCandidate;
                            dev.CurrentVersionMetadata = metadata;
                            // Decide step based on acknowledgement
                            if (metadata != null && metadata.Acknowledgment != null && !metadata.Acknowledgment.Acknowledged)
                            {
                                dev.SetStep(DevelopmentStep.AcceptVersion);
                            }
                            else
                            {
                                dev.SetStep(DevelopmentStep.Development);
                            }
                            dev.Persist();
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                        // ignore failures to query under-development candidate and continue
                    }

                    // If the version state in server and local version record are equal,
                    // ensure the UI/process step is consistent with that state.
                    // Example: version.state == UnderDevelopment should map to
                    // DevelopmentStep.Development even if the local process step
                    // is incorrectly Homologation.
                    if (serverVersion.State == localVersion.State)
                    {
                        var serverStateMatch = (GameVersionState)serverVersion.State;
                        DevelopmentStep desired = dev.CurrentStep;
                        if (serverStateMatch == GameVersionState.UnderDevelopment)
                            desired = DevelopmentStep.Development;
                        else if (serverStateMatch == GameVersionState.Homologation)
                            desired = DevelopmentStep.Homologation;

                        if (dev.CurrentStep != desired)
                        {
                            // Refresh metadata best-effort
                            var metadataMatch = await SafeGetMetadata(serverVersion.Id);
                            dev.CurrentVersion = serverVersion;
                            dev.CurrentVersionMetadata = metadataMatch;
                            dev.SetStep(desired);
                            dev.Persist();
                            return true;
                        }

                        return false;
                    }

                    var serverState = (GameVersionState)serverVersion.State;

                    // If server state is UnderDevelopment or Homologation, update local process accordingly
                    if (serverState == GameVersionState.UnderDevelopment || serverState == GameVersionState.Homologation)
                    {
                        // Refresh metadata (best-effort) and adopt server version.
                        VersionMetadata metadata = await SafeGetMetadata(serverVersion.Id);

                        dev.CurrentVersion = serverVersion;
                        dev.CurrentVersionMetadata = metadata;

                        if (serverState == GameVersionState.Homologation)
                        {
                            dev.SetStep(DevelopmentStep.Homologation);
                        }
                        else // UnderDevelopment -> decide by metadata acknowledgement
                        {
                            if (metadata != null && metadata.Acknowledgment != null && !metadata.Acknowledgment.Acknowledged)
                            {
                                dev.SetStep(DevelopmentStep.AcceptVersion);
                            }
                            else
                            {
                                dev.SetStep(DevelopmentStep.Development);
                            }
                        }

                        dev.Persist();
                        return true;
                    }

                    // Handle terminal server states explicitly: Ready -> Approved, Canceled -> Canceled
                    if (serverState == GameVersionState.Ready)
                    {
                        // Mark local process as approved (development ended)
                        dev.CurrentVersion = serverVersion;
                        dev.CurrentVersionMetadata = await SafeGetMetadata(serverVersion.Id);
                        dev.SetStep(DevelopmentStep.Approved);
                        dev.Persist();

                        // After marking approved, attempt to discover a new version to start developing
                        await DiscoverAndAdoptVersion(dev);
                        return true;
                    }

                    if (serverState == GameVersionState.Canceled)
                    {
                        dev.CurrentVersion = serverVersion;
                        dev.CurrentVersionMetadata = await SafeGetMetadata(serverVersion.Id);
                        dev.SetStep(DevelopmentStep.Canceled);
                        dev.Persist();

                        // After cancellation, attempt to discover next version
                        await DiscoverAndAdoptVersion(dev);
                        return true;
                    }

                    // Fallback: for other terminal states run discovery
                    if (serverState == GameVersionState.Released ||
                        serverState == GameVersionState.Rejected ||
                        serverState == GameVersionState.Deprecated ||
                        serverState == GameVersionState.AwaitingDevelopmentApproval)
                    {
                        return await DiscoverAndAdoptVersion(dev);
                    }
                }
                catch (Exception)
                {
                    // On network or unexpected errors, avoid throwing and preserve current UI state
                    return false;
                }

                return false;
            }

            // No local version registered: go to discovery flow
            return await DiscoverAndAdoptVersion(DevelopmentProcess.instance);
        }

        /// <summary>
        /// Attempts to discover a version on the server that is UnderDevelopment or in Homologation
        /// and adopts it into the local DevelopmentProcess.
        /// </summary>
        /// <param name="dev">Local development process singleton to update.</param>
        /// <returns>True if a version was adopted/changed, false otherwise.</returns>
        private async Awaitable<bool> DiscoverAndAdoptVersion(DevelopmentProcess dev)
        {
            try
            {
                // Prefer an explicit UnderDevelopment version
                var underDev = await _remoteVersionService.GetVersionUnderDevelopmentAsync();
                if (underDev != null)
                {
                    var metadata = await SafeGetMetadata(underDev.Id);
                    dev.CurrentVersion = underDev;
                    dev.CurrentVersionMetadata = metadata;

                    // Server reports UnderDevelopment -> choose Acceptance or Development
                    if (metadata != null && metadata.Acknowledgment != null && !metadata.Acknowledgment.Acknowledged)
                    {
                        dev.SetStep(DevelopmentStep.AcceptVersion);
                    }
                    else
                    {
                        dev.SetStep(DevelopmentStep.Development);
                    }

                    dev.Persist();
                    return true;
                }

                // Otherwise check for a version in Homologation
                var homol = await _remoteVersionService.FilterVersionsAsync(
                    new FilterVersionsDTO { State = (int)GameVersionState.Homologation }
                );

                if (homol != null && homol.Length > 0)
                {
                    var homVersion = homol[0];
                    var metadata = await SafeGetMetadata(homVersion.Id);
                    dev.CurrentVersion = homVersion;
                    dev.CurrentVersionMetadata = metadata;
                    dev.SetStep(DevelopmentStep.Homologation);
                    dev.Persist();
                    return true;
                }
            }
            catch (Exception)
            {
                // Ignore network issues and return no-change
            }

            return false;
        }

        /// <summary>
        /// Safely fetches version metadata, returning null on failure.
        /// </summary>
        private async Awaitable<VersionMetadata> SafeGetMetadata(string versionId)
        {
            try
            {
                return await _remoteVersionService.GetVersionMetadataAsync(versionId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Compares two semantic version strings (major.minor.patch).
        /// </summary>
        /// <param name="a">First semver string.</param>
        /// <param name="b">Second semver string.</param>
        /// <returns>
        /// 1 if a &gt; b, -1 if a &lt; b, 0 if equal.
        /// </returns>
        private int CompareSemver(string a, string b)
        {
            var aParts = a.Split('.');
            var bParts = b.Split('.');
            for (int i = 0; i < 3; i++)
            {
                int ai = int.Parse(aParts[i]);
                int bi = int.Parse(bParts[i]);
                if (ai > bi) return 1;
                if (ai < bi) return -1;
            }
            return 0;
        }
    }
}
