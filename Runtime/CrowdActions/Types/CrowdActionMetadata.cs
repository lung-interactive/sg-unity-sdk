using System;
using System.Collections.Generic;
using UnityEngine;

namespace SGUnitySDK
{
    /// <summary>
    /// Stores metadata fields associated with a crowd action.
    /// </summary>
    [Serializable]
    public class CrowdActionMetadata
    {
        /// <summary>
        /// Name of the agent that generated the action.
        /// </summary>
        public string agent;

        /// <summary>
        /// Source platform that emitted the action.
        /// </summary>
        public string platform;

        /// <summary>
        /// Extensible key-value metadata storage.
        /// </summary>
        public AdditionalData additional_data = new();

        /// <summary>
        /// Provides serializable key-value storage for metadata entries.
        /// </summary>
        [Serializable]
        public class AdditionalData
        {
            [SerializeField] private List<string> _keys = new();
            [SerializeField] private List<SerializableValue> _values = new();

            /// <summary>
            /// Gets or sets a metadata value by key.
            /// </summary>
            /// <param name="key">Metadata key.</param>
            /// <returns>
            /// The stored value for the given key, or <see langword="null"/> when not found.
            /// </returns>
            public object this[string key]
            {
                get => GetValue(key);
                set => SetValue(key, value);
            }

            /// <summary>
            /// Retrieves a metadata value by key.
            /// </summary>
            /// <param name="key">Metadata key.</param>
            /// <returns>
            /// The stored value for the given key, or <see langword="null"/> when not found.
            /// </returns>
            private object GetValue(string key)
            {
                var index = _keys.IndexOf(key);
                return index >= 0 ? _values[index].GetValue() : null;
            }

            /// <summary>
            /// Adds or replaces a metadata value for the specified key.
            /// </summary>
            /// <param name="key">Metadata key.</param>
            /// <param name="value">Value to store. Null removes the key.</param>
            private void SetValue(string key, object value)
            {
                if (value == null)
                {
                    Remove(key);
                    return;
                }

                var index = _keys.IndexOf(key);
                if (index >= 0)
                {
                    _values[index] = new SerializableValue(value);
                }
                else
                {
                    _keys.Add(key);
                    _values.Add(new SerializableValue(value));
                }
            }

            /// <summary>
            /// Determines whether the specified key exists.
            /// </summary>
            /// <param name="key">Metadata key.</param>
            /// <returns><see langword="true"/> when the key exists; otherwise <see langword="false"/>.</returns>
            public bool ContainsKey(string key) => _keys.Contains(key);

            /// <summary>
            /// Removes a metadata entry by key.
            /// </summary>
            /// <param name="key">Metadata key.</param>
            public void Remove(string key)
            {
                var index = _keys.IndexOf(key);
                if (index >= 0)
                {
                    _keys.RemoveAt(index);
                    _values.RemoveAt(index);
                }
            }

            /// <summary>
            /// Gets the number of stored metadata entries.
            /// </summary>
            public int Count => _keys.Count;

            /// <summary>
            /// Gets an enumerable view of stored metadata keys.
            /// </summary>
            public IEnumerable<string> Keys => _keys;
        }

        /// <summary>
        /// Represents a boxed serializable value for supported primitive metadata types.
        /// </summary>
        [Serializable]
        private struct SerializableValue
        {
            /// <summary>
            /// Enumerates supported serialized value representations.
            /// </summary>
            public enum ValueType { String, Integer, Float, Boolean }

            [SerializeField] private ValueType _type;
            [SerializeField] private string _stringValue;
            [SerializeField] private int _intValue;
            [SerializeField] private float _floatValue;
            [SerializeField] private bool _boolValue;

            /// <summary>
            /// Creates a serializable wrapper for the provided metadata value.
            /// </summary>
            /// <param name="value">Supported value type: string, int, float, or bool.</param>
            public SerializableValue(object value)
            {
                switch (value)
                {
                    case string s:
                        _type = ValueType.String;
                        _stringValue = s;
                        _intValue = 0;
                        _floatValue = 0f;
                        _boolValue = false;
                        break;
                    case int i:
                        _type = ValueType.Integer;
                        _intValue = i;
                        _stringValue = string.Empty;
                        _floatValue = 0f;
                        _boolValue = false;
                        break;
                    case float f:
                        _type = ValueType.Float;
                        _floatValue = f;
                        _stringValue = string.Empty;
                        _intValue = 0;
                        _boolValue = false;
                        break;
                    case bool b:
                        _type = ValueType.Boolean;
                        _boolValue = b;
                        _stringValue = string.Empty;
                        _intValue = 0;
                        _floatValue = 0f;
                        break;
                    default:
                        throw new ArgumentException("Unsupported value type");
                }
            }

            /// <summary>
            /// Restores the boxed value according to the stored serialized type.
            /// </summary>
            /// <returns>The reconstructed value instance.</returns>
            public readonly object GetValue()
            {
                return _type switch
                {
                    ValueType.String => _stringValue,
                    ValueType.Integer => _intValue,
                    ValueType.Float => _floatValue,
                    ValueType.Boolean => _boolValue,
                    _ => null
                };
            }
        }
    }
}