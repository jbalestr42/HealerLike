using NUnit.Framework;
using UnityEngine;
namespace HealerLike.UI.Toolkit.Icons
{
    public class DataIconCatalogTests
    {
        [Test] public void RuntimeDataResolvesFactoryOverride()
        {
            var catalog = ScriptableObject.CreateInstance<DataIconCatalog>();
            var factory = ScriptableObject.CreateInstance<ItemFactory>();
            var artwork = new Texture2D(16, 16);
            try
            {
                factory.data = new ItemData { name = "Ward" };
                catalog.entries.Add(new DataIconCatalogEntry { source = factory, artworkOverride = artwork });
                Assert.That(catalog.FindDescriptor(DataIconDescriptor.From(factory.GetItem())), Is.SameAs(artwork));
            }
            finally
            {
                Object.DestroyImmediate(catalog); Object.DestroyImmediate(factory); Object.DestroyImmediate(artwork);
            }
        }
        [Test] public void OverrideWinsAndMissingSourceReturnsNull()
        {
            var catalog = ScriptableObject.CreateInstance<DataIconCatalog>();
            var source = ScriptableObject.CreateInstance<EntityData>();
            var generated = new Texture2D(16, 16);
            var artwork = new Texture2D(16, 16);
            try
            {
                var entry = new DataIconCatalogEntry { source = source, generated = generated };
                catalog.entries.Add(entry);
                Assert.That(catalog.Find(source), Is.SameAs(generated));
                entry.artworkOverride = artwork;
                Assert.That(catalog.Find(source), Is.SameAs(artwork));
                Assert.That(catalog.Find(null), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(catalog); Object.DestroyImmediate(source);
                Object.DestroyImmediate(generated); Object.DestroyImmediate(artwork);
            }
        }
    }
}
