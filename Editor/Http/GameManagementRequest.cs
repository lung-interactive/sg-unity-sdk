using SGUnitySDK.Http;
using UnityEngine;

namespace SGUnitySDK.Editor.Http
{
    public class GameManagementRequest
    {
        #region Static Creation

        /// <summary>
        /// Creates and returns a new request setting its base url and HttpMethod
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static SGHttpRequest To(string endpoint, HttpMethod method = HttpMethod.Get)
        {
            if (!SGEditorConfig.instance.IsGMTValid)
            {
                throw new InvalidGMTException(endpoint);
            }

            if (endpoint.StartsWith("/"))
            {
                endpoint = endpoint[1..];
            }

            string url = $"{SGEditorConfig.instance.ApiBaseURL}/game-management/{endpoint}";
            SGHttpRequest request = new();

            request.SetUrl(url);
            request.SetMethod(method);
            request.AddHeader("Content-Type", "application/json");
            request.SetBearerAuth(SGEditorConfig.instance.GameManagementToken);

            return request;
        }

        #endregion
    }
}