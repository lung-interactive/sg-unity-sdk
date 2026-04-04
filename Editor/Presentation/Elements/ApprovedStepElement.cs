using UnityEngine;
using UnityEngine.UIElements;
using SGUnitySDK.Editor.Core.Singletons;
using SGUnitySDK.Editor.Infrastructure;
using SGUnitySDK.Editor.Presentation.ViewModels;

namespace SGUnitySDK.Editor.Presentation.Elements
{
    /// <summary>
    /// Approved step element - shows informational UI when the process is Approved.
    /// </summary>
    public class ApprovedStepElement : DevelopmentStepElement
    {
        private readonly DevelopmentProcessStateViewModel _processState;

        public override DevelopmentStep Step => DevelopmentStep.Approved;

        public ApprovedStepElement()
        {
            _processState = EditorServiceProvider.Instance
                .GetService<DevelopmentProcessStateViewModel>();

            var visualTree = Resources.Load<VisualTreeAsset>("UXML/DevProcess_ApprovedStep");
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
            if (step == DevelopmentStep.Approved)
                Activate(_processState.Process);
            else
                Deactivate();
        }
    }
}
