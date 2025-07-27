namespace SGUnitySDK
{
    [System.Serializable]
    public struct CompressingResult
    {
        public string output;
        public ulong sizeCompressed;
        public ulong sizeUncompressed;
        public int fileCount;
        public CompressionPlatform platform;
    }
}