using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
namespace HealerLike.Render.Environment
{
    public class HLEnvironmentRidgeTests
    {
        static readonly Vector3 Eye = new Vector3(0, 43.224f, -9.837f);
        static readonly Rect Grid = new Rect(-8, -8, 16, 16);
        const float FogStart = 43.837f, FogEnd = 50.356f, Ground = .5f;
        const int Bands = 6;
        GameObject go;
        [SetUp] public void Setup() { go = new GameObject("HLRidgeTest"); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(go); }

        [Test] public void LastBandIsTheFinalSixthOfTheFog()
        {
            var band = HLEnvironmentRidge.LastBand(FogStart, FogEnd, Bands);
            Assert.AreEqual(49.27f, band.x, .01f); Assert.AreEqual(FogEnd, band.y, 1e-4f);
            Assert.AreEqual(new Vector2(0, 10), HLEnvironmentRidge.LastBand(0, 10, 1));
        }
        [Test] public void SameSeedGivesTheSameLayoutAndAnotherSeedDiffers()
        {
            var a = HLEnvironmentRidge.Layout(Eye, FogStart, FogEnd, Bands, Grid, Ground, 8);
            var b = HLEnvironmentRidge.Layout(Eye, FogStart, FogEnd, Bands, Grid, Ground, 8);
            var c = HLEnvironmentRidge.Layout(Eye, FogStart, FogEnd, Bands, Grid, Ground, 9);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Kind, b[i].Kind); Assert.AreEqual(a[i].Position, b[i].Position);
                Assert.AreEqual(a[i].Height, b[i].Height); Assert.AreEqual(a[i].Seed, b[i].Seed); Assert.AreEqual(a[i].Yaw, b[i].Yaw);
            }
            Assert.IsFalse(a.Select(i => i.Position).SequenceEqual(c.Select(i => i.Position)));
        }
        [Test] public void EveryMidHeightSitsInTheLastBandBeyondTheGrid()
        {
            var band = HLEnvironmentRidge.LastBand(FogStart, FogEnd, Bands);
            for (int seed = 0; seed < 24; seed++)
            {
                var items = HLEnvironmentRidge.Layout(Eye, FogStart, FogEnd, Bands, Grid, Ground, seed);
                Assert.That(items.Count(i => i.Kind == HLRidgeKind.Monolith), Is.InRange(8, 12), "seed " + seed);
                Assert.That(items.Count(i => i.Kind == HLRidgeKind.Mushroom), Is.InRange(8, 12), "seed " + seed);
                foreach (var item in items)
                {
                    string label = $"seed {seed} {item.Kind} at {item.Position}";
                    float distance = Vector3.Distance(HLEnvironmentRidge.MidHeight(item), Eye);
                    Assert.That(distance, Is.GreaterThanOrEqualTo(band.x).And.LessThan(band.y), label);
                    Assert.That(item.Position.z, Is.GreaterThanOrEqualTo(Grid.yMax + HLEnvironmentRidge.GridClearance), label);
                    Assert.That(item.Position.x, Is.InRange(-HLEnvironmentRidge.SpreadX, HLEnvironmentRidge.SpreadX), label);
                    Assert.AreEqual(Ground, item.Position.y, label);
                    if (item.Kind == HLRidgeKind.Monolith) { Assert.That(item.Height, Is.InRange(6f, 12f), label); Assert.AreEqual(0f, item.CapDiameter, label); }
                    else
                    {
                        Assert.That(item.Height - .3f * item.CapThickness, Is.InRange(7f - 1e-3f, 14f + 1e-3f), label);
                        Assert.That(item.CapDiameter, Is.GreaterThan(item.Width * 3), label); Assert.That(item.CapThickness, Is.LessThan(item.CapDiameter * .5f), label);
                    }
                }
                var xs = items.Select(i => i.Position.x).ToList();
                Assert.That(xs.Min(), Is.LessThan(-10)); Assert.That(xs.Max(), Is.GreaterThan(10));
            }
        }
        [Test] public void ItemsTheBandCannotReachAreClampedPastTheGrid()
        {
            // A camera far behind puts the band short of the grid: every item is clamped to the clearance line.
            foreach (var item in HLEnvironmentRidge.Layout(new Vector3(0, 10, -80), FogStart, FogEnd, Bands, Grid, Ground, 1))
                Assert.AreEqual(Grid.yMax + HLEnvironmentRidge.GridClearance, item.Position.z, 1e-4f);
        }
        [Test] public void InvalidInputThrows()
        {
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentRidge.Layout(Eye, FogStart, FogEnd, 0, Grid, Ground, 1));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentRidge.Layout(Eye, FogEnd, FogStart, Bands, Grid, Ground, 1));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentRidge.Layout(Eye, FogStart, float.PositiveInfinity, Bands, Grid, Ground, 1));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentRidge.Layout(Eye, FogStart, FogEnd, Bands, new Rect(0, 0, 0, 4), Ground, 1));
            Assert.Catch<System.ArgumentException>(() => HLEnvironmentRidge.Layout(Eye, FogStart, FogEnd, Bands, Grid, float.NaN, 1));
        }
        [Test] public void BuildMakesOneShadowlessColliderFreeChildPerItem()
        {
            var ridge = go.AddComponent<HLEnvironmentRidge>();
            ridge.Configure(null, null, null, Grid, Ground, FogStart, FogEnd, Bands, 4);
            Assert.DoesNotThrow(() => ridge.Build()); Assert.IsNull(ridge.Root);
            ridge.Build(Eye);
            Assert.AreEqual(ridge.Items.Count, ridge.Root.childCount);
            for (int i = 0; i < ridge.Items.Count; i++)
            {
                var item = ridge.Items[i]; var pivot = ridge.Root.GetChild(i);
                Assert.AreEqual(item.Kind == HLRidgeKind.Monolith ? 1 : 2, pivot.childCount);
                var bounds = pivot.GetComponentsInChildren<MeshRenderer>().Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                Assert.AreEqual(item.Position.y + item.Height, bounds.max.y, .05f * item.Height, item.Kind.ToString());
                Assert.AreEqual(item.Position.y, bounds.min.y, .05f * item.Height, item.Kind.ToString());
            }
            Assert.AreEqual(0, go.GetComponentsInChildren<Collider>().Length);
            foreach (var r in ridge.Root.GetComponentsInChildren<MeshRenderer>())
            { Assert.AreEqual(ShadowCastingMode.Off, r.shadowCastingMode); Assert.IsTrue(r.HasPropertyBlock()); }
            ridge.Clear(); Assert.IsNull(ridge.Root); Assert.AreEqual(0, go.transform.childCount);
        }
    }
}
