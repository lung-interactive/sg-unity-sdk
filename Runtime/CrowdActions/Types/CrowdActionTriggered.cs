using SGUnitySDK.Events;

namespace SGUnitySDK
{
    [System.Serializable]
    public struct CrowdActionTriggered : IEvent
    {
        public CrowdAction Action;
    }
}