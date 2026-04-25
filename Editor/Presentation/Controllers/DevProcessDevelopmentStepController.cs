using System;
using System.Threading.Tasks;
using SGUnitySDK.Utils;
using UnityEngine;
using SGUnitySDK.Editor.Core.Entities;
using SGUnitySDK.Editor.Core.Singletons;
using SGUnitySDK.Editor.Core.Utils;
using SGUnitySDK.Editor.Presentation.ViewModels;

namespace SGUnitySDK.Editor.Presentation.Controllers
{
    /// <summary>
    /// Controller for the development step logic.
    /// Handles fetching status, accepting versions, and notifies the view.
    /// </summary>
    public class DevProcessDevelopmentStepController
    {
        private readonly DevelopmentStepViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="DevProcessDevelopmentStepController"/> class.
        /// </summary>
        /// <param name="viewModel">ViewModel used by this controller.</param>
        public DevProcessDevelopmentStepController(
            DevelopmentStepViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        /// <summary>
        /// Event triggered when the version in development changes.
        /// </summary>
        public event Action<string> OnVersionChanged;

        /// <summary>
        /// Event triggered when an error occurs in the controller.
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// Event triggered for informational messages.
        /// </summary>
        public event Action<string> OnInfo;

        /// <summary>
        /// Loads the current version in development from the local DevelopmentProcess asset and notifies the view.
        /// Never fetches from the server.
        /// </summary>
        public void LoadDevelopmentVersion()
        {
            try
            {
                var versionString = _viewModel.GetLocalDevelopmentVersionString();
                OnVersionChanged?.Invoke(versionString);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Error loading version: {ex.Message}");
            }
        }

        /// <summary>
        /// Accepts the first available version in development from the server using AcceptVersionUseCase.
        /// </summary>
        /// <returns>Task representing the asynchronous operation.</returns>
        public async Task AcceptDevelopmentVersionAsync()
        {
            try
            {
                var accepted = await _viewModel.AcceptDevelopmentVersionAsync(
                    "SDK pipeline started for build upload.");
                if (accepted)
                {
                    OnInfo?.Invoke("Version accepted and acknowledged.");
                    LoadDevelopmentVersion();
                }
                else
                {
                    OnInfo?.Invoke("No version available to accept/acknowledge.");
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Error accepting version: {ex.Message}");
            }
        }

        /// <summary>
        /// Awaitable wrapper for <see cref="AcceptDevelopmentVersionAsync"/> to be
        /// consumed by Awaitable-based UI flow.
        /// </summary>
        public Awaitable AcceptDevelopmentVersionAwaitable()
        {
            return TaskAwaitableAdapter.FromTask(AcceptDevelopmentVersionAsync());
        }

        /// <summary>
        /// Fetches the current version in development from the server and notifies the view.
        /// </summary>
        public async Task FetchAndNotifyDevelopmentVersionAsync()
        {
            try
            {
                var version = await _viewModel.FetchUnderDevelopmentVersionAsync();
                // If no under-development version exists on the server, do not override
                // the locally stored current version. This preserves the label when the
                // process is at Homologation or another non-under-development state.
                if (version == null)
                {
                    return;
                }

                var versionString = version?.Semver?.Raw ?? "-";
                OnVersionChanged?.Invoke(versionString);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Error fetching development version: {ex.Message}");
            }
        }

        /// <summary>
        /// Awaitable wrapper for <see cref="FetchAndNotifyDevelopmentVersionAsync"/>.
        /// </summary>
        public Awaitable FetchAndNotifyDevelopmentVersionAwaitable()
        {
            return TaskAwaitableAdapter.FromTask(FetchAndNotifyDevelopmentVersionAsync());
        }
    }
}
