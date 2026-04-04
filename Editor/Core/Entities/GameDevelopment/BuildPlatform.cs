using System;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Supported target platforms for game builds.
    /// Used when uploading and managing build artifacts.
    /// </summary>
    public enum BuildPlatform
    {
        /// <summary>Windows desktop platform.</summary>
        Windows = 1,

        /// <summary>macOS desktop platform.</summary>
        MacOS = 2,

        /// <summary>Linux desktop platform.</summary>
        Linux = 3,

        /// <summary>Android mobile platform.</summary>
        Android = 4,

        /// <summary>iOS mobile platform.</summary>
        IOS = 5,

        /// <summary>Web browser platform.</summary>
        Web = 6
    }
}