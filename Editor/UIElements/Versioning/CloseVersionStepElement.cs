using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SGUnitySDK.Editor.Versioning;
using SGUnitySDK.Http;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Editor
{
    public class CloseVersionStepElement : VersioningStepElement
    {
        private static readonly string TemplateName =
            "SGProcessStep4_CloseVersion";

        public override VersioningStep Step => VersioningStep.CloseVersion;

        private readonly TemplateContainer _containerMain;
        private VisualElement _containerSummary;
        private Button _buttonCloseVersion;

        private SGEditorConfig Config => SGEditorConfig.instance;

        /// <summary>
        /// Loads UXML and binds UI references.
        /// </summary>
        public CloseVersionStepElement()
        {
            var asset = Resources.Load<VisualTreeAsset>(
                "UXML/" + TemplateName
            );
            _containerMain = asset.CloneTree();
            _containerMain.style.flexGrow = 1;

            _containerSummary = _containerMain.Q<VisualElement>(
                "container-summary"
            );
            _buttonCloseVersion = _containerMain.Q<Button>(
                "button-close-version"
            );

            _buttonCloseVersion.clicked += OnButtonCloseVersionClicked;

            Add(_containerMain);
        }

        /// <summary>
        /// Activates the step and subscribes to process events.
        /// </summary>
        public override void Activate(VersioningProcess process)
        {
            base.Activate(process);
            BuildSummary();
        }

        /// <summary>
        /// Deactivates the step and unsubscribes from events.
        /// </summary>
        public override void Deactivate()
        {
            base.Deactivate();
        }

        /// <summary>
        /// Placeholder: close version routine will be implemented later.
        /// </summary>
        private void OnButtonCloseVersionClicked()
        {
            var goOn = EditorUtility.DisplayDialog(
                "Close Version",
                "This will close the current version and send it into the testing pipeline. Are you sure?",
                "OK"
            );

            if (!goOn) return;
            _ = CloseVersion();
        }

        private async Awaitable CloseVersion()
        {
            try
            {
                await SGOperations.CloseRemoteVersion(_process, _process.TargetVersion);
                SGOperations.UpdateVersionEverywhere(_process.TargetVersion.Raw);
                VersioningProcess.instance.EndProcess();
            }
            catch (Exception ex)
            {
                SGLogger.LogError($"Failed to close version: {ex.Message}");
            }
        }

        /// <summary>
        /// Rebuilds the summary with only the requested fields.
        /// </summary>
        private void BuildSummary()
        {
            if (_containerSummary != null) _containerSummary.Clear();
            if (_process == null) return;

            // Target Version
            var targetSemver = GetSemverString(_process);
            _containerSummary.Add(MakeKvpRow("Target Version", targetSemver));

            // Builds Directory
            var buildsDir = string.IsNullOrEmpty(Config?.BuildsDirectory)
                ? "—"
                : Config.BuildsDirectory;
            _containerSummary.Add(MakeKvpRow("Builds Directory", buildsDir));

            _containerSummary.Add(MakeSeparator());

            var builds = _process.VersionBuilds;
            if (builds == null || builds.Count == 0)
            {
                _containerSummary.Add(new Label("No builds were prepared."));
                return;
            }

            for (int i = 0; i < builds.Count; i++)
            {
                var card = MakeBuildCard(builds[i]);
                _containerSummary.Add(card);
            }
        }

        /// <summary>
        /// Creates a build card with: Executable, Source Key, sizes, checksum, state.
        /// Reads nested fields like "build.executableName".
        /// </summary>
        private VisualElement MakeBuildCard(object entry)
        {
            var card = new VisualElement();
            card.style.marginBottom = 8;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.paddingLeft = 6;
            card.style.paddingRight = 6;
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = new Color(0, 0, 0, 0.2f);
            card.style.borderBottomColor = new Color(0, 0, 0, 0.2f);
            card.style.borderLeftColor = new Color(0, 0, 0, 0.2f);
            card.style.borderRightColor = new Color(0, 0, 0, 0.2f);
            card.style.borderTopLeftRadius = 3;
            card.style.borderTopRightRadius = 3;
            card.style.borderBottomLeftRadius = 3;
            card.style.borderBottomRightRadius = 3;

            // A) Executable Name
            var executable = ReadPathString(
                entry,
                "build.executableName",
                "build.executable_name",
                "executableName",
                "executable_name"
            );

            // B) Source Key (derive from build.path -> "game-builds/<file>")
            var pathStr = ReadPathString(
                entry,
                "build.path",
                "path"
            );
            var srcKey = "—";
            try
            {
                if (!string.IsNullOrEmpty(pathStr))
                {
                    var file = Path.GetFileName(pathStr);
                    if (!string.IsNullOrEmpty(file))
                    {
                        srcKey = "game-builds/" + file;
                    }
                }
            }
            catch
            {
                // Keep placeholder.
            }

            // C) Download Size (build.compression.sizeCompressed)
            var downloadSize = ReadPathString(
                entry,
                "build.compression.sizeCompressed",
                "compression.sizeCompressed",
                "download_size",
                "DownloadSize"
            );

            // D) Installed Size (build.compression.sizeUncompressed)
            var installedSize = ReadPathString(
                entry,
                "build.compression.sizeUncompressed",
                "compression.sizeUncompressed",
                "installed_size",
                "InstalledSize"
            );

            // E) Checksum (sha256 or checksum)
            var checksum = ReadPathString(
                entry,
                "sha256",
                "checksum",
                "Checksum"
            );

            // F) State (derived)
            var state = DeriveState(entry);

            card.Add(MakeKvpRow(
                "Executable Name",
                string.IsNullOrEmpty(executable) ? "—" : executable
            ));
            card.Add(MakeKvpRow(
                "Source Key",
                string.IsNullOrEmpty(srcKey) ? "—" : srcKey
            ));
            card.Add(MakeKvpRow(
                "Download Size",
                string.IsNullOrEmpty(downloadSize) ? "—" : downloadSize
            ));
            card.Add(MakeKvpRow(
                "Installed Size",
                string.IsNullOrEmpty(installedSize) ? "—" : installedSize
            ));
            card.Add(MakeKvpRow(
                "Checksum",
                string.IsNullOrEmpty(checksum) ? "—" : checksum
            ));
            card.Add(MakeKvpRow(
                "State",
                string.IsNullOrEmpty(state) ? "—" : state
            ));

            return card;
        }

        /// <summary>
        /// Creates a key-value row with compact styling.
        /// </summary>
        private VisualElement MakeKvpRow(string key, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;

            var lblKey = new Label(key);
            lblKey.style.unityFontStyleAndWeight = FontStyle.Bold;

            var lblValue = new Label(value ?? "—");
            lblValue.style.whiteSpace = WhiteSpace.Normal;
            lblValue.style.flexGrow = 1;
            lblValue.style.unityTextAlign = TextAnchor.MiddleRight;

            row.Add(lblKey);
            row.Add(lblValue);
            return row;
        }

        /// <summary>
        /// Thin visual separator.
        /// </summary>
        private VisualElement MakeSeparator()
        {
            var sep = new VisualElement();
            sep.style.height = 1;
            sep.style.marginTop = 6;
            sep.style.marginBottom = 6;
            sep.style.backgroundColor = new Color(0, 0, 0, 0.2f);
            return sep;
        }

        /// <summary>
        /// Extracts the target semver safely.
        /// </summary>
        private string GetSemverString(VersioningProcess process)
        {
            try
            {
                var sv = process.TargetVersion;
                return sv == null ? "—" : sv.Raw;
            }
            catch
            {
                return "—";
            }
        }

        /// <summary>
        /// Human-readable state derived from entry fields.
        /// Treats empty/placeholder uploadError ("—") as no error.
        /// </summary>
        private string DeriveState(object entry)
        {
            var uploaded = ReadPathBool(entry, "uploaded", "isUploaded");

            // Note: ReadPathString returns "—" for empty; we must ignore that.
            var uploadError = ReadPathString(entry, "uploadError");

            var buildOk = ReadPathBool(entry, "build.success", "success");

            if (uploaded)
            {
                var hasRealError =
                    !string.IsNullOrEmpty(uploadError) && uploadError != "—";
                if (hasRealError) return "Upload Error";
                return "Uploaded";
            }

            if (buildOk) return "Built (Not Uploaded)";
            return "Build Failed";
        }

        /// <summary>
        /// Reads the first non-empty string from dotted paths (props or fields).
        /// </summary>
        private string ReadPathString(object root, params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                var val = ReadPath(root, paths[i]);
                if (val == null) continue;

                try
                {
                    var s = Convert.ToString(val);
                    if (!string.IsNullOrEmpty(s)) return s;
                }
                catch { }
            }
            return "—";
        }

        /// <summary>
        /// Reads a boolean from dotted paths. False on failure.
        /// </summary>
        private bool ReadPathBool(object root, params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                var val = ReadPath(root, paths[i]);
                if (val == null) continue;

                try
                {
                    if (val is bool b) return b;
                    var s = Convert.ToString(val);
                    if (bool.TryParse(s, out var parsed)) return parsed;

                    // Accept 0/1 or "0"/"1".
                    if (val is int ii) return ii != 0;
                    if (val is long ll) return ll != 0L;
                    if (int.TryParse(s, out var pInt)) return pInt != 0;
                }
                catch { }
            }
            return false;
        }

        /// <summary>
        /// Reads a value from a dotted path (property/field per segment).
        /// </summary>
        private object ReadPath(object root, string dotted)
        {
            if (root == null || string.IsNullOrEmpty(dotted)) return null;

            var current = root;
            var segments = dotted.Split('.');

            for (int i = 0; i < segments.Length; i++)
            {
                if (current == null) return null;

                var seg = segments[i];
                var t = current.GetType();

                // Property first
                var prop = t.GetProperty(
                    seg,
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase
                );
                if (prop != null)
                {
                    current = prop.GetValue(current, null);
                    continue;
                }

                // Field next
                var field = t.GetField(
                    seg,
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase
                );
                if (field != null)
                {
                    current = field.GetValue(current);
                    continue;
                }

                // Segment not found
                return null;
            }

            return current;
        }

        /// <summary>
        /// Rebuilds the summary when the step changes.
        /// </summary>
        private void OnStepChanged(VersioningStep _)
        {
            BuildSummary();
        }

        /// <summary>
        /// Rebuilds the summary when the target version changes.
        /// </summary>
        private void OnTargetVersionDefined(object _)
        {
            BuildSummary();
        }

        /// <summary>
        /// Rebuilds the summary when the local builds list changes.
        /// </summary>
        private void OnLocalBuildsChanged()
        {
            BuildSummary();
        }
    }
}
