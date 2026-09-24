using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Custom/Data/UI/DataIconCatalog")]
public class DataIconCatalog : ScriptableObject
{
    public List<DataIconCatalogEntry> entries = new List<DataIconCatalogEntry>();

    // Runtime skill and item wrappers share their nested data descriptor with the factory asset
    public Texture2D FindDescriptor(DataIconDescriptor descriptor)
    {
        foreach (DataIconCatalogEntry entry in entries)
        {
            if (entry != null && entry.source && DataIconDescriptor.From(entry.source).key == descriptor.key)
            {
                return GetTexture(entry);
            }
        }

        return null;
    }

    public Texture2D Find(Object source)
    {
        if (!source)
        {
            return null;
        }

        foreach (DataIconCatalogEntry entry in entries)
        {
            if (entry != null && entry.source == source)
            {
                return GetTexture(entry);
            }
        }

        return null;
    }

    static Texture2D GetTexture(DataIconCatalogEntry entry)
    {
        return entry.artworkOverride ? entry.artworkOverride : entry.generated;
    }
}
