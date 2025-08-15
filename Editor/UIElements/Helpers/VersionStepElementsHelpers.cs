using System.IO;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public static class VersionStepElementsHelpers
    {
        /// <summary>
        /// Creates a UI row for a version build entry.
        /// - Shows "Upload" button only when build is ok, zip exists, and not uploaded yet.
        /// - When uploaded, hides actions and shows a status line below the build path.
        /// </summary>
        public static VisualElement CreateBuildItemElement(
            SGVersionBuildEntry entry,
            System.Action<SGVersionBuildEntry> onUploadClicked = null
        )
        {
            var build = entry.build;

            var item = new VisualElement();
            item.AddToClassList("build-item");

            // Validate zip existence quickly
            bool isZipValid = !string.IsNullOrEmpty(build.path) && File.Exists(build.path);
            bool showAsFailed = !build.success || !isZipValid;

            // Status indicator
            var statusIndicator = new VisualElement();
            statusIndicator.AddToClassList("build-status");
            statusIndicator.AddToClassList(showAsFailed ? "failed" : "success");

            // Platform icon
            var platformIcon = new VisualElement();
            platformIcon.AddToClassList("platform-icon");
            platformIcon.AddToClassList(build.platform.ToString().ToLower());

            // Info column (flex: column)
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList("build-info");

            // Top line: platform + date
            var platformDate = new Label($"{build.platform} - {build.BuiltAt:g}");
            platformDate.AddToClassList("build-platform-date");

            // Path row
            var pathContainer = new VisualElement();
            pathContainer.AddToClassList("path-container");

            var pathLabel = new Label(build.path);
            pathLabel.AddToClassList("build-path");
            pathContainer.Add(pathLabel);

            // Errors stack
            var errorContainer = new VisualElement();
            errorContainer.style.display = DisplayStyle.None;
            errorContainer.AddToClassList("error-container");

            if (!build.success && !string.IsNullOrEmpty(build.errorMessage))
            {
                var errorLabel = new Label(build.errorMessage);
                errorLabel.AddToClassList("build-error");
                errorContainer.Add(errorLabel);
                errorContainer.style.display = DisplayStyle.Flex;
            }

            if (!isZipValid)
            {
                var zipError = new Label("Zip file is missing or corrupted");
                zipError.AddToClassList("build-error");
                errorContainer.Add(zipError);
                errorContainer.style.display = DisplayStyle.Flex;

                var warningIcon = new VisualElement();
                warningIcon.AddToClassList("warning-icon");
                warningIcon.tooltip = "Zip file is missing or corrupted";
                pathContainer.Add(warningIcon);
            }

            // Uploaded status line (goes BELOW the path, INSIDE infoContainer)
            if (!showAsFailed && entry.uploaded)
            {
                var uploadedLine = new VisualElement();
                uploadedLine.AddToClassList("uploaded-line");

                // "Uploaded" bold-ish label
                var uploadedLabel = new Label("Uploaded");
                uploadedLabel.AddToClassList("uploaded-label");

                // Optional URL (can be long -> wrap + tooltip)
                if (!string.IsNullOrEmpty(entry.remoteUrl))
                {
                    var urlLabel = new Label(entry.remoteUrl);
                    urlLabel.AddToClassList("uploaded-url");
                    // allow wrapping for long URLs
                    urlLabel.style.whiteSpace = WhiteSpace.Normal;
                    urlLabel.tooltip = entry.remoteUrl;
                    uploadedLine.Add(uploadedLabel);
                    uploadedLine.Add(urlLabel);
                }
                else
                {
                    uploadedLine.Add(uploadedLabel);
                }

                // Insert below path
                // Order: platformDate, pathContainer, uploadedLine, errorContainer
                infoContainer.Add(platformDate);
                infoContainer.Add(pathContainer);
                infoContainer.Add(uploadedLine);
                infoContainer.Add(errorContainer);
            }
            else
            {
                // No uploaded line; keep default order
                infoContainer.Add(platformDate);
                infoContainer.Add(pathContainer);
                infoContainer.Add(errorContainer);
            }

            // Actions container (right side). Only show Upload button when possible.
            var actions = new VisualElement();
            actions.AddToClassList("build-actions");

            if (!showAsFailed && !entry.uploaded)
            {
                var uploadBtn = new Button();
                uploadBtn.text = "Upload";
                uploadBtn.AddToClassList("upload-button");
                uploadBtn.clicked += () =>
                {
                    uploadBtn.SetEnabled(false);
                    try
                    {
                        onUploadClicked?.Invoke(entry);
                    }
                    finally
                    {
                        uploadBtn.SetEnabled(true);
                    }
                };
                actions.Add(uploadBtn);
            }
            // else: when uploaded or failed -> no actions on the right,
            //       because we moved the status line below the path.

            item.Add(statusIndicator);
            item.Add(platformIcon);
            item.Add(infoContainer);
            item.Add(actions);

            return item;
        }
    }
}
