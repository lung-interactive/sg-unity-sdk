using System;

namespace SGUnitySDK
{
    /// <summary>
    /// Represents a normalized crowd action definition.
    /// </summary>
    [System.Serializable]
    public struct CrowdAction
    {
        /// <summary>
        /// Unique action identifier.
        /// </summary>
        public string identifier;

        /// <summary>
        /// Human-readable action name.
        /// </summary>
        public string name;

        /// <summary>
        /// Parsed and typed argument collection.
        /// </summary>
        public ProcessedArgument[] processed_arguments;

        /// <summary>
        /// Metadata associated with the action source.
        /// </summary>
        public CrowdActionMetadata metadata;

        /// <summary>
        /// Attempts to retrieve a processed argument by identifier.
        /// </summary>
        /// <param name="argumentIdentifier">Argument identifier to search for.</param>
        /// <param name="argument">Matching argument when found; otherwise default.</param>
        /// <returns>
        /// <see langword="true"/> when a matching argument is found; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public readonly bool TryGetArgument(
            string argumentIdentifier,
            out ProcessedArgument argument)
        {
            if (processed_arguments != null)
            {
                for (var index = 0; index < processed_arguments.Length; index++)
                {
                    var currentArgument = processed_arguments[index];
                    if (string.Equals(
                            currentArgument.key,
                            argumentIdentifier,
                            StringComparison.Ordinal))
                    {
                        argument = currentArgument;
                        return true;
                    }
                }
            }

            argument = default;
            return false;
        }
    }
}