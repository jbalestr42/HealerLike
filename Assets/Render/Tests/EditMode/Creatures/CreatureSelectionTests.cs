using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureSelectionTests : CreatureMeshOwnershipFixture
    {
        [TestCase(false)]
        [TestCase(true)]
        public void HighlightThenClear_RestoresBothAuthoredSubmeshColoursAndOutlineWidths(bool tip)
        {
            Renderer renderer = _rig.partTransforms[0].GetComponent<Renderer>();
            Material[] materials = renderer.sharedMaterials;
            PartPaint paint = new PartPaint();
            Color body = new Color(0.2f, 0.5f, 0.1f, 0.7f);
            Color ochre = new Color(0.6f, 0.4f, 0.2f, 0.8f);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            Color[] baseline = new Color[2];
            paint.Paint(renderer, tip, body, ochre, 0f);
            for (int index = 0; index < baseline.Length; index++)
            {
                renderer.GetPropertyBlock(block, index);
                baseline[index] = block.GetColor("_BaseColor");
                AssertNativeColour(index == 0 ? body : ochre, baseline[index]);
            }

            paint.selection = new CreatureSelection(true, Color.cyan, 4f);
            paint.Paint(renderer, tip, body, ochre, 0f);
            for (int index = 0; index < 2; index++)
            {
                Color authored = index == 0 ? body : ochre;
                renderer.GetPropertyBlock(block, index);
                Color expected = Color.Lerp(authored, Color.cyan, 0.3f);
                expected.a = authored.a;
                AssertNativeColour(expected, block.GetColor("_BaseColor"));
                Assert.AreEqual(4f, block.GetFloat("_HLOutlineWidthMultiplier"));
            }

            paint.selection = default;
            paint.Paint(renderer, tip, body, ochre, 0f);
            for (int index = 0; index < 2; index++)
            {
                renderer.GetPropertyBlock(block, index);
                Assert.AreEqual(baseline[index], block.GetColor("_BaseColor"));
                bool hasTipWidth = tip && index == 0;
                Assert.AreEqual(hasTipWidth, block.HasFloat("_HLOutlineWidthMultiplier"));
                if (hasTipWidth)
                {
                    Assert.AreEqual(PartPaint.TipOutlineWidth, block.GetFloat("_HLOutlineWidthMultiplier"));
                }
            }
            CollectionAssert.AreEqual(materials, renderer.sharedMaterials);
        }

        // Native color storage can round RGB. Bound that comparison in float ULPs; alpha and restoration stay exact.
        static void AssertNativeColour(Color expected, Color actual)
        {
            for (int channel = 0; channel < 3; channel++)
            {
                Assert.That(actual[channel], Is.EqualTo(expected[channel]).Within(4).Ulps, "RGB channel " + channel);
            }
            Assert.AreEqual(expected.a, actual.a);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Highlight_PreservesTheAuthoredMaterialWidthWhenItIsWiderThanSelection(bool indexed)
        {
            Renderer renderer = _rig.partTransforms[0].GetComponent<Renderer>();
            Material second = new Material(_material);
            try
            {
                _material.SetFloat("_HLOutlineWidthMultiplier", 6f);
                second.SetFloat("_HLOutlineWidthMultiplier", 8f);
                renderer.sharedMaterials = new[] { _material, second };
                PartPaint paint = new PartPaint();
                paint.Clear(renderer);
                paint.selection = new CreatureSelection(true, Color.white, 4f);
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                if (indexed)
                {
                    paint.Paint(renderer, false, Color.red, Color.yellow, 0f);
                    renderer.GetPropertyBlock(block, 1);
                    Assert.AreEqual(8f, block.GetFloat("_HLOutlineWidthMultiplier"));
                    renderer.GetPropertyBlock(block, 0);
                }
                else
                {
                    paint.Paint(renderer, false, Color.red, 0f);
                    renderer.GetPropertyBlock(block);
                }
                Assert.AreEqual(6f, block.GetFloat("_HLOutlineWidthMultiplier"));
                Assert.AreEqual(6f, _material.GetFloat("_HLOutlineWidthMultiplier"));
                Assert.AreEqual(8f, second.GetFloat("_HLOutlineWidthMultiplier"));
            }
            finally
            {
                Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void Selection_PreservesTipWidthAndRejectsInvalidPresentationInputs()
        {
            CreatureSelection narrow = new CreatureSelection(true, Color.white, 0.5f);
            Assert.AreEqual(PartPaint.TipOutlineWidth, narrow.Width(PartPaint.TipOutlineWidth));
            CreatureSelection invalid = new CreatureSelection(true, Color.white, float.NaN);
            Assert.IsFalse(invalid.isHighlighted);
            Assert.AreEqual(Color.red, invalid.Tint(Color.red));
            Assert.IsFalse(CreatureSelection.Read(null).isHighlighted);
        }

        [Test]
        public void RecomposeWhileSelected_KeepsTheSelectionAndClearRestoresTheAcceptedPalette()
        {
            Tick(0f);
            Renderer renderer = _rig.partTransforms[0].GetComponent<Renderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, 0);
            Color before = block.GetColor("_BaseColor");
            _rig.SetSelection(new CreatureSelection(true, Color.white, 4f));
            Tick(0f);
            renderer.GetPropertyBlock(block, 0);
            Assert.AreNotEqual(before, block.GetColor("_BaseColor"));
            Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, _meshes));
            Tick(0f);
            renderer.GetPropertyBlock(block, 0);
            Assert.AreEqual(4f, block.GetFloat("_HLOutlineWidthMultiplier"));
            _rig.SetSelection(default);
            Tick(0f);
            renderer.GetPropertyBlock(block, 0);
            Assert.AreEqual(before, block.GetColor("_BaseColor"));
            Assert.IsFalse(block.HasFloat("_HLOutlineWidthMultiplier"));
        }
    }
}
