using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public class VersioningSectionManager
    {
        #region Fields

        private int _currentStepIndex;
        private VersioningStepElement _currentStepElement;
        private List<VersioningStepElement> _stepsList;
        private readonly VisualElement _container;
        private VisualElement _containerSteps;

        private Button _buttonPrevious;
        private Button _buttonNext;

        #endregion

        #region Properties

        private SGEditorConfig Config => SGEditorConfig.instance;

        #endregion

        /// <summary>
        /// ctor. Wires UI and subscribes to ProcessEnded to jump to step 0.
        /// </summary>
        public VersioningSectionManager(VisualElement container)
        {
            _container = container;

            _buttonPrevious = _container.Q<Button>("button-previous");
            _buttonPrevious.clicked += OnPreviousButtonClicked;

            _buttonNext = _container.Q<Button>("button-next");
            _buttonNext.clicked += OnButtonNextClicked;

            _containerSteps = _container.Q<VisualElement>("container-steps");

            // Subscribe to process end: always go back to step 0.
            VersioningProcess.instance.ProcessEnded += OnProcessEnded;

            GenerateStepsList();
            EvaluateVersioningProcess();
        }

        /// <summary>
        /// Optional cleanup if you need to dispose this manager.
        /// </summary>
        public void Dispose()
        {
            try
            {
                VersioningProcess.instance.ProcessEnded -= OnProcessEnded;
            }
            catch { /* ignore if instance is unavailable on domain reload */ }
        }

        private void OnPreviousButtonClicked()
        {
            var previousStepIndex = _currentStepIndex - 1;
            if (previousStepIndex >= 0)
            {
                VersioningProcess.instance.RetrocedeStep();
                ActivateStep((VersioningStep)previousStepIndex);
                Config.Persist();
                GC.Collect();
            }
        }

        private void OnButtonNextClicked()
        {
            var nextStepIndex = _currentStepIndex + 1;
            if (nextStepIndex < _stepsList.Count)
            {
                VersioningProcess.instance.AdvanceStep();
                ActivateStep((VersioningStep)nextStepIndex);
                Config.Persist();
                GC.Collect();
            }
        }

        /// <summary>
        /// When the process ends, jump to step 0 and refresh UI.
        /// </summary>
        private void OnProcessEnded()
        {
            ActivateStep(VersioningStep.DefineTargetVersion);
            Config.Persist();
            GC.Collect();
        }

        private void EvaluateVersioningProcess()
        {
            var onGoingProcess = VersioningProcess.instance;
            ActivateStep(onGoingProcess.CurrentStep);
        }

        private void ActivateStep(VersioningStep versioningStep)
        {
            if (_currentStepElement != null)
            {
                _currentStepElement.ReadyStatusChanged -=
                    OnCurrentStepReadyStatusChanged;
                _containerSteps.Remove(_currentStepElement);
            }

            var index = (int)versioningStep;
            if (index < 0 || index >= _stepsList.Count)
            {
                Debug.LogError(
                    $"Index {index} is out of range for step list " +
                    $"{_stepsList.Count}"
                );
                return;
            }

            _currentStepIndex = index;
            _currentStepElement = _stepsList[_currentStepIndex];
            _currentStepElement.ReadyStatusChanged +=
                OnCurrentStepReadyStatusChanged;

            // Update button visibility and state
            UpdateNavigationButtons();

            _containerSteps.Add(_currentStepElement);
            _currentStepElement.Activate(VersioningProcess.instance);
        }

        private void UpdateNavigationButtons()
        {
            // Previous button - visible only if not first step
            _buttonPrevious.style.display = _currentStepIndex > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            // Next button - visible only if not last step
            _buttonNext.style.display =
                _currentStepIndex < _stepsList.Count - 1
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            // Next button starts disabled until step is ready
            _buttonNext.SetEnabled(false);
        }

        private void OnCurrentStepReadyStatusChanged(bool ready)
        {
            // Only enable Next button if:
            // 1. The step is ready
            // 2. We're not on the last step
            _buttonNext.SetEnabled(
                ready && _currentStepIndex < _stepsList.Count - 1
            );
            Config.Persist();
        }

        private void GenerateStepsList()
        {
            Type versioningStepElementType = typeof(VersioningStepElement);
            var stepElementTypes = versioningStepElementType.Assembly
                .GetTypes()
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.IsSubclassOf(versioningStepElementType)
                );

            _stepsList = new List<VersioningStepElement>();

            foreach (var type in stepElementTypes)
            {
                try
                {
                    var instance =
                        (VersioningStepElement)Activator.CreateInstance(type);

                    if (instance != null)
                    {
                        _stepsList.Add(instance);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"Failed to create instance of {type.Name}"
                    );
                    Debug.LogException(ex);
                }
            }

            _stepsList.Sort((x, y) => (int)x.Step - (int)y.Step);
        }
    }
}
