using System;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Status of development work for a version.
    /// </summary>
    public enum DevelopmentStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        OnHold = 3,
        Blocked = 4
    }
}