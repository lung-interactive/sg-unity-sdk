using System.IO;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public static class VersionStepElementsHelpers
    {
        public static VisualElement CreateBuildItemElement(SGLocalBuildResult build)
        {
            var item = new VisualElement();
            item.AddToClassList("build-item");

            // Validate zip file existence and integrity
            bool isZipValid = ValidateZipFile(build.path);
            bool showAsFailed = !build.success || !isZipValid;

            // Status indicator
            var statusIndicator = new VisualElement();
            statusIndicator.AddToClassList("build-status");
            statusIndicator.AddToClassList(showAsFailed ? "failed" : "success");

            // Platform icon
            var platformIcon = new VisualElement();
            platformIcon.AddToClassList("platform-icon");
            platformIcon.AddToClassList(build.platform.ToString().ToLower());

            // Main info container
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList("build-info");

            // Platform and date
            var platformDate = new Label($"{build.platform} - {build.BuiltAt:g}");
            platformDate.AddToClassList("build-platform-date");

            // Path with warning if invalid
            var pathContainer = new VisualElement();
            pathContainer.AddToClassList("path-container");

            var pathLabel = new Label(build.path);
            pathLabel.AddToClassList("build-path");
            pathContainer.Add(pathLabel);

            if (!isZipValid)
            {
                var warningIcon = new VisualElement();
                warningIcon.AddToClassList("warning-icon");
                warningIcon.tooltip = "Zip file is missing or corrupted";
                pathContainer.Add(warningIcon);
            }

            // Error messages
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
            }

            infoContainer.Add(platformDate);
            infoContainer.Add(pathContainer);
            infoContainer.Add(errorContainer);

            item.Add(statusIndicator);
            item.Add(platformIcon);
            item.Add(infoContainer);

            return item;
        }

        private static bool ValidateZipFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            // Basic zip file validation
            try
            {
                // Check file extension
                if (!path.EndsWith(".zip", System.StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Quick check for zip file signature
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[4];
                    fs.Read(buffer, 0, 4);
                    // ZIP file signature: 0x50 0x4B 0x03 0x04
                    if (buffer[0] != 0x50 || buffer[1] != 0x4B ||
                        buffer[2] != 0x03 || buffer[3] != 0x04)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}