using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace HealerLike.Render.Environment
{

public class EnvironmentRidgeTests
{
    static readonly Vector3 eye = new Vector3(0f, 43.224f, -9.837f);
    static readonly Rect grid = new Rect(-8f, -8f, 16f, 16f);
    static readonly float fogStart = 43.837f;
    static readonly float fogEnd = 50.356f;
    static readonly float ground = 0.5f;
    static readonly int bands = 6;

    GameObject _go;
    GameObject _cameraGo;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("RidgeTest");
        _cameraGo = new GameObject("RidgeCamera");
    }

    [TearDown]
    public void TearDown()
    {
        // Stones are generated per seed; the baked primitive meshes are assets and stay
        foreach (MeshFilter filter in _go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh != null && !EditorUtility.IsPersistent(filter.sharedMesh))
            {
                Object.DestroyImmediate(filter.sharedMesh);
            }
        }

        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_cameraGo);
    }

    [Test]
    public void LastBand_SixBands_IsFinalSixthOfFog()
    {
        Vector2 band = EnvironmentRidge.LastBand(fogStart, fogEnd, bands);

        Assert.AreEqual(49.27f, band.x, 0.01f);
        Assert.AreEqual(fogEnd, band.y, 0.0001f);
        Assert.AreEqual(new Vector2(0f, 10f), EnvironmentRidge.LastBand(0f, 10f, 1));
    }

    [Test]
    public void Layout_SameSeed_RepeatsAndOtherSeedDiffers()
    {
        List<RidgeItem> a = EnvironmentRidge.Layout(eye, fogStart, fogEnd, bands, grid, ground, 8);
        List<RidgeItem> b = EnvironmentRidge.Layout(eye, fogStart, fogEnd, bands, grid, ground, 8);
        List<RidgeItem> c = EnvironmentRidge.Layout(eye, fogStart, fogEnd, bands, grid, ground, 9);

        Assert.AreEqual(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.AreEqual(a[i].kind, b[i].kind);
            Assert.AreEqual(a[i].position, b[i].position);
            Assert.AreEqual(a[i].height, b[i].height);
            Assert.AreEqual(a[i].seed, b[i].seed);
            Assert.AreEqual(a[i].yaw, b[i].yaw);
        }
        Assert.IsFalse(a.Select(i => i.position).SequenceEqual(c.Select(i => i.position)));
    }

    [Test]
    public void Layout_AnySeed_PutsEveryMidHeightInLastBandBeyondGrid()
    {
        Vector2 band = EnvironmentRidge.LastBand(fogStart, fogEnd, bands);
        for (int seed = 0; seed < 24; seed++)
        {
            List<RidgeItem> items = EnvironmentRidge.Layout(eye, fogStart, fogEnd, bands, grid, ground, seed);

            Assert.That(items.Count(i => i.kind == RidgeKind.Monolith), Is.InRange(8, 12), "seed " + seed);
            Assert.That(items.Count(i => i.kind == RidgeKind.Mushroom), Is.InRange(8, 12), "seed " + seed);
            foreach (RidgeItem item in items)
            {
                string label = $"seed {seed} {item.kind} at {item.position}";
                float distance = Vector3.Distance(EnvironmentRidge.MidHeight(item), eye);
                Assert.That(distance, Is.GreaterThanOrEqualTo(band.x).And.LessThan(band.y), label);
                Assert.That(item.position.z,
                    Is.GreaterThanOrEqualTo(grid.yMax + EnvironmentRidge.GridClearance), label);
                Assert.That(item.position.x, Is.InRange(-EnvironmentRidge.SpreadX, EnvironmentRidge.SpreadX),
                    label);
                Assert.AreEqual(ground, item.position.y, label);
                if (item.kind == RidgeKind.Monolith)
                {
                    Assert.That(item.height, Is.InRange(6f, 12f), label);
                    Assert.AreEqual(0f, item.capDiameter, label);
                }
                else
                {
                    Assert.That(item.height - 0.3f * item.capThickness, Is.InRange(7f - 0.001f, 14f + 0.001f),
                        label);
                    Assert.That(item.capDiameter, Is.GreaterThan(item.width * 3f), label);
                    Assert.That(item.capThickness, Is.LessThan(item.capDiameter * 0.5f), label);
                }
            }
            List<float> xs = items.Select(i => i.position.x).ToList();
            Assert.That(xs.Min(), Is.LessThan(-10f));
            Assert.That(xs.Max(), Is.GreaterThan(10f));
        }
    }

    [Test]
    public void Layout_BandShortOfGrid_ClampsItemsPastGrid()
    {
        // A camera far behind puts the band short of the grid: every item is clamped to the clearance line
        Vector3 farEye = new Vector3(0f, 10f, -80f);
        foreach (RidgeItem item in EnvironmentRidge.Layout(farEye, fogStart, fogEnd, bands, grid, ground, 1))
        {
            Assert.AreEqual(grid.yMax + EnvironmentRidge.GridClearance, item.position.z, 0.0001f);
        }
    }

    [Test]
    public void Layout_InvalidInput_LogsAndReturnsEmpty()
    {
        Regex rejected = new Regex(@"^\[EnvironmentRidge\] Rejected");
        for (int i = 0; i < 5; i++)
        {
            LogAssert.Expect(LogType.Error, rejected);
        }

        Assert.IsEmpty(EnvironmentRidge.Layout(eye, fogStart, fogEnd, 0, grid, ground, 1));
        Assert.IsEmpty(EnvironmentRidge.Layout(eye, fogEnd, fogStart, bands, grid, ground, 1));
        Assert.IsEmpty(EnvironmentRidge.Layout(eye, fogStart, float.PositiveInfinity, bands, grid, ground, 1));
        Assert.IsEmpty(EnvironmentRidge.Layout(eye, fogStart, fogEnd, bands, new Rect(0f, 0f, 0f, 4f), ground, 1));
        Assert.IsEmpty(EnvironmentRidge.Layout(eye, fogStart, fogEnd, bands, grid, float.NaN, 1));
    }

    [Test]
    public void Build_NoCamera_BuildsNothing()
    {
        EnvironmentRidge ridge = _go.AddComponent<EnvironmentRidge>();

        Assert.DoesNotThrow(() => ridge.Build());

        Assert.IsNull(ridge.root);
    }

    [Test]
    public void Init_Eye_MakesOneShadowlessColliderFreeChildPerItem()
    {
        Camera camera = _cameraGo.AddComponent<Camera>();
        _cameraGo.transform.position = eye;
        EnvironmentRidge ridge = _go.AddComponent<EnvironmentRidge>();
        TestHelpers.SetPrivateField(ridge, "_fogBands", bands);
        TestHelpers.SetPrivateField(ridge, "_seed", 4);

        ridge.Init(camera, grid, ground, fogStart, fogEnd, RenderTestAssets.LoadMeshes());

        Assert.AreEqual(ridge.items.Count, ridge.root.childCount);
        for (int i = 0; i < ridge.items.Count; i++)
        {
            RidgeItem item = ridge.items[i];
            Transform pivot = ridge.root.GetChild(i);
            int expectedParts = item.kind == RidgeKind.Monolith ? 1 : 2;
            Assert.AreEqual(expectedParts, pivot.childCount);
            Bounds bounds = pivot.GetComponentsInChildren<MeshRenderer>()
                .Select(r => r.bounds)
                .Aggregate((x, y) =>
                {
                    x.Encapsulate(y);
                    return x;
                });
            Assert.AreEqual(item.position.y + item.height, bounds.max.y, 0.05f * item.height,
                item.kind.ToString());
            Assert.AreEqual(item.position.y, bounds.min.y, 0.05f * item.height, item.kind.ToString());
        }
        Assert.AreEqual(0, _go.GetComponentsInChildren<Collider>().Length);
        foreach (MeshRenderer meshRenderer in ridge.root.GetComponentsInChildren<MeshRenderer>())
        {
            Assert.AreEqual(ShadowCastingMode.Off, meshRenderer.shadowCastingMode);
            Assert.IsTrue(meshRenderer.HasPropertyBlock());
        }
    }

    [Test]
    public void Init_Camera_BuildsLayoutWithTheGivenMeshes()
    {
        Camera camera = _cameraGo.AddComponent<Camera>();
        _cameraGo.transform.position = eye;
        EnvironmentRidge ridge = _go.AddComponent<EnvironmentRidge>();

        ridge.Init(camera, grid, ground, fogStart, fogEnd, RenderTestAssets.LoadMeshes());

        List<RidgeItem> expected = EnvironmentRidge.Layout(eye, fogStart, fogEnd, bands, grid, ground, 1707);
        Assert.AreEqual(expected.Count, ridge.items.Count);
        Assert.AreEqual(expected.Count, ridge.root.childCount);
        Assert.IsTrue(ridge.root.GetComponentsInChildren<MeshFilter>().Any(f => f.sharedMesh == RenderTestAssets.LoadMeshes().capsule));
    }
}

}
