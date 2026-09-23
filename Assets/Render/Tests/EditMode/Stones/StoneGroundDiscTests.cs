using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class StoneGroundDiscTests
    {
        static StoneGroundDisc CreateDisc(Transform parent, bool isShadow)
        {
            GameObject discGo = new GameObject("HLDisc", typeof(MeshFilter), typeof(MeshRenderer));
            discGo.transform.SetParent(parent, false);
            StoneGroundDisc disc = discGo.AddComponent<StoneGroundDisc>();
            TestHelpers.SetPrivateField(disc, "_renderer", discGo.GetComponent<MeshRenderer>());
            TestHelpers.SetPrivateField(disc, "_isShadow", isShadow);
            return disc;
        }

        [Test]
        public void ShadowPointsAwayFromLightStaysFlatAndCanBeHidden()
        {
            GameObject go = new GameObject("HLShadowOwner");
            try
            {
                StoneGroundDisc shadow = CreateDisc(go.transform, true);
                shadow.Init(new Bounds(Vector3.up, Vector3.one * 2f), new Vector3(-1f, 2f, 0f));
                Assert.Greater(shadow.transform.position.x, 0);
                Assert.Greater(shadow.transform.position.y, 0);
                Assert.Greater(Vector3.Dot(shadow.transform.forward, Vector3.right), 0.999f);
                Assert.Greater(shadow.transform.localScale.z, shadow.transform.localScale.x);
                Assert.Less(shadow.transform.localScale.y, 0.01f);

                shadow.Show(false);
                Assert.False(shadow.gameObject.activeSelf);

                shadow.Init(new Bounds(Vector3.up, Vector3.one * 2f), Vector3.zero);
                shadow.Show(true);
                Assert.True(shadow.gameObject.activeSelf);
                Assert.Greater(Vector3.Dot(shadow.transform.forward, Vector3.forward), 0.999f);
                Assert.AreEqual(1, go.transform.childCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BareEarthRadiusCoversScaledClumpAndColourGoesThroughTheBlock()
        {
            GameObject root = new GameObject("HLRing");
            try
            {
                StoneGroundDisc ring = CreateDisc(root.transform, false);
                ring.Init(new Bounds(Vector3.up, new Vector3(2f, 2f, 1f)), Vector3.up);
                Assert.AreEqual(1.18f, ring.radius, 0.001f);
                Assert.AreEqual(0.006f, ring.center.y, 0.0001f);

                root.transform.localScale = new Vector3(2f, 1f, 3f);
                Assert.AreEqual(3.54f, ring.radius, 0.001f);

                Color colour = new Color(0.2f, 0.6f, 0.3f, 1f);
                ring.colour = colour;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                ring.GetComponent<MeshRenderer>().GetPropertyBlock(block);
                Assert.IsFalse(block.isEmpty);
                Assert.AreEqual(colour, ring.colour);
                Assert.AreEqual(0, root.GetComponentsInChildren<Collider>().Length);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TerrainReusesItsTwoDiscsAndPropagatesShadowToggle()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/HLStoneBlock.prefab");
            GameObject block = Object.Instantiate(prefab);
            HLStoneTerrainClump terrain = block.GetComponentInChildren<HLStoneTerrainClump>();
            try
            {
                terrain.Init(4, 1f, null, null);
                terrain.Init(5, 2f, null, null);
                Assert.AreEqual(2, block.GetComponentsInChildren<StoneGroundDisc>(true).Length);
                Assert.AreEqual(2, block.GetComponentsInChildren<StoneGroundDisc>().Length);

                terrain.groundShadowEnabled = false;
                Assert.AreEqual(1, block.GetComponentsInChildren<StoneGroundDisc>().Length);
                Assert.IsFalse(block.GetComponentInChildren<StoneGroundDisc>().isShadow);
            }
            finally
            {
                TestHelpers.InvokePrivate(terrain, "OnDestroy");
                Object.DestroyImmediate(block);
            }
        }
    }
}
