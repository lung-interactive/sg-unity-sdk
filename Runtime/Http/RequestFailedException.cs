using System;

namespace SGUnitySDK.Http
{
    public class RequestFailedException : Exception
    {
        public SGHttpResponse.SGErrorBody ErrorBody { get; }
        public long ResponseCode { get; }
        public ErrorMessageBag ErrorMessages { get; }

        public RequestFailedException(SGHttpResponse.SGErrorBody errorBody, long responseCode)
            : base($"Request failed with status code {responseCode}")
        {
            ErrorBody = errorBody;
            ResponseCode = responseCode;
            ErrorMessages = ErrorMessageBag.FromException(this);
        }

        public string GetFormattedMessages()
        {
            return ErrorMessages.ToString();
        }
    }
}