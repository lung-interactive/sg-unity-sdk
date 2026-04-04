namespace SGUnitySDK.Editor.Presentation.ViewModels
{
    /// <summary>
    /// ViewModel for runtime panel window presentation metadata.
    /// </summary>
    public sealed class RuntimePanelViewModel
    {
        /// <summary>
        /// Gets the UXML template path for runtime panel layout.
        /// </summary>
        public string TemplatePath => "RuntimePanel";

        /// <summary>
        /// Gets the window title used by runtime panel.
        /// </summary>
        public string WindowTitle => "RuntimePanel";
    }
}