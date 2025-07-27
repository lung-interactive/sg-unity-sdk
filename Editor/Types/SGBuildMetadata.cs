namespace SGUnitySDK.Editor
{
    [System.Serializable]
    public struct SGBuildMetadata
    {
        public string filename;
        public string executable_name;
        public string version;
        public ulong disk_size;
        public ulong download_size;
        public string platform;
    }
}