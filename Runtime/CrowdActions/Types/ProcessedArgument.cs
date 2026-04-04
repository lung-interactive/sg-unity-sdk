using System;

namespace SGUnitySDK
{
    /// <summary>
    /// Represents a typed action argument after payload processing.
    /// </summary>
    [System.Serializable]
    public struct ProcessedArgument
    {
        /// <summary>
        /// Argument key.
        /// </summary>
        public string key;

        /// <summary>
        /// Declared argument type.
        /// </summary>
        public ArgumentType type;

        /// <summary>
        /// Boxed argument value.
        /// </summary>
        public object value;

        /// <summary>
        /// Attempts to convert the boxed value to the requested target type.
        /// </summary>
        /// <typeparam name="T">Desired output type.</typeparam>
        /// <param name="result">Converted value when successful; otherwise default.</param>
        /// <returns>
        /// <see langword="true"/> when conversion succeeded; otherwise
        /// <see langword="false"/>.
        /// </returns>
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