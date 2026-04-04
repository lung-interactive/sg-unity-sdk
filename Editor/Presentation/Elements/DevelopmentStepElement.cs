using System;
using SGUnitySDK.Editor.Core.Singletons;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor.Presentation.Elements
{
    public abstract class DevelopmentStepElement : VisualElement
    {
        protected DevelopmentProcess _process;
        public event Action<bool> ReadyStatusChanged;

        public abstract DevelopmentStep Step { get; }

        public virtual void Activate(DevelopmentProcess process)
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
