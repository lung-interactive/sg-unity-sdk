using System.Collections.Generic;
using UnityEngine;

namespace SGUnitySDK.Utils
{
    [System.Serializable]
    public abstract class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        #region Inspector

        [HideInInspector]
        [SerializeField]
        private List<TKey> _listKeys = new();

        [HideInInspector]
        [SerializeField]
        private List<TValue> _listValues = new();

        #endregion

        #region Properties

        protected List<TKey> ListKeys => _listKeys;
        protected List<TValue> ListValues => _listValues;

        #endregion

        #region  Serialization Callbacks

        public void OnAfterDeserialize()
        {
            Clear();

            for (int i = 0; i < _listKeys.Count && i < _listValues.Count; i++)
            {
                this[_listKeys[i]] = _listValues[i];
            }
        }

        public void OnBeforeSerialize()
        {
            _listKeys.Clear();
            _listValues.Clear();

            foreach (var item in this)
            {
                _listKeys.Add(item.Key);
                _listValues.Add(item.Value);
            }
        }

        #endregion
    }
}