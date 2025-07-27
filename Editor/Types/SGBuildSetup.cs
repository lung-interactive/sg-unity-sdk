using UnityEditor;
using UnityEditor.Build.Profile;

namespace SGUnitySDK.Editor
{
    [System.Serializable]
    public class SGBuildSetup
    {
        public BuildTarget platform;
        public BuildProfile profile;
    }
}