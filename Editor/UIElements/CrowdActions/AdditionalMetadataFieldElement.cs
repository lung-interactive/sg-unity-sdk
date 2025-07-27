using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public enum AdditionalMetadataValueType { String, Integer, Float, Boolean }

    [UxmlElement]
    public partial class AdditionalMetadataFieldElement : VisualElement
    {
        private const string TemplatePath = "CrowdActions/AdditionalMetadataFieldElement";

        // UI Fields
        private readonly TextField _fieldKey;
        private readonly EnumField _fieldType;
        private readonly TextField _fieldValueString;
        private readonly IntegerField _fieldValueInteger;
        private readonly FloatField _fieldValueFloat;
        private readonly Toggle _toggleValueBoolean;

        public AdditionalMetadataFieldElement()
        {
            var template = Resources.Load<VisualTreeAsset>($"UXML/{TemplatePath}");
            var container = template.CloneTree();
            container.style.flexGrow = 1;
            Add(container);

            // Get references
            _fieldKey = container.Q<TextField>("field-key");
            _fieldType = container.Q<EnumField>("field-type");
            _fieldValueString = container.Q<TextField>("field-value-string");
            _fieldValueInteger = container.Q<IntegerField>("field-value-integer");
            _fieldValueFloat = container.Q<FloatField>("field-value-float");
            _toggleValueBoolean = container.Q<Toggle>("toggle-value-boolean");

            // Initialize
            _fieldType.Init(AdditionalMetadataValueType.String);
            _fieldType.RegisterValueChangedCallback(OnTypeChanged);
            UpdateValueFieldVisibility();
        }

        private void OnTypeChanged(ChangeEvent<Enum> evt)
        {
            UpdateValueFieldVisibility();
            ClearOtherFields((AdditionalMetadataValueType)evt.newValue);
        }

        private void ClearOtherFields(AdditionalMetadataValueType currentType)
        {
            if (currentType != AdditionalMetadataValueType.String) _fieldValueString.value = "";
            if (currentType != AdditionalMetadataValueType.Integer) _fieldValueInteger.value = 0;
            if (currentType != AdditionalMetadataValueType.Float) _fieldValueFloat.value = 0f;
            if (currentType != AdditionalMetadataValueType.Boolean) _toggleValueBoolean.value = false;
        }

        private void UpdateValueFieldVisibility()
        {
            var type = (AdditionalMetadataValueType)_fieldType.value;
            _fieldValueString.style.display = type == AdditionalMetadataValueType.String ? DisplayStyle.Flex : DisplayStyle.None;
            _fieldValueInteger.style.display = type == AdditionalMetadataValueType.Integer ? DisplayStyle.Flex : DisplayStyle.None;
            _fieldValueFloat.style.display = type == AdditionalMetadataValueType.Float ? DisplayStyle.Flex : DisplayStyle.None;
            _toggleValueBoolean.style.display = type == AdditionalMetadataValueType.Boolean ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void LoadKeyValuePair(string key, object value)
        {
            _fieldKey.value = key;

            switch (value)
            {
                case string s:
                    _fieldType.value = AdditionalMetadataValueType.String;
                    _fieldValueString.value = s;
                    break;
                case int i:
                    _fieldType.value = AdditionalMetadataValueType.Integer;
                    _fieldValueInteger.value = i;
                    break;
                case float f:
                    _fieldType.value = AdditionalMetadataValueType.Float;
                    _fieldValueFloat.value = f;
                    break;
                case bool b:
                    _fieldType.value = AdditionalMetadataValueType.Boolean;
                    _toggleValueBoolean.value = b;
                    break;
                default:
                    throw new ArgumentException("Unsupported value type");
            }

            UpdateValueFieldVisibility();
        }

        public KeyValuePair<string, object> GetKeyValuePair()
        {
            var key = _fieldKey.value;
            var value = (AdditionalMetadataValueType)_fieldType.value switch
            {
                AdditionalMetadataValueType.String => (object)_fieldValueString.value,
                AdditionalMetadataValueType.Integer => _fieldValueInteger.value,
                AdditionalMetadataValueType.Float => _fieldValueFloat.value,
                AdditionalMetadataValueType.Boolean => _toggleValueBoolean.value,
                _ => throw new InvalidOperationException("Unknown value type")
            };

            return new KeyValuePair<string, object>(key, value);
        }

        public bool Validate()
        {
            return !string.IsNullOrWhiteSpace(_fieldKey.value);
        }
    }
}