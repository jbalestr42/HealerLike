using System.Collections.Generic;
using UnityEngine;

// The data icons of one view: the sprite Julien authored on the data first, then the baked ones from the catalog,
// then a generated texture owned here
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

        Texture2D authored = AuthoredTexture(TryGetAuthoredSprite(data));
        if (authored)
        {
            return authored;
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
        return DataIconSource.AuthoredSprite(DataIconSource.Unwrap(data));
    }

    // The texture of a sprite that is its whole texture. A packed or cropped sprite would show its neighbours, so it
    // gives nothing and the icon falls through to the catalog.
    public static Texture2D AuthoredTexture(Sprite sprite)
    {
        if (!sprite || sprite.packed)
        {
            return null;
        }

        Texture2D texture = sprite.texture;
        if (!texture)
        {
            return null;
        }

        Rect rect = sprite.rect;
        bool isWhole = Mathf.Approximately(rect.x, 0f) && Mathf.Approximately(rect.y, 0f)
            && Mathf.Approximately(rect.width, texture.width) && Mathf.Approximately(rect.height, texture.height);
        return isWhole ? texture : null;
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
