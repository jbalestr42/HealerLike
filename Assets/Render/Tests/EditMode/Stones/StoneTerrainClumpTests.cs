using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Stones
{

public class StoneTerrainClumpTests
{
    StoneTerrainClump _clump;
    GameObject _block;
    GameObject _bareA;
    GameObject _bareB;
    StoneEffects _effects;

    [SetUp]
    public void SetUp()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/StoneBlock.prefab");
        _block = Object.Instantiate(prefab);
        _clump = _block.GetComponentInChildren<StoneTerrainClump>();
        _bareA = new GameObject("A");
        _bareB = new GameObject("B");
    }

    [TearDown]
    public void TearDown()
    {
        ReleaseClumps(_block);
        ReleaseClumps(_bareA);
        ReleaseClumps(_bareB);
        Object.DestroyImmediate(_block);
        Object.DestroyImmediate(_bareA);
        Object.DestroyImmediate(_bareB);
        if (_effects != null)
        {
            TestHelpers.InvokePrivate(_effects, "OnDestroy");
            Object.DestroyImmediate(_effects.gameObject);
            _effects = null;
        }
    }

    static void ReleaseClumps(GameObject root)
    {
        foreach (StoneTerrainClump clump in root.GetComponentsInChildren<StoneTerrainClump>(true))
        {
            TestHelpers.InvokePrivate(clump, "OnDestroy");
        }
    }

    [Test]
    public void Init_OchreSeed_ShowsOneOchreFaceAndPublishesTheRing()
    {
        _clump.Init(5, 1f, null, null);

        Assert.Greater(_clump.bareGroundRadius, _clump.assembly.localBounds.extents.x);
        Transform facet = _clump.assembly.parts[0].transform.Find("OchreFace");
        Assert.IsNotNull(facet);
        Assert.IsTrue(facet.gameObject.activeSelf);
        Assert.AreEqual(3, facet.GetComponent<MeshFilter>().sharedMesh.vertexCount);

        _clump.Init(6, 1f, null, null);
        Assert.IsNull(_clump.assembly.parts[0].transform.Find("OchreFace"));
        Assert.IsFalse(_clump.transform.Find("OchreFace").gameObject.activeSelf);
    }

    [Test]
    public void OnDisable_BuiltClump_HidesPartsAndFootprintsAndEnableRestoresThem()
    {
        _clump.Init(5, 1f, null, null);

        _clump.enabled = false;
        TestHelpers.InvokePrivate(_clump, "OnDisable");

        foreach (StoneAssembly.Part part in _clump.assembly.parts)
        {
            Assert.IsFalse(part.transform.gameObject.activeSelf);
        }
        Assert.IsFalse(_clump.transform.Find("GroundShadow").gameObject.activeSelf);
        Assert.IsFalse(_clump.transform.Find("GroundDisc").gameObject.activeSelf);

        _clump.enabled = true;
        TestHelpers.InvokePrivate(_clump, "OnEnable");
        foreach (StoneAssembly.Part part in _clump.assembly.parts)
        {
            Assert.IsTrue(part.transform.gameObject.activeSelf);
        }
        Assert.IsTrue(_clump.transform.Find("GroundShadow").gameObject.activeSelf);
        Assert.IsTrue(_clump.transform.Find("GroundDisc").gameObject.activeSelf);
    }

    [Test]
    public void Init_Reinitialised_ReusesItsTwoDiscsAndPropagatesTheShadowToggle()
    {
        _clump.Init(4, 1f, null, null);
        _clump.Init(5, 2f, null, null);

        Assert.AreEqual(2, _block.GetComponentsInChildren<StoneGroundDisc>(true).Length);
        Assert.AreEqual(2, _block.GetComponentsInChildren<StoneGroundDisc>().Length);

        _clump.groundShadowEnabled = false;

        Assert.AreEqual(1, _block.GetComponentsInChildren<StoneGroundDisc>().Length);
        Assert.IsFalse(_block.GetComponentInChildren<StoneGroundDisc>().isShadow);
    }

    [Test]
    public void Init_WithEffects_SharesStoneMeshesWithTheEffectsOwner()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/StoneEffects.prefab");
        _effects = Object.Instantiate(prefab).GetComponent<StoneEffects>();

        _clump.Init(5, 1f, _effects, null);

        Assert.AreEqual(_clump.assembly.parts.Count, _effects.stoneMeshes.count);
    }

    [Test]
    public void Init_SameCellSeed_BuildsTheSameBoundedClumpInAnyOrder()
    {
        StoneTerrainClump clumpA = _bareA.AddComponent<StoneTerrainClump>();
        StoneTerrainClump clumpB = _bareB.AddComponent<StoneTerrainClump>();
        for (int i = 0; i < 32; i++)
        {
            uint seed = StoneSeed.ForCell(-12, new Vector2Int(i, -i));
            clumpA.Init(seed, 2f, null, null);
            clumpB.Init(seed + 1, 2f, null, null);
            clumpB.Init(seed, 2f, null, null);
            Assert.That(clumpA.assembly.parts.Count, Is.InRange(3, 5));
            Assert.AreEqual(clumpA.assembly.parts.Count, clumpB.assembly.parts.Count);

            Bounds bounds = clumpA.assembly.localBounds;
            Assert.That(bounds.size.x, Is.LessThanOrEqualTo(1.92001f));
            Assert.That(bounds.size.z, Is.LessThanOrEqualTo(1.92001f));
            Assert.That(bounds.min.y, Is.EqualTo(0).Within(0.00001f));
            Assert.That(bounds.max.y, Is.InRange(1.39999f, 2.40001f));
            for (int j = 0; j < clumpA.assembly.parts.Count; j++)
            {
                StoneAssembly.Part partA = clumpA.assembly.parts[j];
                StoneAssembly.Part partB = clumpB.assembly.parts[j];
                CollectionAssert.AreEqual(partA.lease.data.vertices, partB.lease.data.vertices);
                Assert.AreEqual(partA.transform.localPosition, partB.transform.localPosition);
            }
        }

        int count = clumpA.assembly.parts.Count;
        LogAssert.Expect(LogType.Error, new Regex(@"\[StoneTerrainClump\] Cell size"));
        clumpA.Init(1, float.NaN, null, null);
        Assert.AreEqual(count, clumpA.assembly.parts.Count); // the bad call left the clump as it was
    }
}

}
