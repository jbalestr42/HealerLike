using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneTerrainClumpTests
    {
        [Test]
        public void OchreIsOneFaceAndRingIsPublic()
        {
            GameObject go = new GameObject("HLTerrain");
            HLStoneTerrainClump clump = go.AddComponent<HLStoneTerrainClump>();
            try
            {
                clump.Initialize(5, 1f);
                Assert.Greater(clump.bareGroundRadius, clump.assembly.localBounds.extents.x);
                Transform facet = clump.assembly.parts[0].transform.Find("HLOchreFace");
                Assert.IsNotNull(facet);
                Assert.AreEqual(3, facet.GetComponent<MeshFilter>().sharedMesh.vertexCount);

                clump.Initialize(6, 1f);
                Assert.IsNull(clump.assembly.parts[0].transform.Find("HLOchreFace"));
            }
            finally
            {
                TestHelpers.InvokePrivate(clump, "OnDestroy");
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DisableHidesOwnedPartsAndFootprintsAndEnableRestoresThem()
        {
            GameObject go = new GameObject("HLTerrain");
            HLStoneTerrainClump clump = go.AddComponent<HLStoneTerrainClump>();
            try
            {
                clump.Initialize(5, 1f);
                clump.enabled = false;
                TestHelpers.InvokePrivate(clump, "OnDisable");
                foreach (HLStoneAssembly.Part part in clump.assembly.parts)
                {
                    Assert.IsFalse(part.transform.gameObject.activeSelf);
                }
                Assert.IsFalse(go.GetComponent<HLStoneGroundShadow>().enabled);
                Assert.IsFalse(go.GetComponent<HLStoneGroundRing>().enabled);

                clump.enabled = true;
                TestHelpers.InvokePrivate(clump, "OnEnable");
                foreach (HLStoneAssembly.Part part in clump.assembly.parts)
                {
                    Assert.IsTrue(part.transform.gameObject.activeSelf);
                }
            }
            finally
            {
                TestHelpers.InvokePrivate(clump, "OnDestroy");
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BoundsAndCellOrderDeterminism()
        {
            GameObject a = new GameObject("HLA");
            GameObject b = new GameObject("HLB");
            HLStoneTerrainClump clumpA = a.AddComponent<HLStoneTerrainClump>();
            HLStoneTerrainClump clumpB = b.AddComponent<HLStoneTerrainClump>();
            try
            {
                for (int i = 0; i < 32; i++)
                {
                    uint seed = HLStoneSeed.ForCell(-12, new Vector2Int(i, -i));
                    clumpA.Initialize(seed, 2f);
                    clumpB.Initialize(seed + 1, 2f);
                    clumpB.Initialize(seed, 2f);
                    Assert.That(clumpA.assembly.parts.Count, Is.InRange(3, 5));
                    Assert.AreEqual(clumpA.assembly.parts.Count, clumpB.assembly.parts.Count);

                    Bounds bounds = clumpA.assembly.localBounds;
                    Assert.That(bounds.size.x, Is.LessThanOrEqualTo(1.92001f));
                    Assert.That(bounds.size.z, Is.LessThanOrEqualTo(1.92001f));
                    Assert.That(bounds.min.y, Is.EqualTo(0).Within(1e-5));
                    Assert.That(bounds.max.y, Is.InRange(1.39999f, 2.40001f));
                    for (int j = 0; j < clumpA.assembly.parts.Count; j++)
                    {
                        HLStoneAssembly.Part partA = clumpA.assembly.parts[j];
                        HLStoneAssembly.Part partB = clumpB.assembly.parts[j];
                        CollectionAssert.AreEqual(partA.lease.data.vertices, partB.lease.data.vertices);
                        Assert.AreEqual(partA.transform.localPosition, partB.transform.localPosition);
                    }
                }
                Assert.Throws<System.ArgumentOutOfRangeException>(() => clumpA.Initialize(1, float.NaN));
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
