using System.Collections.Generic;
using UnityEngine;

// Shared runtime entry point for data icons, the generated textures are owned here and not by the callers
public static class DataIconService
{
    public static readonly string CatalogResourcePath = "UIToolkit/DataIconCatalog";

    static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
    static DataIconCatalog _catalog;

    public static Texture2D GetIcon(object data)
    {
        if (!_catalog)
        {
            _catalog = Resources.Load<DataIconCatalog>(CatalogResourcePath);
        }

        if (_catalog && data is Object)
        {
            Texture2D baked = _catalog.Find((Object)data);
            if (baked)
            {
                return baked;
            }
        }

        DataIconDescriptor descriptor = DataIconDescriptor.From(data);
        if (_catalog)
        {
            Texture2D baked = _catalog.FindDescriptor(descriptor);
            if (baked)
            {
                return baked;
            }
        }

        Texture2D cached;
        if (cache.TryGetValue(descriptor.key, out cached) && cached)
        {
            return cached;
        }

        // Keep the references alive while UI elements display them, Clear releases them
        Texture2D texture = ProceduralDataIcon.Create(descriptor);
        cache[descriptor.key] = texture;
        return texture;
    }

    public static Sprite TryGetAuthoredSprite(object data)
    {
        object nested = DataIconDescriptor.ReadField(data, "data");
        if (nested == null)
        {
            nested = data;
        }

        return DataIconDescriptor.ReadField(nested, "icon") as Sprite;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear()
    {
        foreach (Texture2D texture in cache.Values)
        {
            if (!texture)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(texture);
            }
            else
            {
                Object.DestroyImmediate(texture);
            }
        }

        cache.Clear();
        _catalog = null;
    }
}
