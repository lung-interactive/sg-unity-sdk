using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SGUnitySDK.Editor.Core.Entities
{
    /// <summary>
    /// Custom JSON converter for ReleaseNotesDTO.
    /// Handles both simple string and complex localized object formats.
    /// If input is a string, sets it as default.
    /// If input is an object, deserializes to localized dictionary.
    /// </summary>
    public class ReleaseNotesConverter : JsonConverter<ReleaseNotesDTO>
    {
        /// <summary>
        /// Deserializes JSON to ReleaseNotesDTO.
        /// </summary>
        /// <param name="reader">JSON reader positioned at the value.</param>
        /// <param name="objectType">Type being deserialized.</param>
        /// <param name="existingValue">Existing value if applicable.</param>
        /// <param name="hasExistingValue">Whether existing value is present.</param>
        /// <param name="serializer">JSON serializer instance.</param>
        /// <returns>Deserialized ReleaseNotesDTO instance.</returns>
        public override ReleaseNotesDTO ReadJson(
            JsonReader reader,
            Type objectType,
            ReleaseNotesDTO existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            var result = new ReleaseNotesDTO();

            if (reader.TokenType == JsonToken.String)
            {
                result.Default = reader.Value.ToString();
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                var obj = JObject.Load(reader);
                result.Localized = obj.ToObject<Dictionary<string, string>>();

                // Assume English as default if available
                if (result.Localized.ContainsKey("en"))
                {
                    result.Default = result.Localized["en"];
                }
            }

            return result;
        }

        /// <summary>
        /// Serializes ReleaseNotesDTO to JSON.
        /// Outputs localized dictionary if available, otherwise default.
        /// </summary>
        /// <param name="writer">JSON writer instance.</param>
        /// <param name="value">ReleaseNotesDTO to serialize.</param>
        /// <param name="serializer">JSON serializer instance.</param>
        public override void WriteJson(
            JsonWriter writer,
            ReleaseNotesDTO value,
            JsonSerializer serializer
        )
        {
            if (value.Localized != null)
            {
                serializer.Serialize(writer, value.Localized);
            }
            else
            {
                writer.WriteValue(value.Default);
            }
        }
    }
}