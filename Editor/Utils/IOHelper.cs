using System;
using System.IO;
using UnityEngine;

namespace SGUnitySDK.Editor.Utils
{
    public static class IOHelper
    {
        /// <summary>
        /// Deletes all contents of a directory (files and subdirectories) without deleting the main directory.
        /// </summary>
        /// <param name="directoryPath">Path of the directory whose contents will be deleted</param>
        /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist</exception>
        public static void ClearDirectoryContents(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogWarning($"Directory not found for content clearing: {directoryPath}.");
                return;
            }

            // Delete all files
            foreach (string file in Directory.GetFiles(directoryPath))
            {
                File.Delete(file);
            }

            // Delete all subdirectories recursively
            foreach (string subDirectory in Directory.GetDirectories(directoryPath))
            {
                Directory.Delete(subDirectory, true);
            }
        }
    }
}
