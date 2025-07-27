using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace SGUnitySDK.Editor.Versioning
{
    public static class SGVersionLogger
    {
        private static List<string> logEntries = new();
        private static string logFilePath;
        private static bool errorOccurred = false;

        public static void Initialize()
        {
            logEntries.Clear();
            errorOccurred = false;
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var logDirectory = Path.Combine(Application.persistentDataPath, "Versioning");
            Directory.CreateDirectory(logDirectory);
            logFilePath = Path.Combine(logDirectory, $"version_log_{timestamp}.txt");

            Log("Versioning process started");
            Log($"Timestamp: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
        }

        public static void Log(string message)
        {
            var entry = $"[{DateTime.Now.ToString("HH:mm:ss.fff")}] {message}";
            logEntries.Add(entry);
            Debug.Log(entry);
        }

        public static void LogError(string errorMessage)
        {
            var entry = $"[{DateTime.Now.ToString("HH:mm:ss.fff")}] ERROR: {errorMessage}";
            logEntries.Add(entry);
            Debug.LogError(entry);
            errorOccurred = true;
        }

        public static void SaveLog(bool openFile = false)
        {
            try
            {
                File.WriteAllLines(logFilePath, logEntries);

                if (errorOccurred || openFile)
                {
                    EditorUtility.RevealInFinder(logFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save log file: {ex.Message}");
            }
        }

        public static string GetLogContent()
        {
            return string.Join(Environment.NewLine, logEntries);
        }
    }
}