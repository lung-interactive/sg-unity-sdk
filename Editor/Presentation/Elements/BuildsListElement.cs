using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using System.Threading.Tasks;
using SGUnitySDK.Editor.Core.Entities;
using UnityEditor;
using SGUnitySDK.Editor.Presentation.ViewModels;
using SGUnitySDK.Editor.Infrastructure;

namespace SGUnitySDK.Editor.Presentation.Elements
{
    /// <summary>
    /// VisualElement responsible for managing the list of build items.
    /// Encapsulates population, refresh and batch upload behavior.
    /// </summary>
    public class BuildsListElement : VisualElement
    {
        private readonly ScrollView _scrollView;
        private readonly DevelopmentStepViewModel _viewModel;
        private readonly DevelopmentProcessStateViewModel _processState;
        private List<SGVersionBuildEntry> _entries = new List<SGVersionBuildEntry>();
        private Button _uploadAllButton;
        private List<BuildItemElement> _items = new List<BuildItemElement>();

        /// <summary>
        /// Creates a builds list that renders into the provided scroll view.
        /// The scroll view is not owned by this element but used as a rendering
        /// container to keep compatibility with existing UXML templates.
        /// </summary>
        /// <param name="scrollView">ScrollView from the template where items will be rendered.</param>
        /// <param name="viewModel">View model used for upload operations.</param>
        public BuildsListElement(
            ScrollView scrollView,
            DevelopmentStepViewModel viewModel,
            DevelopmentProcessStateViewModel processState = null)
        {
            _scrollView = scrollView ?? throw new System.ArgumentNullException(nameof(scrollView));
            _viewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            _processState = processState ?? EditorServiceProvider.Instance
                .GetService<DevelopmentProcessStateViewModel>();
        }

        /// <summary>
        /// Creates a builds list and also injects an "Upload All" button
        /// into the provided parent container. The button will trigger
        /// `UploadAllAsync` when clicked and will be disabled while the
        /// operation runs.
        /// </summary>
        /// <param name="scrollView">ScrollView to render items into.</param>
        /// <param name="parentContainer">Parent container where the Upload All button will be placed.</param>
        /// <param name="viewModel">View model used for upload operations.</param>
        public BuildsListElement(
            ScrollView scrollView,
            VisualElement parentContainer,
            DevelopmentStepViewModel viewModel,
            DevelopmentProcessStateViewModel processState = null)
            : this(scrollView, viewModel, processState)
        {
            if (parentContainer == null) return;

            _uploadAllButton = parentContainer.Q<Button>("button-upload-all");
            if (_uploadAllButton == null)
            {
                _uploadAllButton = new Button();
                _uploadAllButton.name = "button-upload-all";
                _uploadAllButton.text = "Upload All Unuploaded";
                _uploadAllButton.style.marginLeft = 8;
                parentContainer.Add(_uploadAllButton);
            }

            _uploadAllButton.clicked += async () =>
            {
                _uploadAllButton.SetEnabled(false);
                try
                {
                    await UploadAllAsync();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Upload All failed: {ex.Message}");
                    EditorUtility.DisplayDialog("Upload All failed", ex.Message, "OK");
                }
                finally
                {
                    // Re-evaluate enabled state based on current entries
                    _uploadAllButton.SetEnabled(!_entries.TrueForAll(e => e.uploaded));
                }
            };
        }

        /// <summary>
        /// Replace the current list of build entries and re-render items.
        /// </summary>
        public void SetBuilds(List<SGVersionBuildEntry> entries)
        {
            _entries = entries ?? new List<SGVersionBuildEntry>();
            Refresh();
            // Update UploadAll button enabled state: enable only if some entries are not uploaded
            if (_uploadAllButton != null)
            {
                _uploadAllButton.SetEnabled(!_entries.TrueForAll(e => e.uploaded));
            }
        }

        /// <summary>
        /// Re-render items into the scroll view.
        /// </summary>
        public void Refresh()
        {
            _scrollView.Clear();
            _items.Clear();

            if (_entries == null || _entries.Count == 0)
            {
                _scrollView.Add(new Label("No builds prepared."));
                return;
            }

            foreach (var entry in _entries)
            {
                var item = new BuildItemElement(entry, _viewModel, _processState);
                _items.Add(item);
                _scrollView.Add(item);
                // Disable upload button for already uploaded entries
                item.SetUploadEnabled(!entry.uploaded);
            }
        }

        /// <summary>
        /// Performs batch upload using `UploadMultipleBuildsUseCase` and
        /// applies returned entries into the process state store.
        /// This method is async and intended to be awaited by callers.
        /// </summary>
        public async Task<List<SGVersionBuildEntry>> UploadAllAsync()
        {
            if (!_processState.IsDevelopmentStep())
            {
                EditorUtility.DisplayDialog("Upload", "Cannot upload: development process is not in Development step.", "OK");
                return _entries;
            }

            var toUpload = _entries.FindAll(e => !e.uploaded);
            if (toUpload == null || toUpload.Count == 0)
            {
                EditorUtility.DisplayDialog("Upload", "All builds are already uploaded.", "OK");
                return _entries;
            }

            // Disable per-item upload buttons while the batch runs
            foreach (var it in _items)
            {
                it.SetUploadEnabled(false);
            }

            var results = await _viewModel.UploadAllBuildsAsync(toUpload);

            // Apply results on main thread
            EditorApplication.delayCall += () =>
            {
                SetBuilds(_processState.GetVersionBuildsOrEmpty());
                // after applying results, ensure per-item buttons reflect new uploaded state
                foreach (var it in _items)
                {
                    it.SetUploadEnabled(!it.IsUploaded);
                }
            };

            return results;
        }
    }
}
