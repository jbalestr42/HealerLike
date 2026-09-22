using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneTerrainClumpTests
    {
        [Test] public void OchreIsOneFaceAndRingIsPublic()
        {
            var go=new GameObject("HLTerrain"); var clump=go.AddComponent<HLStoneTerrainClump>();
            try
            {
                clump.Initialize(5,1); Assert.Greater(clump.BareGroundRadius,clump.Assembly.LocalBounds.extents.x);
                var facet=clump.Assembly.Parts[0].Transform.Find("HLOchreFace"); Assert.IsNotNull(facet);
                Assert.AreEqual(3,facet.GetComponent<MeshFilter>().sharedMesh.vertexCount);
                clump.Initialize(6,1); Assert.IsNull(clump.Assembly.Parts[0].Transform.Find("HLOchreFace"));
            }
            finally { TestHelpers.InvokePrivate(clump,"OnDestroy"); Object.DestroyImmediate(go); }
        }
        [Test] public void BoundsAndCellOrderDeterminism()
        {
            var a=new GameObject("HLA");var b=new GameObject("HLB");var ca=a.AddComponent<HLStoneTerrainClump>();var cb=b.AddComponent<HLStoneTerrainClump>();
            try {
                for(int i=0;i<32;i++)
                {
                    uint seed=HLStoneSeed.ForCell(-12,new Vector2Int(i,-i));ca.Initialize(seed,2); cb.Initialize(seed+1,2);cb.Initialize(seed,2);
                    Assert.That(ca.Assembly.Parts.Count,Is.InRange(3,5));Assert.AreEqual(ca.Assembly.Parts.Count,cb.Assembly.Parts.Count);
                    var bounds=ca.Assembly.LocalBounds;
                    Assert.That(bounds.size.x,Is.LessThanOrEqualTo(1.92001f));Assert.That(bounds.size.z,Is.LessThanOrEqualTo(1.92001f));
                    Assert.That(bounds.min.y,Is.EqualTo(0).Within(1e-5));Assert.That(bounds.max.y,Is.InRange(1.39999f,2.40001f));
                    for(int j=0;j<ca.Assembly.Parts.Count;j++) { CollectionAssert.AreEqual(ca.Assembly.Parts[j].Lease.Data.Vertices,cb.Assembly.Parts[j].Lease.Data.Vertices); Assert.AreEqual(ca.Assembly.Parts[j].Transform.localPosition,cb.Assembly.Parts[j].Transform.localPosition); }
                }
                Assert.Throws<System.ArgumentOutOfRangeException>(()=>ca.Initialize(1,float.NaN));
            }finally{TestHelpers.InvokePrivate(ca,"OnDestroy");TestHelpers.InvokePrivate(cb,"OnDestroy");Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
        }
    }
}
