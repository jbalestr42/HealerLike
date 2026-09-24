using System.Collections.Generic;
using UnityEngine;

// The data icons of one view: baked ones come from the catalog, the generated textures are owned here
public class DataIconService
{
    public static readonly string CatalogResourcePath = "UIToolkit/DataIconCatalog";

    readonly Dictionary<string, Texture2D> _generated = new Dictionary<string, Texture2D>();
    DataIconCatalog _catalog;

    public Texture2D GetIcon(object data)
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
        if (_generated.TryGetValue(descriptor.key, out cached) && cached)
        {
            return cached;
        }

        // Keep the references alive while UI elements display them, Clear releases them
        Texture2D texture = ProceduralDataIcon.Create(descriptor);
        _generated[descriptor.key] = texture;
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

    public void Clear()
    {
        foreach (Texture2D texture in _generated.Values)
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

        _generated.Clear();
        _catalog = null;
    }
}
