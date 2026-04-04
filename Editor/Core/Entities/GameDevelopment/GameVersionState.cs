using System;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Represents the lifecycle state of a game version.
    /// Defines all possible states a version can transition through from
    /// initial development approval to final release or cancellation.
    /// </summary>
    public enum GameVersionState
    {
        /// <summary>
        /// Version is awaiting development approval to begin work.
        /// Initial state after version creation.
        /// </summary>
        AwaitingDevelopmentApproval = 1,

        /// <summary>
        /// Version is being prepared and not yet submitted for approval.
        /// Development team is actively working on this version.
        /// </summary>
        UnderDevelopment = 2,

        /// <summary>
        /// Version is being tested for release.
        /// Quality assurance phase before final release.
        /// </summary>
        Homologation = 3,

        /// <summary>
        /// Version is ready for release but not yet released.
        /// All checks passed, awaiting release trigger.
        /// </summary>
        Ready = 4,

        /// <summary>
        /// Version has been officially released.
        /// Available to end users.
        /// </summary>
        Released = 5,

        /// <summary>
        /// Version has been canceled and will not proceed to release.
        /// Terminal state - no further transitions possible.
        /// </summary>
        Canceled = 6,

        /// <summary>
        /// Version has been rejected and will not be released.
        /// Failed quality checks or requirements.
        /// Terminal state - no further transitions possible.
        /// </summary>
        Rejected = 7,

        /// <summary>
        /// Version is deprecated and should not be used.
        /// Superceded by newer versions.
        /// Terminal state - no further transitions possible.
        /// </summary>
        Deprecated = 8
    }
}