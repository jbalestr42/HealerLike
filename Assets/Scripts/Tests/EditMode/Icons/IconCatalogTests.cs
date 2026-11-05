using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Icons
{
    public class IconCatalogTests
    {
        static IEnumerable<IconCatalog.Entry> Entries => IconCatalog.entries;

        [Test]
        public void EveryEntry_UsesAKnownGlyph()
        {
            foreach (IconCatalog.Entry entry in Entries)
            {
                Assert.IsTrue(IconGlyphs.all.ContainsKey(entry.glyph), $"{entry.iconPath}: no glyph '{entry.glyph}'");
            }
        }

        [Test]
        public void EveryEntry_PointsAtExistingData()
        {
            foreach (IconCatalog.Entry entry in Entries)
            {
                Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(entry.assetPath), $"No data at {entry.assetPath}");
            }
        }

        [Test]
        public void NoTwoEntries_ShareAnIconOrAData()
        {
            Assert.AreEqual(Entries.Count(), Entries.Select(entry => entry.iconPath).Distinct().Count());
            Assert.AreEqual(Entries.Count(), Entries.Select(entry => entry.assetPath).Distinct().Count());
        }

        [Test]
        public void EveryGlyph_DrawsOverItsFrame()
        {
            foreach (KeyValuePair<string, System.Action<IconPen>> glyph in IconGlyphs.all)
            {
                var bare = new IconCanvas(64);
                IconFrames.Disc(bare, IconColor.Hex("#3E6A8C"));
                var drawn = new IconCanvas(64);
                glyph.Value(IconFrames.Disc(drawn, IconColor.Hex("#3E6A8C")));

                CollectionAssert.AreNotEqual(bare.ToRgba32(false), drawn.ToRgba32(false), $"Glyph '{glyph.Key}' draws nothing");
            }
        }

        [Test]
        public void Disc_IsRound_TransparentInItsCorners()
        {
            var canvas = new IconCanvas(64);

            IconFrames.Disc(canvas, IconColor.Hex("#3E6A8C"));

            Assert.AreEqual(0f, canvas.GetPixel(0, 0).a);
            Assert.AreEqual(1f, canvas.GetPixel(32, 32).a, 1e-4f);
        }
    }
}
