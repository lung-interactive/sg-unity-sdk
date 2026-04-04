using System;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Cloud storage providers for build file hosting.
    /// </summary>
    public enum FileHost
    {
        /// <summary>
        /// Amazon S3 object storage service.
        /// Default provider for build file storage and delivery.
        /// </summary>
        S3 = 1
    }
}