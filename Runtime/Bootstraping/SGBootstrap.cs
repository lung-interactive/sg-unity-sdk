using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HMSUnitySDK;
using HMSUnitySDK.LauncherInteroperations;
using UnityEngine;

namespace SGUnitySDK
{
    public static class SGBootstrapper
    {
        /// <summary>
        /// Executed AFTER the HMSBootstrapper
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
        }
    }
}