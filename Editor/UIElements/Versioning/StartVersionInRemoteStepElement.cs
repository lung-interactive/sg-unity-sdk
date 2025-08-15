using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SGUnitySDK.Editor.Http;
using SGUnitySDK.Editor.Versioning;
using SGUnitySDK.Http;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public class StartVersionInRemoteStepElement : VersioningStepElement
    {
        private static readonly string TemplateName = "SGProcessStep2_StartVersionInRemote";
        public override VersioningStep Step => VersioningStep.StartVersionInRemote;

        private readonly TemplateContainer _containerMain;
        private Button _buttonStartVersion;
        private Button _buttonCancelPreparation;
        private VisualElement _currentErrorElement;

        public StartVersionInRemoteStepElement()
        {
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            _buttonStartVersion = _containerMain.Q<Button>("button-start-version");
            _buttonStartVersion.clicked += OnButtonStartVersionClicked;

            _buttonCancelPreparation = _containerMain.Q<Button>("button-cancel-version-in-preparation");
            _buttonCancelPreparation.clicked += OnButtonCancelPreparationClicked;

            Add(_containerMain);
        }

        public override void Activate(VersioningProcess process)
        {
            base.Activate(process);
            _buttonStartVersion.style.display = DisplayStyle.None;
            _ = EvaluateVersionInPreparation();
        }

        private void OnButtonStartVersionClicked()
        {
            _ = StartVersionWithRemote();
        }

        private void OnButtonCancelPreparationClicked()
        {
            _ = CancelVersionPreparation();
        }

        private void ClearErrors()
        {
            if (_currentErrorElement != null)
            {
                Remove(_currentErrorElement);
                _currentErrorElement = null;
            }
        }

        private void ShowErrors(ErrorMessageBag errors)
        {
            ClearErrors();
            _currentErrorElement = ErrorMessageUSSHelper.CreateErrorElement(errors);
            Add(_currentErrorElement);
        }

        private async Awaitable EvaluateVersionInPreparation()
        {
            var versionInPreparation = await FetchVersionInPreparation();
            if (versionInPreparation != null)
            {
                _buttonStartVersion.style.display = DisplayStyle.None;
                _buttonCancelPreparation.style.display = DisplayStyle.Flex;
                SetReadyStatus(true);
            }
            else
            {
                _buttonStartVersion.style.display = DisplayStyle.Flex;
                _buttonCancelPreparation.style.display = DisplayStyle.None;
            }
        }

        private async Awaitable<VersionDTO> FetchVersionInPreparation()
        {
            ClearErrors();
            var request = GameManagementRequest.To("version-in-preparation");
            try
            {
                var response = await request.SendAsync();
                return response.ReadBodyData<VersionDTO>();
            }
            catch (RequestFailedException ex)
            {
                ShowErrors(ErrorMessageBag.FromException(ex));
                throw;
            }
            catch (Exception ex)
            {
                ShowErrors(new ErrorMessageBag(new[] { ex.Message }));
                throw;
            }
        }

        private async Awaitable StartVersionWithRemote()
        {
            ClearErrors();
            try
            {
                var startedVersion = await SGOperations
                    .StartVersionWithRemote(
                        VersioningProcess.instance,
                        VersioningProcess.instance.TargetVersion
                    );

                if (startedVersion != null)
                {
                    _buttonStartVersion.style.display = DisplayStyle.None;
                    _buttonCancelPreparation.style.display = DisplayStyle.Flex;
                    SetReadyStatus(true);
                }
                else
                {
                    SetReadyStatus(false);
                }
            }
            catch (RequestFailedException ex)
            {
                ShowErrors(ErrorMessageBag.FromException(ex));
                SetReadyStatus(false);
            }
            catch (Exception ex)
            {
                ShowErrors(new ErrorMessageBag(new[] { ex.Message }));
                SetReadyStatus(false);
            }
        }

        private async Awaitable CancelVersionPreparation()
        {
            ClearErrors();
            try
            {
                await SGOperations.CancelVersionPreparation(VersioningProcess.instance);

                _buttonStartVersion.style.display = DisplayStyle.Flex;
                _buttonCancelPreparation.style.display = DisplayStyle.None;
                SetReadyStatus(false);
            }
            catch (RequestFailedException ex)
            {
                ShowErrors(ErrorMessageBag.FromException(ex));
            }
            catch (Exception ex)
            {
                ShowErrors(new ErrorMessageBag(new[] { ex.Message }));
            }
        }

        [System.Serializable]
        private class StartGameVersionUpdateDTO
        {
            [JsonProperty("version_update_type")]
            public VersionUpdateType VersionUpdateType;

            [JsonProperty("specific_version")]
            public string SpecificVersion;

            [JsonProperty("is_prerelease")]
            public bool IsPrerelease;

            [JsonProperty("release_notes")]
            public List<string> ReleaseNotes = new();
        }
    }
}