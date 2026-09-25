using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    public class FragmentPlacementTests
    {
        static LookPart[] Chain()
        {
            return new[]
            {
                new LookPart
                {
                    id = "Base", shape = ShapeProfile.Segment(), pivot = ShapeAnchor.Bottom,
                    position = new Vector3(0.2f, 0.1f, 0f), size = new Vector3(0.3f, 0.8f, 0.3f),
                    euler = new Vector3(0f, 0f, -25f)
                },
                new LookPart
                {
                    id = "Blade", shape = ShapeProfile.Leaf(), pivot = ShapeAnchor.Bottom,
                    attachTo = "Base", attachAt = ShapeAnchor.Top,
                    size = new Vector3(0.55f, 1.3f, 0.24f), euler = new Vector3(0f, 40f, -15f)
                },
                new LookPart
                {
                    id = "Accent", shape = ShapeProfile.Bulb(), pivot = ShapeAnchor.Bottom,
                    attachTo = "Blade", attachAt = ShapeAnchor.Top,
                    position = new Vector3(0f, -0.05f, 0f), size = Vector3.one * 0.15f
                }
            };
        }

        static Vector3 Anchor(LookPart part, ShapeAnchor anchor)
        {
            return part.position + Quaternion.Euler(part.euler)
                * Vector3.Scale(part.size, ProceduralShapeMeshes.Anchor(part.shape, anchor));
        }

        static void Same(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), 0.00001f);
        }

        [TestCase(0.15f, 0.5f, -0.2f)]
        [TestCase(0.85f, 1.6f, 0.65f)]
        public void Resolve_ProfileEdits_PreserveBasalContactAndMoveTheDependentAccent(float bend, float fullness,
            float taper)
        {
            LookPart[] fragment = Chain();
            Assert.IsTrue(FragmentPlacement.TryResolve(fragment, CountBand.One, 17, 0, out LookPart[] before,
                out string initialError), initialError);
            ShapeProfile changed = fragment[1].shape;
            changed.bend = bend;
            changed.fullness = fullness;
            changed.taper = taper;
            fragment[1].shape = changed;

            Assert.IsTrue(FragmentPlacement.TryResolve(fragment, CountBand.One, 17, 0, out LookPart[] after,
                out string error), error);

            Same(Anchor(before[1], ShapeAnchor.Bottom), Anchor(after[1], ShapeAnchor.Bottom));
            Same(Anchor(after[0], ShapeAnchor.Top), Anchor(after[1], ShapeAnchor.Bottom));
            Same(Anchor(after[1], ShapeAnchor.Top) + fragment[2].position, Anchor(after[2], ShapeAnchor.Bottom));
            Assert.Greater(Vector3.Distance(Anchor(before[2], ShapeAnchor.Bottom), Anchor(after[2], ShapeAnchor.Bottom)),
                0.001f);
        }

        [Test]
        public void Resolve_DefaultMetadata_KeepsLegacyPartCentresUnchanged()
        {
            LookPart[] fragment = Chain();
            for (int i = 0; i < fragment.Length; i++)
            {
                fragment[i].shape = default;
                fragment[i].pivot = default;
                fragment[i].attachTo = null;
                fragment[i].attachAt = default;
            }

            Assert.IsTrue(FragmentPlacement.TryResolve(fragment, CountBand.One, 17, 4, out LookPart[] placed,
                out string error), error);

            for (int i = 0; i < fragment.Length; i++)
            {
                Assert.AreEqual(fragment[i].position, placed[i].position);
                Assert.AreEqual(fragment[i].size, placed[i].size);
                Assert.AreEqual(fragment[i].euler, placed[i].euler);
            }
        }

        [TestCase("missing")]
        [TestCase("forward")]
        [TestCase("ambiguous")]
        [TestCase("inactive")]
        public void Resolve_InvalidReference_RejectsWithoutPartialPlacement(string failure)
        {
            LookPart[] fragment = Chain();
            if (failure == "missing") fragment[1].attachTo = "Absent";
            if (failure == "forward") fragment[0].attachTo = "Blade";
            if (failure == "ambiguous") fragment[2].id = "Base";
            if (failure == "inactive") fragment[0].minCount = CountBand.Few;

            Assert.IsFalse(FragmentPlacement.TryResolve(fragment, CountBand.One, 17, 0, out LookPart[] placed,
                out string error));

            Assert.IsNull(placed);
            StringAssert.Contains("earlier unique active part", error);
        }

        [Test]
        public void Resolve_ConditionalChain_ActivatesTogetherAtTheCountBand()
        {
            LookPart[] fragment = Chain();
            fragment[1].minCount = CountBand.Few;
            fragment[2].minCount = CountBand.Few;

            Assert.IsTrue(FragmentPlacement.TryResolve(fragment, CountBand.One, 17, 0, out LookPart[] one,
                out string oneError), oneError);
            Assert.IsTrue(FragmentPlacement.TryResolve(fragment, CountBand.Few, 17, 0, out LookPart[] few,
                out string fewError), fewError);

            Assert.AreEqual(1, one.Length);
            Assert.AreEqual(3, few.Length);
            Same(Anchor(few[1], ShapeAnchor.Top) + fragment[2].position, Anchor(few[2], ShapeAnchor.Bottom));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Resolve_InvalidAnchor_RejectsBeforeQueryingTheGenerator(bool ownPivot)
        {
            LookPart[] fragment = Chain();
            if (ownPivot) fragment[1].pivot = (ShapeAnchor)999;
            else fragment[1].attachAt = (ShapeAnchor)999;

            Assert.IsFalse(FragmentPlacement.TryResolve(fragment, CountBand.One, 17, 0, out _, out string error));

            StringAssert.Contains("invalid name, anchor or shape profile", error);
        }

        [Test]
        public void Extent_RotatedProceduralSlab_EnclosesItsBoxCorners()
        {
            Vector3 half = new Vector3(0.8f, 0.25f, 0.4f);
            Vector3 direction = Quaternion.Inverse(Quaternion.Euler(15f, 35f, 28f)) * Vector3.right;
            float measured = LookMeasure.Extent(half, direction, ShapeProfile.Block());
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = Vector3.Scale(half, new Vector3(x, y, z));
                Assert.LessOrEqual(Mathf.Abs(Vector3.Dot(corner, direction)), measured + 0.000001f);
            }
            Assert.Greater(measured, LookMeasure.Extent(half, direction));
            Assert.AreEqual(LookMeasure.Extent(half, direction), LookMeasure.Extent(half, direction, default));
        }
    }
}
