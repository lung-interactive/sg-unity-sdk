using System;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public abstract class VersioningStepElement : VisualElement
    {
        protected VersioningProcess _process;
        public event Action<bool> ReadyStatusChanged;

        public abstract VersioningStep Step { get; }

        public virtual void Activate(VersioningProcess process)
        {
            _process = process;
            style.display = DisplayStyle.Flex;
        }

        public virtual void Deactivate()
        {
            _process = null;
            style.display = DisplayStyle.None;
        }

        protected void SetReadyStatus(bool isReady) => ReadyStatusChanged?.Invoke(isReady);
    }
}