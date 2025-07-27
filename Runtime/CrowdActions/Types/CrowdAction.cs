namespace SGUnitySDK
{
    [System.Serializable]
    public struct CrowdAction
    {
        public string identifier;
        public string name;
        public ProcessedArgument[] processed_arguments;
        public CrowdActionMetadata metadata;
    }
}