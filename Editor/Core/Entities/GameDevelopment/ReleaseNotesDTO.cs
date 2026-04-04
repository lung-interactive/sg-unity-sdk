using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Represents release notes with multi-language support.
    /// Stores default notes and localized versions for different languages.
    /// </summary>
    [System.Serializable]
    [JsonConverter(typeof(ReleaseNotesConverter))]
    public class ReleaseNotesDTO
    {
        /// <summary>
        /// Default release notes (typically in English).
        /// Used as fallback when localized version is not available.
        /// </summary>
        public string Default;

        /// <summary>
        /// Release notes localized by language code (e.g., "pt-BR", "es").
        /// Dictionary mapping language codes to localized notes.
        /// </summary>
        public Dictionary<string, string> Localized;

        /// <summary>
        /// Retrieves release notes for a specific language.
        /// </summary>
        /// <param name="languageCode">
        /// Language code (e.g., "en", "pt-BR"). Defaults to "en".
        /// </param>
        /// <returns>
        /// Localized notes if available, otherwise default notes,
        /// or empty string if neither exists.
        /// </returns>
        public string GetForLanguage(string languageCode = "en")
        {
            if (Localized != null && Localized.ContainsKey(languageCode))
                return Localized[languageCode];
            return Default ?? string.Empty;
        }
    }
}