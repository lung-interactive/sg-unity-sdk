using System;

namespace SGUnitySDK
{
    [Serializable]
    public class CrowdActionPayload
    {
        public string identifier;
        public ProcessedArgument[] arg;
        public CrowdActionMetadata metadata;

        // Método auxiliar para encontrar argumentos
        public ProcessedArgument GetArgument(string key) =>
            Array.Find(arg, a => a.key == key);
    }
}