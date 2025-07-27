using UnityEngine;
using UnityEngine.Events;

namespace SGUnitySDK.Events
{
    public abstract class EventChannel<TData> : ScriptableObject
    {
        public UnityAction<TData> OnEventRaised;

        public void RaiseEvent(TData data)
        {
            OnEventRaised?.Invoke(data);
        }
    }

    public abstract class EventChannel : ScriptableObject
    {
        public UnityAction OnEventRaised;

        public void RaiseEvent()
        {
            OnEventRaised?.Invoke();
        }
    }
}