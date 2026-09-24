using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio.Editor
{
    public class CreatureRosterTests
    {
        CreatureRoster _roster;
        LookVocabulary _vocabulary;
        LookPalette _palette;

        [SetUp]
        public void SetUp()
        {
            _roster = new CreatureRoster();
            _vocabulary = Object.Instantiate(RenderTestAssets.LoadLookVocabulary());
            _palette = Object.Instantiate(_vocabulary.palette);
            _vocabulary.palette = _palette;
        }

        [TearDown]
        public void TearDown()
        {
            _roster.Dispose();
            Object.DestroyImmediate(_vocabulary);
            Object.DestroyImmediate(_palette);
        }

        [TestCase(Entity.EntityType.Player)]
        [TestCase(Entity.EntityType.Computer)]
        public void Reload_AllRealSources_UsesProductionChannelsWithoutChangingAssets(Entity.EntityType side)
        {
            string[] expected = AssetDatabase.FindAssets("t:EntityData", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, System.StringComparer.Ordinal).ToArray();
            _roster.Reload(_vocabulary, side);
            CollectionAssert.AreEqual(expected, _roster.rows.Select(row => row.path));
            Assert.Greater(_roster.rows.Count, 0);
            foreach (CreatureRosterRow row in _roster.rows)
            {
                string before = EditorJsonUtility.ToJson(row.source);
                Assert.AreEqual(LookDerivation.Channels(row.source, side), row.channels);
                Assert.NotNull(row.recipe, row.path);
                Assert.IsFalse(AssetDatabase.Contains(row.recipe));
                Assert.NotNull(row.preview.Sample(row.recipe, 1.25f), row.path);
                Assert.AreEqual(before, EditorJsonUtility.ToJson(row.source));
            }
        }

        [Test]
        public void Rebuild_PaletteEditAndUndo_ChangesTheRecipeAndPreservesSource()
        {
            _roster.Reload(_vocabulary, Entity.EntityType.Player);
            CreatureRosterRow row = _roster.rows[0];
            string source = EditorJsonUtility.ToJson(row.source);
            Color before = row.recipe.parts[0].colour;
            Undo.RecordObject(_palette, "Roster palette test");
            _palette.plantBody = Color.magenta;
            Undo.FlushUndoRecordObjects();
            _roster.Rebuild(_vocabulary, Entity.EntityType.Player);
            Assert.AreEqual(Color.magenta, row.recipe.parts[0].colour);
            Undo.PerformUndo();
            _roster.Rebuild(_vocabulary, Entity.EntityType.Player);
            Assert.AreEqual(before, row.recipe.parts[0].colour);
            Assert.AreEqual(source, EditorJsonUtility.ToJson(row.source));
            Undo.ClearUndo(_palette);
        }

        [Test]
        public void ReloadDispose_Repeatedly_ClosesEveryPrivateScene()
        {
            int scenes = EditorSceneManager.previewSceneCount;
            for (int i = 0; i < 2; i++)
            {
                _roster.Reload(_vocabulary, Entity.EntityType.Player);
                foreach (CreatureRosterRow row in _roster.rows)
                {
                    row.preview.Sample(row.recipe, 0f);
                }
            }
            _roster.Dispose();
            Assert.AreEqual(scenes, EditorSceneManager.previewSceneCount);
        }

        [Test]
        public void Capture_PaletteEdit_ChangesRenderedPixels()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Requires a graphics device for the production preview renderer.");
            }
            _roster.Reload(_vocabulary, Entity.EntityType.Player);
            CreatureRosterRow row = _roster.rows[0];
            Color32[] before = row.Image().GetPixels32();
            _palette.plantBody = Color.magenta;
            _palette.plantStem = Color.red;
            _roster.Rebuild(_vocabulary, Entity.EntityType.Player);
            Color32[] after = row.Image().GetPixels32();
            int changed = 0;
            for (int i = 0; i < before.Length; i++)
            {
                if (Mathf.Abs(before[i].r - after[i].r) + Mathf.Abs(before[i].g - after[i].g)
                    + Mathf.Abs(before[i].b - after[i].b) > 30)
                {
                    changed++;
                }
            }
            Assert.Greater(changed, 100, "A palette edit must reach the actual preview pixels.");
        }
    }
}
