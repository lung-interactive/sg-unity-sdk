using System;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Defines the semantic version increment type when creating new versions.
    /// Controls how the version number is automatically incremented.
    /// </summary>
    public enum VersionUpdateType
    {
        /// <summary>
        /// Increment patch version (e.g., 1.2.3 -> 1.2.4).
        /// Used for bug fixes and minor improvements.
        /// </summary>
        Patch = 1,

        /// <summary>
        /// Increment minor version (e.g., 1.2.3 -> 1.3.0).
        /// Used for new features without breaking changes.
        /// </summary>
        Minor = 2,

        /// <summary>
        /// Increment major version (e.g., 1.2.3 -> 2.0.0).
        /// Used for significant updates with breaking changes.
        /// </summary>
        Major = 3,

        /// <summary>
        /// Use a specific version number provided explicitly.
        /// Allows manual version specification instead of auto-increment.
        /// </summary>
        Specific = 4
    }
}