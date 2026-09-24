using System.Collections.Generic;
using UnityEngine;

namespace HealerLike.UI.Toolkit.Icons
{
    /// <summary>Shared runtime entry point; generated images are owned by this service, not callers.</summary>
    public static class DataIconService
    {
        public const string CatalogResourcePath = "UIToolkit/DataIconCatalog";
        static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        static DataIconCatalog catalog;
        static bool loaded;

        public static Texture2D GetIcon(object data)
        {
            if (!loaded) { catalog = Resources.Load<DataIconCatalog>(CatalogResourcePath); loaded = true; }
            if (catalog && data is UnityEngine.Object asset)
            {
                var baked = catalog.Find(asset);
                if (baked) return baked;
            }
            var descriptor = DataIconDescriptor.From(data);
            if (catalog)
            {
                var baked = catalog.FindDescriptor(descriptor);
                if (baked) return baked;
            }
            if (Cache.TryGetValue(descriptor.Key, out var cached) && cached) return cached;
            // Keep references alive while UI elements display them. Call Clear on UI teardown.
            var texture = ProceduralDataIcon.Create(descriptor);
            Cache[descriptor.Key] = texture;
            return texture;
        }

        public static Sprite TryGetAuthoredSprite(object data)
        {
            var nested = DataIconDescriptor.ReadField(data, "data") ?? data;
            return DataIconDescriptor.ReadField(nested, "icon") as Sprite;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            foreach (var texture in Cache.Values)
            {
                if (!texture) continue;
                if (Application.isPlaying) Object.Destroy(texture);
                else Object.DestroyImmediate(texture);
            }
            Cache.Clear();
            catalog = null;
            loaded = false;
        }
    }
}
