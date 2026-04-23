using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SGUnitySDK.Initialization
{
    /// <summary>
    /// Represents a root page in launcher.config and provides typed value
    /// access for its fields.
    /// </summary>
    public sealed class InitSettingsPage
    {
        private readonly string _pageName;
        private readonly Dictionary<string, JToken> _fields;

        /// <summary>
        /// Initializes a new empty page wrapper.
        /// </summary>
        /// <param name="pageName">Page name represented by this wrapper.</param>
        private InitSettingsPage(string pageName)
        {
            _pageName = pageName ?? string.Empty;
            _fields = new Dictionary<string, JToken>(
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a page wrapper using a JSON object containing fields.
        /// </summary>
        /// <param name="pageName">Page name represented by this wrapper.</param>
        /// <param name="fields">JSON object with page fields.</param>
        internal InitSettingsPage(string pageName, JObject fields)
            : this(pageName)
        {
            if (fields == null)
            {
                return;
            }

            foreach (var property in fields.Properties())
            {
                _fields[property.Name] = property.Value;
            }
        }

        /// <summary>
        /// Gets the page name represented by this wrapper.
        /// </summary>
        public string PageName => _pageName;

        /// <summary>
        /// Creates an empty page wrapper.
        /// </summary>
        /// <param name="pageName">Requested page name.</param>
        /// <returns>Empty page wrapper with no fields.</returns>
        public static InitSettingsPage Empty(string pageName)
        {
            return new InitSettingsPage(pageName);
        }

        /// <summary>
        /// Checks whether a field exists in the page.
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <returns>True when the field exists.</returns>
        public bool HasField(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return _fields.ContainsKey(key);
        }

        /// <summary>
        /// Gets a snapshot of all field keys in this page.
        /// </summary>
        /// <returns>Collection containing field keys.</returns>
        public IReadOnlyCollection<string> GetFieldKeys()
        {
            return new List<string>(_fields.Keys);
        }

        /// <summary>
        /// Gets a text value by key.
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <param name="defaultValue">Fallback value when key is missing.</param>
        /// <returns>String representation of the field value.</returns>
        public string GetText(string key, string defaultValue = "")
        {
            if (!TryGetToken(key, out var token))
            {
                return defaultValue;
            }

            if (token.Type == JTokenType.String)
            {
                return token.Value<string>();
            }

            if (token is JValue value && value.Value != null)
            {
                return Convert.ToString(value.Value, CultureInfo.InvariantCulture)
                    ?? defaultValue;
            }

            return token.ToString(Formatting.None);
        }

        /// <summary>
        /// Gets a boolean value by key.
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <param name="defaultValue">Fallback value when key is missing or invalid.</param>
        /// <returns>Parsed boolean value.</returns>
        public bool GetBool(string key, bool defaultValue = false)
        {
            if (!TryGetToken(key, out var token))
            {
                return defaultValue;
            }

            if (token.Type == JTokenType.Boolean)
            {
                return token.Value<bool>();
            }

            if (token.Type == JTokenType.Integer)
            {
                return token.Value<long>() != 0;
            }

            if (token.Type == JTokenType.Float)
            {
                return Math.Abs(token.Value<double>()) > double.Epsilon;
            }

            if (token.Type == JTokenType.String)
            {
                var textValue = token.Value<string>();

                if (bool.TryParse(textValue, out var parsedBool))
                {
                    return parsedBool;
                }

                if (long.TryParse(
                    textValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsedInt))
                {
                    return parsedInt != 0;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets an integer value by key.
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <param name="defaultValue">Fallback value when key is missing or invalid.</param>
        /// <returns>Parsed integer value.</returns>
        public int GetInt(string key, int defaultValue = 0)
        {
            if (!TryGetToken(key, out var token))
            {
                return defaultValue;
            }

            if (token.Type == JTokenType.Integer)
            {
                return token.Value<int>();
            }

            if (token.Type == JTokenType.Float)
            {
                return (int)Math.Round(token.Value<double>());
            }

            if (token.Type == JTokenType.String &&
                int.TryParse(
                    token.Value<string>(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsedInt))
            {
                return parsedInt;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets a floating-point value by key.
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <param name="defaultValue">Fallback value when key is missing or invalid.</param>
        /// <returns>Parsed floating-point value.</returns>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (!TryGetToken(key, out var token))
            {
                return defaultValue;
            }

            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
            {
                return token.Value<float>();
            }

            if (token.Type == JTokenType.String &&
                float.TryParse(
                    token.Value<string>(),
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out var parsedFloat))
            {
                return parsedFloat;
            }

            return defaultValue;
        }

        /// <summary>
        /// Tries to resolve a raw JSON token by field key.
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <param name="token">Resolved token when found.</param>
        /// <returns>True when token exists.</returns>
        private bool TryGetToken(string key, out JToken token)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                token = null;
                return false;
            }

            return _fields.TryGetValue(key, out token);
        }
    }
}