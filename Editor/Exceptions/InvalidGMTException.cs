namespace SGUnitySDK
{
    public class InvalidGMTException : System.Exception
    {
        private static readonly string _message = $"Trying to reach endpoint {1} but the Game Management Token is not valid.";
        public InvalidGMTException(string endpoint) : base(string.Format(_message, endpoint))
        {
        }
    }
}