namespace SGUnitySDK.Editor.Core.Entities
{
    public struct CommitVersionResult
    {
        public bool Success;
        public string ErrorMessage;
        public string FailedStep;
        public string GitOutput;
        public string GitError;
    }
}