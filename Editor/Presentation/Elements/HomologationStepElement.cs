using System;
using UnityEngine;
using UnityEngine.UIElements;
using SGUnitySDK.Editor.Core.Singletons;
using SGUnitySDK.Editor.Infrastructure;
using SGUnitySDK.Editor.Presentation.ViewModels;

namespace SGUnitySDK.Editor.Presentation.Elements
{
    /// <summary>
    /// Homologation step element.
    /// Presents a static informational message while the process step is Homologation.
    /// This element contains no interactive controls; approval is performed by admins.
    /// </summary>
    public class HomologationStepElement : DevelopmentStepElement
    {
        private readonly DevelopmentProcessStateViewModel _processState;

        /// <summary>
        /// The development step this element represents.
        /// </summary>
        public override DevelopmentStep Step => DevelopmentStep.Homologation;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomologationStepElement"/> class.
        /// Loads the homologation UXML and wires visibility updates to the
        /// DevelopmentProcess step changes.
        /// </summary>
        public HomologationStepElement()
        {
            _processState = EditorServiceProvider.Instance
                .GetService<DevelopmentProcessStateViewModel>();

            var visualTree = Resources.Load<VisualTreeAsset>("UXML/DevProcess_HomologationStep");
            visualTree.CloneTree(this);

            // React to step changes when attached to panel, and detach when removed.
            this.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                _processState.StepChanged += UpdateVisibilityForStep;
                UpdateVisibilityForStep(_processState.CurrentStep);
            });

            this.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                try
                {
                    _processState.StepChanged -= UpdateVisibilityForStep;
                }
                catch (Exception)
                {
                    // ignore – safe cleanup
                }
            });
        }

        /// <summary>
        /// Shows the element only when the current process step is Homologation.
        /// </summary>
        /// <param name="step">Current development step.</param>
        private void UpdateVisibilityForStep(DevelopmentStep step)
        {
            if (step == DevelopmentStep.Homologation)
            {
                Activate(_processState.Process);
            }
            else
            {
                Deactivate();
            }
        }
    }
}
