using System;

namespace SGUnitySDK
{
    [System.Serializable]
    public struct ProcessedArgument
    {
        public string key;
        public ArgumentType type;
        public object value;

        public readonly bool TryGetValue<T>(out T result)
        {
            try
            {
                result = (T)Convert.ChangeType(value, typeof(T));
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }
}