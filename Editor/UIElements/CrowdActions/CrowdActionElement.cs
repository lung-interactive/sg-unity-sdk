using System;
using System.Collections.Generic;
using HMSUnitySDK;
using HMSUnitySDK.LauncherInteroperations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    [UxmlElement]
    public partial class CrowdActionElement : VisualElement
    {
        private static readonly string TemplatePath = "CrowdActions/CrowdActionElement";

        public event Action<CrowdAction> OnSave;

        private readonly TemplateContainer _containerMain;
        private readonly Button _buttonSave;
        private readonly Button _buttonEmit;
        private readonly TextField _fieldIdentifier;
        private readonly TextField _fieldName;
        private readonly VisualElement _containerArguments;
        private readonly Button _buttonAddArgument;
        private readonly CrowdActionMetadataElement _metadataElement;

        public CrowdActionElement()
        {
            style.flexGrow = 1;

            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplatePath}").CloneTree();
            _containerMain.style.flexGrow = 1;

            _buttonSave = _containerMain.Q<Button>("button-save");
            _buttonEmit = _containerMain.Q<Button>("button-emit");
            _fieldIdentifier = _containerMain.Q<TextField>("field-identifier");
            _fieldName = _containerMain.Q<TextField>("field-name");
            _containerArguments = _containerMain.Q<VisualElement>("container-arguments");
            _buttonAddArgument = _containerMain.Q<Button>("button-add-argument");
            _metadataElement = _containerMain.Q<CrowdActionMetadataElement>("metadata-element");

            Add(_containerMain);
            InitializeButtons();

            // Atualiza o estado inicial do botão emit
            UpdateEmitButtonState();

            // Registra para atualizar o botão quando o estado do Play Mode mudar
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Atualiza o estado do botão quando o Play Mode mudar
            UpdateEmitButtonState();
        }

        private void UpdateEmitButtonState()
        {
            // Habilita o botão apenas se estiver no modo Play
            _buttonEmit.SetEnabled(EditorApplication.isPlaying);
        }

        private void InitializeButtons()
        {
            _buttonAddArgument.clicked += AddNewArgument;
            _buttonSave.clicked += SaveAction;
            _buttonEmit.clicked += OnEmitClicked;
        }

        private void OnEmitClicked()
        {
            try
            {
                EmitCrowdAction();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error emitting CrowdAction: {ex.Message}");
            }
        }

        private void EmitCrowdAction()
        {
            // Método dummy - você pode implementar a lógica real aqui
            Debug.Log($"-SG | Emitting crowd action: {_fieldIdentifier.value}");
            var interopsService = HMSLocator.Get<HMSLauncherInteropsService>();
            if (!interopsService.Interops.IsConnected)
            {
                Debug.LogError("Launcher not connected.");
                return;
            }

            if (interopsService.Interops.Socket is not HMSDummyLauncherInteropsSocket dummySocket)
            {
                Debug.LogError("-SG | Dummy socket not found.");
                return;
            }

            // Aqui você pode adicionar a lógica para emitir a ação
            // Por exemplo:
            // 1. Validar os dados
            // 2. Criar a estrutura de dados necessária
            // 3. Enviar para o sistema apropriado

            // Exemplo de implementação dummy:
            if (string.IsNullOrEmpty(_fieldIdentifier.value))
            {
                EditorUtility.DisplayDialog("Error", "Identifier cannot be empty when emitting", "OK");
                return;
            }

            var crowdAction = GetCrowdAction();
            dummySocket.TriggerEvent("crowd.action", crowdAction);

            // Mostra um feedback visual temporário
            _buttonEmit.text = "Emitting...";
            _buttonEmit.SetEnabled(false);
            this.schedule.Execute(() =>
            {
                _buttonEmit.text = "Emit";
                _buttonEmit.SetEnabled(true);
            }).ExecuteLater(1000);
        }

        private void AddNewArgument()
        {
            var argumentElement = new ProcessedArgumentElement();
            AddArgumentToScrollView(argumentElement);
        }

        private void AddArgumentToScrollView(ProcessedArgumentElement argumentElement)
        {
            var wrapper = new VisualElement();
            wrapper.style.flexDirection = FlexDirection.Row;
            wrapper.style.alignItems = Align.Center;
            wrapper.style.marginBottom = 5;

            var removeButton = new Button(() => _containerArguments.Remove(wrapper))
            {
                text = "×",
                style =
                {
                    width = 25,
                    height = 20,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginLeft = 10
                }
            };

            wrapper.Add(argumentElement);
            wrapper.Add(removeButton);
            _containerArguments.Add(wrapper);
        }

        public CrowdAction GetCrowdAction()
        {
            return new CrowdAction
            {
                identifier = _fieldIdentifier.value,
                name = _fieldName.value,
                processed_arguments = GetArgumentsArray(),
                metadata = _metadataElement.GetMetadata()
            };
        }

        private ProcessedArgument[] GetArgumentsArray()
        {
            var arguments = new List<ProcessedArgument>();
            foreach (var child in _containerArguments.Children())
            {
                if (child.childCount > 0 && child[0] is ProcessedArgumentElement argumentElement)
                {
                    arguments.Add(argumentElement.GetProcessedArgument());
                }
            }
            return arguments.ToArray();
        }

        public void LoadCrowdAction(CrowdAction crowdAction)
        {
            _fieldIdentifier.value = crowdAction.identifier;
            _fieldName.value = crowdAction.name;

            _containerArguments.Clear();
            if (crowdAction.processed_arguments != null)
            {
                foreach (var arg in crowdAction.processed_arguments)
                {
                    var argumentElement = new ProcessedArgumentElement();
                    argumentElement.LoadProcessedArgument(arg);
                    AddArgumentToScrollView(argumentElement);
                }
            }

            _metadataElement?.LoadMetadata(crowdAction.metadata);
        }

        private void SaveAction()
        {
            try
            {
                var crowdAction = GetCrowdAction();

                if (string.IsNullOrWhiteSpace(crowdAction.identifier))
                {
                    EditorUtility.DisplayDialog("Error", "Identifier cannot be empty", "OK");
                    return;
                }

                OnSave?.Invoke(crowdAction);

                _buttonSave.text = "Saved!";
                _buttonSave.RemoveFromClassList("unity-button");
                _buttonSave.AddToClassList("saved-button");

                this.schedule.Execute(() =>
                {
                    _buttonSave.text = "Save";
                    _buttonSave.RemoveFromClassList("saved-button");
                    _buttonSave.AddToClassList("unity-button");
                }).ExecuteLater(2000);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error saving CrowdAction: {ex.Message}");
            }
        }
    }
}