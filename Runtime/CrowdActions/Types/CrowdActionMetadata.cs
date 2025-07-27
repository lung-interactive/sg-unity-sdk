using System;
using System.Collections.Generic;
using UnityEngine;

namespace SGUnitySDK
{
    [Serializable]
    public class CrowdActionMetadata
    {
        public string agent;
        public string platform;
        public AdditionalData additional_data = new AdditionalData();

        [Serializable]
        public class AdditionalData
        {
            [SerializeField] private List<string> _keys = new List<string>();
            [SerializeField] private List<SerializableValue> _values = new List<SerializableValue>();

            public object this[string key]
            {
                get => GetValue(key);
                set => SetValue(key, value);
            }

            private object GetValue(string key)
            {
                var index = _keys.IndexOf(key);
                return index >= 0 ? _values[index].GetValue() : null;
            }

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

            public bool ContainsKey(string key) => _keys.Contains(key);
            public void Remove(string key)
            {
                var index = _keys.IndexOf(key);
                if (index >= 0)
                {
                    _keys.RemoveAt(index);
                    _values.RemoveAt(index);
                }
            }
            public int Count => _keys.Count;
            public IEnumerable<string> Keys => _keys;
        }

        [Serializable]
        private struct SerializableValue
        {
            public enum ValueType { String, Integer, Float, Boolean }

            [SerializeField] private ValueType _type;
            [SerializeField] private string _stringValue;
            [SerializeField] private int _intValue;
            [SerializeField] private float _floatValue;
            [SerializeField] private bool _boolValue;

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

            public object GetValue()
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