using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oisif.Inspector
{
    // A dictionary Unity saves, as a list of key and value entries the inspector edits like any list. The entries stay
    // as they are while they still match the dictionary, so an entry being added in the inspector (e.g. a key already
    // used) is not lost before it is changed; an entry whose key is already used is left out of the dictionary
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [Serializable]
        public struct Entry
        {
            public TKey key;
            public TValue value;

            public Entry(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }

        [SerializeField] List<Entry> _entries = new List<Entry>();
        public IReadOnlyList<Entry> entries => _entries;

        public SerializableDictionary() {}

        public SerializableDictionary(IDictionary<TKey, TValue> source) : base(source) {}

        // Whether the dictionary holds exactly the first entry of each key
        bool MatchesEntries()
        {
            int keyCount = 0;
            HashSet<TKey> seen = new HashSet<TKey>(Comparer);
            foreach (Entry entry in _entries)
            {
                if (entry.key == null || !seen.Add(entry.key))
                {
                    continue;
                }

                keyCount++;
                if (!TryGetValue(entry.key, out TValue value) || !EqualityComparer<TValue>.Default.Equals(value, entry.value))
                {
                    return false;
                }
            }

            return keyCount == Count;
        }

        #region ISerializationCallbackReceiver
        public void OnBeforeSerialize()
        {
            if (MatchesEntries())
            {
                return;
            }

            _entries.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                _entries.Add(new Entry(pair.Key, pair.Value));
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            foreach (Entry entry in _entries)
            {
                if (entry.key != null && !ContainsKey(entry.key))
                {
                    Add(entry.key, entry.value);
                }
            }
        }
        #endregion
    }
}
