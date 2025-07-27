using System;
using Newtonsoft.Json;
using UnityEngine;

namespace SGUnitySDK.Http
{
    /// <summary>
    /// Represents the response of an HTTP request. <br />
    /// Use the <see cref="ReadBodyData"/> to cast the response body to a specific type.
    /// </summary>
    public class SGHttpResponse
    {
        #region Fields

        private readonly string _contents;

        private readonly SGHttpRequest _originalRequest;
        private readonly bool _success;
        private readonly string _httpErrorMessage;
        private readonly long _responseCode;

        #endregion

        #region Getters

        /// <summary>
        /// If the request was successful.
        /// </summary>
        public bool Success => _success;

        /// <summary>
        /// The HTTP status code of the response.
        /// </summary>
        public long ResponseCode => _responseCode;

        /// <summary>
        /// The original request.
        /// </summary>
        public SGHttpRequest OriginalRequest => _originalRequest;

        /// <summary>
        /// The contents of the response body as a string.
        /// </summary>
        public string Contents => _contents;

        /// <summary>
        /// If the request resulted in error, this holds the Http error message. 
        /// Important: This is not a message provided in the response body. This is machine generated.
        /// </summary>
        public string HttpErrorMessage => _httpErrorMessage;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new HttpResponse
        /// </summary>
        /// <param name="originalRequest"></param>
        /// <param name="requestSuccess"></param>
        /// <param name="responseContents"></param>
        /// <param name="responseHttpErrorMessage"></param>
        public SGHttpResponse(
            SGHttpRequest originalRequest,
            bool requestSuccess,
            long responseCode,
            string responseContents,
            string responseHttpErrorMessage
        )
        {
            _originalRequest = originalRequest;
            _success = requestSuccess;
            _responseCode = responseCode;
            _contents = responseContents;
            _httpErrorMessage = responseHttpErrorMessage;
        }

        #endregion

        #region Data

        /// <summary>
        /// Reads the body of the response as a JSON object.
        /// </summary>
        /// <typeparam name="T">The type of the data to read from the body.</typeparam>
        /// <returns>The data read from the body as an object of type T.</returns>
        /// <exception cref="EmptyBodyException">Thrown if the response body is empty.</exception>
        public T ReadBodyData<T>()
        {
            var body = ReadBody<T>();
            return body.data;
        }

        /// <summary>
        /// Reads the body of the response as a JSON object.
        /// </summary>
        /// <typeparam name="T">The type of the data to read from the body.</typeparam>
        /// <returns>The <see cref="HMSReponseStructure"/> containing the data read from the body.</returns>
        /// <exception cref="EmptyBodyException">Thrown if the response body is empty.</exception>
        public SGReponseStructure<T> ReadBody<T>()
        {
            if (!typeof(T).IsSerializable && typeof(T) != typeof(string))
            {
                throw new ArgumentException($"Trying to read non serializable type {typeof(T)}");
            }

            if (string.IsNullOrEmpty(_contents))
            {
                throw new EmptyBodyException();
            }

            try
            {
                return JsonConvert.DeserializeObject<SGReponseStructure<T>>(_contents);

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to read response body: {_contents}");
                throw ex;
            }

        }

        public SGErrorBody ReadErrorBody()
        {
            try
            {
                return JsonConvert.DeserializeObject<SGErrorBody>(_contents);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to read response body: {_contents}");
                throw ex;
            }
        }

        [System.Serializable]
        public class SGReponseStructure<T>
        {
            public T data;
            public object meta;
        }

        [System.Serializable]
        public class SGErrorBody
        {
            [JsonProperty("status_code")]
            public int StatusCode;
            [JsonProperty("messages")]
            public string[] Messages;
            [JsonProperty("timestamp")]
            public string Timestamp;
            [JsonProperty("path")]
            public string Path;
        }


        #endregion
    }
}