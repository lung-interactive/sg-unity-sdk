namespace SGUnitySDK
{
    /// <summary>
    /// Defines supported argument payload types for crowd actions.
    /// </summary>
    public enum ArgumentType
    {
        /// <summary>
        /// No type is defined.
        /// </summary>
        None = 1,

        /// <summary>
        /// String argument.
        /// </summary>
        String = 2,

        /// <summary>
        /// 32-bit integer argument.
        /// </summary>
        Integer = 3,

        /// <summary>
        /// Single-precision floating-point argument.
        /// </summary>
        Float = 4,

        /// <summary>
        /// Boolean argument.
        /// </summary>
        Boolean = 5,
    }
}