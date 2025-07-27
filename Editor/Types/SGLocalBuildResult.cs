using UnityEditor;

namespace SGUnitySDK.Editor
{
    [System.Serializable]
    public struct SGLocalBuildResult
    {
        public bool success;
        public string productName;
        public string path;
        public string executableName;
        public BuildTarget platform;
        public CompressingResult compression;
        public long unixTimestamp; // Serialized as Unix timestamp
        public string errorMessage;

        // Property to handle conversion
        public System.DateTime BuiltAt
        {
            readonly get => System.DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
            set => unixTimestamp = new System.DateTimeOffset(value).ToUnixTimeSeconds();
        }

        public readonly BuildPlatform GetBuildPlatform()
        {
            return platform switch
            {
                BuildTarget.StandaloneWindows
                or BuildTarget.StandaloneWindows64 => BuildPlatform.Windows,
                BuildTarget.StandaloneLinux64 => BuildPlatform.Linux,
                BuildTarget.StandaloneOSX => BuildPlatform.MacOS,
                _ => BuildPlatform.Windows
            };
        }
    }
}