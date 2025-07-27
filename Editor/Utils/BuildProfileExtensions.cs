using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;

namespace SGUnitySDK.Editor
{
    public static class BuildProfileExtensions
    {
        private static FieldInfo m_BuildTargetField;

        public static BuildTarget GetBuildTargetInternal(this BuildProfile profile)
        {
            if (m_BuildTargetField == null)
            {
                m_BuildTargetField = typeof(BuildProfile).GetField(
                    "m_BuildTarget",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (m_BuildTargetField == null)
                {
                    throw new InvalidOperationException("Não foi possível encontrar o campo m_BuildTarget em BuildProfile");
                }
            }

            return (BuildTarget)m_BuildTargetField.GetValue(profile);
        }
    }
}