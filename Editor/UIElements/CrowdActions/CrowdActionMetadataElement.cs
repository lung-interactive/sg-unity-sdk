using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    [UxmlElement]
    public partial class CrowdActionMetadataElement : VisualElement
    {
        private const string TemplatePath = "CrowdActions/CrowdActionMetadataElement";

        public enum Platform { Twitch, Youtube, Kick, Discord, Steam }

        // UI References
        private readonly TextField _fieldAgent;
        private readonly EnumField _fieldPlatform;
        private readonly Button _buttonAddAdditionalValue;
        private readonly VisualElement _containerValues;

        public CrowdActionMetadataElement()
        {
            var template = Resources.Load<VisualTreeAsset>($"UXML/{TemplatePath}");
            var container = template.CloneTree();
            container.style.flexGrow = 1;
            Add(container);

            // Get references
            _fieldAgent = container.Q<TextField>("field-agent");
            _fieldPlatform = container.Q<EnumField>("field-platform");
            _buttonAddAdditionalValue = container.Q<Button>("button-add-additional-value");
            _containerValues = container.Q<VisualElement>("container-values");

            // Initialize
            _fieldPlatform.Init(Platform.Twitch);
            _buttonAddAdditionalValue.clicked += AddNewAdditionalField;
        }

        private void AddNewAdditionalField()
        {
            var fieldElement = new AdditionalMetadataFieldElement();
            AddAdditionalFieldToView(fieldElement);
        }

        private void AddAdditionalFieldToView(AdditionalMetadataFieldElement fieldElement)
        {
            fieldElement.style.flexGrow = 1;
            var wrapper = new VisualElement();
            wrapper.style.flexDirection = FlexDirection.Row;
            wrapper.style.alignItems = Align.Center;
            wrapper.style.marginBottom = 5;

            var removeButton = new Button(() => _containerValues.Remove(wrapper))
            {
                text = "×",
                style =
                {
                    width = 25,
                    height = 20,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginLeft = 5
                }
            };

            wrapper.Add(fieldElement);
            wrapper.Add(removeButton);
            _containerValues.Add(wrapper);
        }

        public void LoadMetadata(CrowdActionMetadata metadata)
        {
            if (metadata == null) return;

            _fieldAgent.value = metadata.agent;
            _fieldPlatform.value = ParsePlatformString(metadata.platform);
            _containerValues.Clear();

            if (metadata.additional_data != null)
            {
                foreach (var key in metadata.additional_data.Keys)
                {
                    var value = metadata.additional_data[key];
                    if (value != null)
                    {
                        var fieldElement = new AdditionalMetadataFieldElement();
                        fieldElement.LoadKeyValuePair(key, value);
                        AddAdditionalFieldToView(fieldElement);
                    }
                }
            }
        }

        public CrowdActionMetadata GetMetadata()
        {
            var metadata = new CrowdActionMetadata
            {
                agent = _fieldAgent.value,
                platform = PlatformToString((Platform)_fieldPlatform.value)
            };

            foreach (var child in _containerValues.Children())
            {
                if (child.childCount > 0 && child[0] is AdditionalMetadataFieldElement fieldElement)
                {
                    var (key, value) = fieldElement.GetKeyValuePair();
                    if (fieldElement.Validate())
                    {
                        metadata.additional_data[key] = value;
                    }
                }
            }

            return metadata;
        }

        public static string PlatformToString(Platform platform) => platform.ToString().ToLower();
        public static Platform ParsePlatformString(string platform) =>
            Enum.TryParse(platform, true, out Platform result) ? result : Platform.Twitch;
    }
}