using System;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Status of testing phase for a version.
    /// </summary>
    public enum TestingStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Passed = 2,
        Failed = 3
    }
}