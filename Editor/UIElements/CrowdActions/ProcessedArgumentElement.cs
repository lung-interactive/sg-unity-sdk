using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    [UxmlElement]
    public partial class ProcessedArgumentElement : VisualElement
    {
        private static readonly string TemplatePath = "CrowdActions/ProcessedArgumentElement";

        private readonly TemplateContainer _containerMain;

        private readonly VisualElement _containerValue;
        private readonly TextField _fieldKey;
        private readonly EnumField _fieldType;
        private readonly TextField _fieldValueString;
        private readonly IntegerField _fieldValueInteger;
        private readonly FloatField _fieldValueFloat;
        private readonly Toggle _toggleValueBoolean;

        public ProcessedArgumentElement()
        {
            style.flexGrow = 1;

            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplatePath}").CloneTree();
            _containerMain.style.flexGrow = 1;

            _containerValue = _containerMain.Q<VisualElement>("container-value");
            _fieldKey = _containerMain.Q<TextField>("field-key");
            _fieldType = _containerMain.Q<EnumField>("field-type");
            _fieldValueString = _containerMain.Q<TextField>("field-value-string");
            _fieldValueInteger = _containerMain.Q<IntegerField>("field-value-integer");
            _fieldValueFloat = _containerMain.Q<FloatField>("field-value-float");
            _toggleValueBoolean = _containerMain.Q<Toggle>("toggle-value-boolean");

            Add(_containerMain);

            InitializeFields();

            _fieldType.Init(ArgumentType.None);
            UpdateValueFieldVisibility();
        }

        private void InitializeFields()
        {
            _fieldType.RegisterValueChangedCallback(OnFieldTypeChanged);
        }

        private void OnFieldTypeChanged(ChangeEvent<Enum> evt)
        {
            UpdateValueFieldVisibility();

            var newType = (ArgumentType)evt.newValue;
            switch (newType)
            {
                case ArgumentType.String:
                    _fieldValueInteger.value = 0;
                    _fieldValueFloat.value = 0f;
                    _toggleValueBoolean.value = false;
                    break;
                case ArgumentType.Integer:
                    _fieldValueString.value = string.Empty;
                    _fieldValueFloat.value = 0f;
                    _toggleValueBoolean.value = false;
                    break;
                case ArgumentType.Float:
                    _fieldValueString.value = string.Empty;
                    _fieldValueInteger.value = 0;
                    _toggleValueBoolean.value = false;
                    break;
                case ArgumentType.Boolean:
                    _fieldValueString.value = string.Empty;
                    _fieldValueInteger.value = 0;
                    _fieldValueFloat.value = 0f;
                    break;
            }
        }

        private void UpdateValueFieldVisibility()
        {
            _fieldValueString.style.display = DisplayStyle.None;
            _fieldValueInteger.style.display = DisplayStyle.None;
            _fieldValueFloat.style.display = DisplayStyle.None;
            _toggleValueBoolean.style.display = DisplayStyle.None;

            // Show only the relevant field based on the selected type
            var currentType = (ArgumentType)_fieldType.value;

            if (currentType == ArgumentType.None)
            {
                _containerValue.style.display = DisplayStyle.None;
                return;
            }

            _containerValue.style.display = DisplayStyle.Flex;

            switch (currentType)
            {
                case ArgumentType.String:
                    _fieldValueString.style.display = DisplayStyle.Flex;
                    break;
                case ArgumentType.Integer:
                    _fieldValueInteger.style.display = DisplayStyle.Flex;
                    break;
                case ArgumentType.Float:
                    _fieldValueFloat.style.display = DisplayStyle.Flex;
                    break;
                case ArgumentType.Boolean:
                    _toggleValueBoolean.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        public void LoadProcessedArgument(ProcessedArgument argument)
        {
            _fieldKey.value = argument.key;
            _fieldType.value = argument.type;

            switch (argument.type)
            {
                case ArgumentType.String:
                    _fieldValueString.value = argument.value as string;
                    break;
                case ArgumentType.Integer:
                    _fieldValueInteger.value = argument.value is int i ? i : 0;
                    break;
                case ArgumentType.Float:
                    _fieldValueFloat.value = argument.value is float f ? f : 0f;
                    break;
                case ArgumentType.Boolean:
                    _toggleValueBoolean.value = argument.value is bool b && b;
                    break;
            }

            UpdateValueFieldVisibility();
        }

        public ProcessedArgument GetProcessedArgument()
        {
            var argument = new ProcessedArgument
            {
                key = _fieldKey.value,
                type = (ArgumentType)_fieldType.value
            };

            argument.value = argument.type switch
            {
                ArgumentType.String => _fieldValueString.value,
                ArgumentType.Integer => _fieldValueInteger.value,
                ArgumentType.Float => _fieldValueFloat.value,
                ArgumentType.Boolean => _toggleValueBoolean.value,
                _ => null
            };

            return argument;
        }
    }
}
