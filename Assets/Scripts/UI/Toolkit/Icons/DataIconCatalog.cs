using System;
using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.UI.Toolkit.Icons
{
    [CreateAssetMenu(menuName = "HealerLike/UI Toolkit/Icon Catalog")]
    public sealed class DataIconCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public UnityEngine.Object source;
            public Texture2D generated;
            [Tooltip("Optional artwork takes priority over generated icons.")]
            public Texture2D artworkOverride;
            [HideInInspector] public string fingerprint;
        }
        public List<Entry> entries = new List<Entry>();

        // Runtime skill/item wrappers share their nested data descriptor with the factory asset.
        public Texture2D FindDescriptor(DataIconDescriptor descriptor)
        {
            foreach (var entry in entries)
                if (entry != null && entry.source && DataIconDescriptor.From(entry.source).Key == descriptor.Key)
                    return entry.artworkOverride ? entry.artworkOverride : entry.generated;
            return null;
        }

        public Texture2D Find(UnityEngine.Object source)
        {
            if (!source) return null;
            foreach (var entry in entries)
                if (entry != null && entry.source == source)
                    return entry.artworkOverride ? entry.artworkOverride : entry.generated;
            return null;
        }
    }
}
