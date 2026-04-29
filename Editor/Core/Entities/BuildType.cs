namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Identifies the execution context of a generated build artifact.
    /// Client builds are delivered to players, while server builds are
    /// deployed to backend runtime environments.
    /// </summary>
    public enum BuildType
    {
        /// <summary>
        /// Playable client build distributed to game users.
        /// </summary>
        Client = 0,

        /// <summary>
        /// Dedicated server build executed in server environments.
        /// </summary>
        Server = 1,
    }
}