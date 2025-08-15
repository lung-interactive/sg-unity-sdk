using UnityEngine;
using System;

namespace SGUnitySDK
{
    public static class SGLogger
    {
        private const string PREFIX = "-SGUnitySDK | ";

        #region Basic Logging

        public static void Log(object message)
        {
            Debug.Log(PREFIX + message);
        }

        public static void Log(object message, UnityEngine.Object context)
        {
            Debug.Log(PREFIX + message, context);
        }

        public static void LogFormat(string format, params object[] args)
        {
            Debug.LogFormat(PREFIX + format, args);
        }

        public static void LogFormat(UnityEngine.Object context, string format, params object[] args)
        {
            Debug.LogFormat(context, PREFIX + format, args);
        }

        public static void LogFormat(LogType logType, LogOption logOptions, UnityEngine.Object context, string format, params object[] args)
        {
            Debug.LogFormat(logType, logOptions, context, PREFIX + format, args);
        }

        #endregion

        #region Warning Logging

        public static void LogWarning(object message)
        {
            Debug.LogWarning(PREFIX + message);
        }

        public static void LogWarning(object message, UnityEngine.Object context)
        {
            Debug.LogWarning(PREFIX + message, context);
        }

        public static void LogWarningFormat(string format, params object[] args)
        {
            Debug.LogWarningFormat(PREFIX + format, args);
        }

        public static void LogWarningFormat(UnityEngine.Object context, string format, params object[] args)
        {
            Debug.LogWarningFormat(context, PREFIX + format, args);
        }

        #endregion

        #region Error Logging

        public static void LogError(object message)
        {
            Debug.LogError(PREFIX + message);
        }

        public static void LogError(object message, UnityEngine.Object context)
        {
            Debug.LogError(PREFIX + message, context);
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            Debug.LogErrorFormat(PREFIX + format, args);
        }

        public static void LogErrorFormat(UnityEngine.Object context, string format, params object[] args)
        {
            Debug.LogErrorFormat(context, PREFIX + format, args);
        }

        #endregion

        #region Exception Logging

        public static void LogException(Exception exception)
        {
            Debug.LogError(PREFIX + exception.ToString());
            Debug.LogException(exception);
        }

        public static void LogException(Exception exception, UnityEngine.Object context)
        {
            Debug.LogError(PREFIX + exception.ToString());
            Debug.LogException(exception, context);
        }

        #endregion
    }
}