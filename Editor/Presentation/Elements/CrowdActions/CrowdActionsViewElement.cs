using System;
using System.Collections.Generic;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Infrastructure;
using SGUnitySDK.Editor.Presentation.ViewModels;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor.Presentation.Elements
{
    [UxmlElement]
    public partial class CrowdActionsViewElement : VisualElement
    {
        private const string TemplatePath = "CrowdActions/CrowdActionsViewElement";

        // UI References
        private readonly ScrollView _scrollViewActions;
        private readonly Button _buttonAddAction;

        // Data
        private readonly CrowdActionsViewModel _viewModel;
        private readonly Dictionary<GUID, (VisualElement wrapper, CrowdActionElement element, Label headerLabel)> _actionUiElements = new();

        public CrowdActionsViewElement()
        {
            // Load and setup UI template
            var template = Resources.Load<VisualTreeAsset>($"UXML/{TemplatePath}");
            var container = template.CloneTree();
            container.style.flexGrow = 1;
            Add(container);

            // Get UI references
            _scrollViewActions = container.Q<ScrollView>("scroll-view-actions");
            _buttonAddAction = container.Q<Button>("button-add-action");

            // Initialize
            _viewModel = EditorServiceProvider.Instance
                .GetService<CrowdActionsViewModel>();
            SetupEventHandlers();
            LoadAllActions();
        }

        private void SetupEventHandlers()
        {
            _buttonAddAction.clicked += CreateNewAction;
        }

        private void LoadAllActions()
        {
            _scrollViewActions.Clear();
            _actionUiElements.Clear();

            foreach (var entry in _viewModel.GetEntries())
            {
                AddActionToUI(entry);
            }
        }

        private void CreateNewAction()
        {
            var newEntry = _viewModel.CreateNewEntry();
            AddActionToUI(newEntry);
        }

        private void AddActionToUI(CrowdActionRegistryEntry entry)
        {
            // Create container
            var wrapper = new VisualElement();
            wrapper.style.marginBottom = 10;
            wrapper.style.borderLeftWidth = 1;
            wrapper.style.borderRightWidth = 1;
            wrapper.style.borderTopWidth = 1;
            wrapper.style.borderBottomWidth = 1;
            wrapper.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
            wrapper.style.borderRightColor = new Color(0.5f, 0.5f, 0.5f);
            wrapper.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f);
            wrapper.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f);
            wrapper.style.borderBottomLeftRadius = 5;
            wrapper.style.borderBottomRightRadius = 5;
            wrapper.style.borderTopLeftRadius = 5;
            wrapper.style.borderTopRightRadius = 5;

            // Create foldout with custom header
            var foldout = new Foldout { value = true };
            var (header, headerLabel) = SetupFoldoutHeader(
                    foldout,
                    entry.CrowdAction.name,
                    () => RemoveAction(entry.Guid, wrapper)
            );
            headerLabel.text = entry.CrowdAction.identifier;


            // Create action element
            var actionElement = new CrowdActionElement();
            actionElement.LoadCrowdAction(entry.CrowdAction);

            var fieldIdentifier = actionElement.Q<TextField>("field-identifier");
            fieldIdentifier.RegisterValueChangedCallback(evt =>
            {
                headerLabel.text = evt.newValue;
            });

            actionElement.OnSave += updatedAction => SaveAction(entry.Guid, updatedAction, wrapper);

            // Assemble UI
            foldout.Add(actionElement);
            wrapper.Add(foldout);
            _scrollViewActions.Add(wrapper);

            // Track in dictionary
            _actionUiElements[entry.Guid] = (wrapper, actionElement, headerLabel);
        }

        private (VisualElement header, Label nameLabel) SetupFoldoutHeader(Foldout foldout, string actionName, Action onRemove)
        {
            var toggle = foldout.Q<Toggle>();
            toggle.Clear();

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    alignItems = Align.Center,
                    flexGrow = 1,
                    paddingTop = 5,
                    paddingBottom = 5
                }
            };

            // Arrow icon
            var arrow = new VisualElement();
            arrow.AddToClassList("foldout-arrow");
            header.Add(arrow);

            // Action name label
            var nameLabel = new Label(actionName)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    flexGrow = 1,
                    marginLeft = 5
                }
            };
            header.Add(nameLabel);

            // Remove button
            var removeBtn = new Button(onRemove)
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
            header.Add(removeBtn);

            toggle.Add(header);
            return (header, nameLabel);
        }

        private void SaveAction(GUID entryGuid, CrowdAction updatedAction, VisualElement wrapper)
        {
            _viewModel.SaveEntry(entryGuid, updatedAction);

            // Update the UI if identifier changed
            if (_actionUiElements.TryGetValue(entryGuid, out var existing))
            {
                if (existing.wrapper != wrapper)
                {
                    _scrollViewActions.Remove(wrapper);
                    _actionUiElements.Remove(entryGuid);
                    AddActionToUI(new CrowdActionRegistryEntry
                    {
                        Guid = entryGuid,
                        NeverSaved = false,
                        CrowdAction = updatedAction
                    });
                }
                else
                {
                    // Update the name in the header
                    existing.headerLabel.text = updatedAction.identifier;
                }
            }
        }

        private void RemoveAction(GUID guid, VisualElement wrapper)
        {
            if (_actionUiElements.ContainsKey(guid))
            {
                _viewModel.RemoveEntry(guid);
                _scrollViewActions.Remove(wrapper);
                _actionUiElements.Remove(guid);
            }
        }
    }
}