namespace SGUnitySDK.Data
{
    [System.Serializable]
    public struct Game
    {
        public string id;
        public string name;
        public string description;
        public string cover_url;
        public GameType @type;
        public GameAvailability availability;

        public ConnectionModule[] supported_modules;
    }
}