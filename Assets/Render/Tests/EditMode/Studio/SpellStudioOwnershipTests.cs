using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    public class SpellStudioOwnershipTests
    {
        SpellStudioPreset _preset;
        SpellStudioPreview _preview;

        [SetUp]
        public void SetUp()
        {
            _preset = ScriptableObject.CreateInstance<SpellStudioPreset>();
            _preset.vocabulary = RenderTestAssets.LoadEffectVocabulary();
            _preset.overrideEntry = true;
            _preset.entry = new ElementEntry
            {
                parts = new[] { new LookPart { id = "OwnedPreviewShape", primitive = Primitive.Sphere,
                    shape = ShapeProfile.Bulb(), role = PartRole.Body, colour = ColourRole.Accent,
                    size = Vector3.one * .2f } },
                cycleSeconds = 1f
            };
            _preview = new SpellStudioPreview();
            _preview.Init();
        }

        [TearDown]
        public void TearDown()
        {
            _preview.Dispose();
            Object.DestroyImmediate(_preset);
        }

        [Test]
        public void SeekBackAndDispose_ReleaseGeneratedMeshesInDisabledPreview()
        {
            SpellEffect first = _preview.Sample(_preset, .4f);
            Assert.IsNotNull(first);
            Mesh original = first.parts[0].GetComponent<MeshFilter>().sharedMesh;
            SpellEffect next = _preview.Sample(_preset, .1f);
            Assert.IsTrue(original == null, "Rebuilding a disabled preview releases its mesh cache.");
            Mesh replacement = next.parts[0].GetComponent<MeshFilter>().sharedMesh;
            Assert.IsTrue(replacement);
            _preview.Dispose();
            Assert.IsTrue(replacement == null, "Closing the studio releases the final mesh cache.");
            Assert.IsTrue(RenderTestAssets.LoadMeshes().sphere);
        }
    }
}
