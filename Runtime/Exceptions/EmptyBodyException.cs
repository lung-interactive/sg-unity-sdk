namespace SGUnitySDK
{
    public class EmptyBodyException : System.Exception
    {
        private static readonly string _message = $"Trying to read an empty body";
        public EmptyBodyException() : base(_message) { }
    }
}