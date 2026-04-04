using SGUnitySDK.Events;

namespace SGUnitySDK
{
    /// <summary>
    /// Event payload emitted when a crowd action is triggered.
    /// </summary>
    [System.Serializable]
    public struct CrowdActionTriggered : IEvent
    {
        /// <summary>
        /// Triggered crowd action data.
        /// </summary>
        public CrowdAction Action;
    }
}