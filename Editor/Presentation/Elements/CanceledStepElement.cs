using UnityEngine;
using UnityEngine.UIElements;
using SGUnitySDK.Editor.Core.Singletons;
using SGUnitySDK.Editor.Infrastructure;
using SGUnitySDK.Editor.Presentation.ViewModels;

namespace SGUnitySDK.Editor.Presentation.Elements
{
    /// <summary>
    /// Canceled step element - shows informational UI when the process is Canceled.
    /// </summary>
    public class CanceledStepElement : DevelopmentStepElement
    {
        private readonly DevelopmentProcessStateViewModel _processState;

        public override DevelopmentStep Step => DevelopmentStep.Canceled;

        public CanceledStepElement()
        {
            _processState = EditorServiceProvider.Instance
                .GetService<DevelopmentProcessStateViewModel>();

            var visualTree = Resources.Load<VisualTreeAsset>("UXML/DevProcess_CanceledStep");
            visualTree.CloneTree(this);

            this.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                _processState.StepChanged += UpdateVisibilityForStep;
                UpdateVisibilityForStep(_processState.CurrentStep);
            });

            this.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                try { _processState.StepChanged -= UpdateVisibilityForStep; } catch { }
            });
        }

        private void UpdateVisibilityForStep(DevelopmentStep step)
        {
            if (step == DevelopmentStep.Canceled)
                Activate(_processState.Process);
            else
                Deactivate();
        }
    }
}
