using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Stones
{
    public class HLStoneGroundShadowTests
    {
        [Test] public void ShadowPointsAwayFromLightStaysFlatAndCanBeDisabled()
        {
            var go=new GameObject("HLShadowOwner");
            try
            {
                var shadow=go.AddComponent<HLStoneGroundShadow>();
                shadow.Configure(new Bounds(Vector3.up,Vector3.one*2),new Vector3(-1,2,0),true);
                Assert.Greater(shadow.Disc.position.x,0); Assert.Greater(shadow.Disc.position.y,0);
                Assert.Greater(Vector3.Dot(shadow.Disc.forward,Vector3.right),.999f);
                Assert.Greater(shadow.Disc.localScale.z,shadow.Disc.localScale.x);
                Assert.Less(shadow.Disc.localScale.y,.01f);
                shadow.Visible=false; Assert.False(shadow.Disc.gameObject.activeSelf);
                shadow.Configure(new Bounds(Vector3.up,Vector3.one*2),Vector3.zero,true);
                Assert.True(shadow.Disc.gameObject.activeSelf); Assert.AreEqual(1,go.transform.childCount);
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void TerrainAutomaticallyCreatesOneShadowAndPropagatesToggle()
        {
            var go=new GameObject("HLTerrain");
            try
            {
                var terrain=go.AddComponent<HLStoneTerrainClump>(); terrain.Initialize(4,1); terrain.Initialize(5,2);
                Assert.AreEqual(1,go.GetComponents<HLStoneGroundShadow>().Length);
                terrain.GroundShadowEnabled=false; Assert.False(go.GetComponent<HLStoneGroundShadow>().Disc.gameObject.activeSelf);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
