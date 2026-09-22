using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Zones;
namespace HealerLike.Render.Grass
{
    public class HLGrassLayoutTests
    {
        [Test] public void DeterministicIndependentOfUnityRandomAndSeedChangesLayout()
        {
            var saved = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(987); var before = UnityEngine.Random.state;
                var a = HLGrassLayout.Generate(3, 7, 2, new Vector3(4, 9, -5), 3, 101, 42);
                var b = HLGrassLayout.Generate(3, 7, 2, new Vector3(4, 9, -5), 3, 101, 42);
                CollectionAssert.AreEqual(a, b);
                Assert.AreEqual(before, UnityEngine.Random.state);
                Assert.AreNotEqual(a[0], HLGrassLayout.Generate(3, 7, 2, new Vector3(4, 9, -5), 3, 101, 43)[0]);
                foreach (var blade in a) { Assert.That(blade.positionYaw.x, Is.InRange(1f, 7f)); Assert.That(blade.positionYaw.z, Is.InRange(-12f, 2f)); }
            }
            finally { UnityEngine.Random.state = saved; }
        }
        [Test] public void GoldenFixture()
        {
            var blade = HLGrassLayout.Generate(1, 1, 1, Vector3.zero, 0.5f, 1, 1)[0];
            Assert.That(blade.positionYaw.x, Is.EqualTo(0.24905614852905278f).Within(0.000001f));
            Assert.That(blade.positionYaw.z, Is.EqualTo(0.21287755966186528f).Within(0.000001f));
            Assert.That(blade.heightPhaseWidthRandom.x, Is.EqualTo(0.3229929780960083f).Within(0.000001f));
        }
        [Test] public void MainHasExactCountsAndRanges()
        {
            var a = HLGrassLayout.Generate(16, 16, 1, Vector3.zero, 0.5f);
            Assert.AreEqual(65536, a.Length); var quotas = new int[256];
            foreach (var b in a)
            {
                Assert.That(b.positionYaw.x, Is.GreaterThanOrEqualTo(-8).And.LessThan(8));
                Assert.That(b.positionYaw.z, Is.GreaterThanOrEqualTo(-8).And.LessThan(8));
                Assert.AreEqual(0.505f, b.positionYaw.y);
                Assert.That(b.positionYaw.w, Is.InRange(0, 2 * Mathf.PI));
                Assert.That(b.heightPhaseWidthRandom.x, Is.InRange(0.17f, 0.42f));
                Assert.That(b.heightPhaseWidthRandom.y, Is.InRange(0, 2 * Mathf.PI));
                Assert.That(b.heightPhaseWidthRandom.z, Is.InRange(0.035f, 0.05f));
                Assert.That(b.heightPhaseWidthRandom.w, Is.InRange(0, 1));
                quotas[Mathf.FloorToInt(b.positionYaw.x + 8) + 16 * Mathf.FloorToInt(b.positionYaw.z + 8)]++;
            }
            foreach (int q in quotas) Assert.AreEqual(256, q);
        }
        [TestCase(32768)] [TestCase(65536)] [TestCase(98304)]
        public void StandardQualityBudgets(int count) => Assert.AreEqual(count, HLGrassLayout.Generate(16, 16, 1, Vector3.zero, 0, count).Length);
        [Test] public void CapAndStableQuotientRemainderAndDensity()
        {
            Assert.AreEqual(98304, HLGrassLayout.Generate(32, 32, 1, Vector3.zero, 0, int.MaxValue).Length);
            var a = HLGrassLayout.Generate(3, 1, 1, Vector3.zero, 0, 8);
            var quotas = new int[3]; foreach (var b in a) quotas[Mathf.FloorToInt(b.positionYaw.x + 1.5f)]++;
            CollectionAssert.AreEqual(new[] { 3, 3, 2 }, quotas);
            var rect = new Rect(2, -3, 4, 7);
            var dense = HLGrassLayout.Generate(rect, 0, 8);
            Assert.AreEqual(224, dense.Length);
            foreach (var b in dense) Assert.IsTrue(rect.Contains(new Vector2(b.positionYaw.x, b.positionYaw.z)));
            Assert.IsEmpty(HLGrassLayout.Generate(1, 1, 1, Vector3.zero, 0, 0));
        }
        [Test] public void ClumpsShareRootsPhaseAndPatchHueWithThreeToSevenBlades()
        {
            var seeds = HLGrassLayout.Generate(8, 8, 1, Vector3.zero, 0);
            int i = 0; float shortest = 1, tallest = 0;
            var sizes = new System.Collections.Generic.HashSet<int>();
            while (i < seeds.Length)
            {
                var first = seeds[i]; int end = i + 1;
                while (end < seeds.Length && (Vector3)seeds[end].positionYaw == (Vector3)first.positionYaw) end++;
                Assert.That(end - i, Is.InRange(3, 7)); sizes.Add(end - i);
                for (int j = i; j < end; j++)
                {
                    Assert.AreEqual(first.heightPhaseWidthRandom.y, seeds[j].heightPhaseWidthRandom.y);
                    Assert.AreEqual(first.heightPhaseWidthRandom.w, seeds[j].heightPhaseWidthRandom.w);
                    shortest = Mathf.Min(shortest, seeds[j].heightPhaseWidthRandom.x);
                    tallest = Mathf.Max(tallest, seeds[j].heightPhaseWidthRandom.x);
                }
                i = end;
            }
            Assert.AreEqual(5, sizes.Count); Assert.Less(shortest, 0.19f); Assert.Greater(tallest, 0.40f);
            // The first 2x2 cells always belong to one patch, regardless of the seeded 2..4 scale.
            Assert.AreEqual(seeds[0].heightPhaseWidthRandom.w, seeds[1024].heightPhaseWidthRandom.w);
            Assert.AreEqual(seeds[0].heightPhaseWidthRandom.w, seeds[8192].heightPhaseWidthRandom.w);
        }
        [Test] public void RejectsInvalidInputs()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HLGrassLayout.Generate(0, 1, 1, Vector3.zero, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => HLGrassLayout.Generate(1, 1, float.NaN, Vector3.zero, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => HLGrassLayout.Generate(1, 1, 1, Vector3.zero, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => HLGrassLayout.Generate(1, 1, 1, Vector3.zero, float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => HLGrassLayout.Generate(1, 1, 1, new Vector3(float.NaN, 0, 0), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => HLGrassLayout.Generate(new Rect(0, 0, 1, 1), 0, -1));
        }
        [Test] public void BufferStridesAndZoneOffsetsMatchFrozenAbi()
        {
            Assert.AreEqual(HLZone.Stride, Marshal.SizeOf<HLZone>());
            Assert.AreEqual(32, HLZone.Stride);
            Assert.AreEqual(HLBladeSeed.Stride, Marshal.SizeOf<HLBladeSeed>());
            Assert.AreEqual(HLBladeState.Stride, Marshal.SizeOf<HLBladeState>());
            string[] names = { "position", "radius", "kind", "strength", "age", "reserved" };
            int[] offsets = { 0, 12, 16, 20, 24, 28 };
            for (int i = 0; i < names.Length; i++) Assert.AreEqual(offsets[i], Marshal.OffsetOf<HLZone>(names[i]).ToInt32());
        }
    }
}
