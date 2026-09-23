using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Stones
{
    public class StoneTerrainClumpTests
    {
        static StoneTerrainClump CreateClump()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/HLStoneBlock.prefab");
            return Object.Instantiate(prefab).GetComponentInChildren<StoneTerrainClump>();
        }

        static void DestroyClump(StoneTerrainClump clump)
        {
            TestHelpers.InvokePrivate(clump, "OnDestroy");
            Object.DestroyImmediate(clump.transform.root.gameObject);
        }

        [Test]
        public void OchreIsOneFaceAndRingIsPublic()
        {
            StoneTerrainClump clump = CreateClump();
            try
            {
                clump.Init(5, 1f, null, null);
                Assert.Greater(clump.bareGroundRadius, clump.assembly.localBounds.extents.x);
                Transform facet = clump.assembly.parts[0].transform.Find("HLOchreFace");
                Assert.IsNotNull(facet);
                Assert.IsTrue(facet.gameObject.activeSelf);
                Assert.AreEqual(3, facet.GetComponent<MeshFilter>().sharedMesh.vertexCount);

                clump.Init(6, 1f, null, null);
                Assert.IsNull(clump.assembly.parts[0].transform.Find("HLOchreFace"));
                Assert.IsFalse(clump.transform.Find("HLOchreFace").gameObject.activeSelf);
            }
            finally
            {
                DestroyClump(clump);
            }
        }

        [Test]
        public void DisableHidesOwnedPartsAndFootprintsAndEnableRestoresThem()
        {
            StoneTerrainClump clump = CreateClump();
            try
            {
                clump.Init(5, 1f, null, null);
                clump.enabled = false;
                TestHelpers.InvokePrivate(clump, "OnDisable");
                foreach (StoneAssembly.Part part in clump.assembly.parts)
                {
                    Assert.IsFalse(part.transform.gameObject.activeSelf);
                }
                Assert.IsFalse(clump.transform.Find("GroundShadow").gameObject.activeSelf);
                Assert.IsFalse(clump.transform.Find("GroundDisc").gameObject.activeSelf);

                clump.enabled = true;
                TestHelpers.InvokePrivate(clump, "OnEnable");
                foreach (StoneAssembly.Part part in clump.assembly.parts)
                {
                    Assert.IsTrue(part.transform.gameObject.activeSelf);
                }
                Assert.IsTrue(clump.transform.Find("GroundShadow").gameObject.activeSelf);
                Assert.IsTrue(clump.transform.Find("GroundDisc").gameObject.activeSelf);
            }
            finally
            {
                DestroyClump(clump);
            }
        }

        [Test]
        public void SharesStoneMeshesWithTheEffectsOwner()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/StoneEffects.prefab");
            StoneEffects effects = Object.Instantiate(prefab).GetComponent<StoneEffects>();
            StoneTerrainClump clump = CreateClump();
            try
            {
                clump.Init(5, 1f, effects, null);
                Assert.AreEqual(clump.assembly.parts.Count, effects.stoneMeshes.count);
            }
            finally
            {
                DestroyClump(clump);
                TestHelpers.InvokePrivate(effects, "OnDestroy");
                Object.DestroyImmediate(effects.gameObject);
            }
        }

        [Test]
        public void BoundsAndCellOrderDeterminism()
        {
            GameObject a = new GameObject("HLA");
            GameObject b = new GameObject("HLB");
            StoneTerrainClump clumpA = a.AddComponent<StoneTerrainClump>();
            StoneTerrainClump clumpB = b.AddComponent<StoneTerrainClump>();
            try
            {
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
                LogAssert.Expect(LogType.Error, new Regex(@"\[HLStoneTerrainClump\] Cell size"));
                clumpA.Init(1, float.NaN, null, null);
                Assert.AreEqual(count, clumpA.assembly.parts.Count); // the bad call left the clump as it was
            }
            finally
            {
                TestHelpers.InvokePrivate(clumpA, "OnDestroy");
                TestHelpers.InvokePrivate(clumpB, "OnDestroy");
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }
    }
}
