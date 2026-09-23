using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneGroundShadowTests
    {
        [Test]
        public void ShadowPointsAwayFromLightStaysFlatAndCanBeDisabled()
        {
            GameObject go = new GameObject("HLShadowOwner");
            try
            {
                HLStoneGroundShadow shadow = go.AddComponent<HLStoneGroundShadow>();
                shadow.Configure(new Bounds(Vector3.up, Vector3.one * 2f), new Vector3(-1f, 2f, 0f), true);
                Assert.Greater(shadow.disc.position.x, 0);
                Assert.Greater(shadow.disc.position.y, 0);
                Assert.Greater(Vector3.Dot(shadow.disc.forward, Vector3.right), 0.999f);
                Assert.Greater(shadow.disc.localScale.z, shadow.disc.localScale.x);
                Assert.Less(shadow.disc.localScale.y, 0.01f);

                shadow.visible = false;
                Assert.False(shadow.disc.gameObject.activeSelf);

                shadow.Configure(new Bounds(Vector3.up, Vector3.one * 2f), Vector3.zero, true);
                Assert.True(shadow.disc.gameObject.activeSelf);
                Assert.AreEqual(1, go.transform.childCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TerrainAutomaticallyCreatesOneShadowAndPropagatesToggle()
        {
            GameObject go = new GameObject("HLTerrain");
            try
            {
                HLStoneTerrainClump terrain = go.AddComponent<HLStoneTerrainClump>();
                terrain.Initialize(4, 1f);
                terrain.Initialize(5, 2f);
                Assert.AreEqual(1, go.GetComponents<HLStoneGroundShadow>().Length);

                terrain.groundShadowEnabled = false;
                Assert.False(go.GetComponent<HLStoneGroundShadow>().disc.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
