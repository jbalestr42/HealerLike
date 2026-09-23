using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class StoneEffectsTests
    {
        static StoneEffects CreateEffects()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Render/Stones/Prefabs/StoneEffects.prefab");
            return Object.Instantiate(prefab).GetComponent<StoneEffects>();
        }

        static StoneEnemyVisual CreateVisual(GameObject target)
        {
            Transform pivot = new GameObject("BodyPivot").transform;
            pivot.SetParent(target.transform, false);
            Transform presentation = new GameObject("StonePresentation").transform;
            presentation.SetParent(pivot, false);
            StoneEnemyVisual visual = target.AddComponent<StoneEnemyVisual>();
            TestHelpers.SetPrivateField(visual, "_bodyPivot", pivot);
            TestHelpers.SetPrivateField(visual, "_presentation", presentation);
            return visual;
        }

        [Test]
        public void DustRisesFadesExpiresAndReusesWithoutAllocating()
        {
            StoneEffects fx = CreateEffects();
            GameObject go = fx.gameObject;
            try
            {
                fx.EmitDust(Vector3.zero, 1);
                Assert.AreEqual(5, fx.liveCount);

                MeshRenderer renderer = go.GetComponentInChildren<MeshRenderer>();
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                fx.Advance(0.1f);
                renderer.GetPropertyBlock(block);
                float alpha = block.GetColor("_BaseColor").a;
                Assert.Greater(renderer.transform.position.y, 0);

                fx.Advance(0.2f);
                renderer.GetPropertyBlock(block);
                Assert.Less(block.GetColor("_BaseColor").a, alpha);

                fx.Advance(1f);
                Assert.AreEqual(0, fx.liveCount);

                fx.EmitDust(Vector3.zero, 1);
                fx.Advance(0.01f);
                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 10; i++)
                {
                    fx.Advance(1f);
                    fx.EmitDust(Vector3.zero, 1);
                    fx.Advance(0.01f);
                }
                long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0, allocated);
                Assert.AreEqual(5, go.transform.childCount);
            }
            finally
            {
                TestHelpers.InvokePrivate(fx, "OnDestroy");
                Object.DestroyImmediate(go);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DisableClearsCopiesSlotsAndRejectsEveryEmission(bool deactivateObject)
        {
            StoneEffects fx = CreateEffects();
            GameObject go = fx.gameObject;
            GameObject source = new GameObject("SourceVisual");
            StoneEnemyVisual visual = CreateVisual(source);
            Mesh mesh = StoneMesh.CreateMesh(1, StonePresets.Boulder);
            try
            {
                visual.Init(null, 1, fx);
                fx.EmitDetachedPart(mesh, null, Matrix4x4.identity, Vector3.zero, 0f, 1);
                Mesh copy = go.GetComponentInChildren<MeshFilter>().sharedMesh;
                fx.EmitThrownContact(Vector3.zero, 1);
                if (deactivateObject)
                {
                    go.SetActive(false);
                }
                else
                {
                    fx.enabled = false;
                }
                TestHelpers.InvokePrivate(fx, "OnDisable");
                Assert.AreEqual(0, fx.liveCount);
                Assert.IsTrue(copy == null);
                foreach (MeshFilter filter in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    Assert.IsFalse(filter.gameObject.activeSelf);
                    Assert.IsNull(filter.sharedMesh);
                }

                fx.EmitHit(default, false, 1);
                fx.EmitThrownContact(Vector3.zero, 1);
                fx.EmitDetachedPart(mesh, null, Matrix4x4.identity, Vector3.zero, 0f, 1);
                fx.CollapseOnce(visual, 1);
                Assert.AreEqual(0, fx.liveCount);
                Assert.IsTrue(visual.parts[0].transform.gameObject.activeSelf);
                Assert.IsTrue(visual.TryBeginCollapse(), "Inactive effects must not consume collapse state");

                int pooled = go.transform.childCount;
                if (deactivateObject)
                {
                    go.SetActive(true);
                }
                else
                {
                    fx.enabled = true;
                }
                fx.EmitThrownContact(Vector3.zero, 1);
                Assert.Greater(fx.liveCount, 0);
                Assert.AreEqual(pooled, go.transform.childCount);
            }
            finally
            {
                TestHelpers.InvokePrivate(visual, "OnDestroy");
                TestHelpers.InvokePrivate(fx, "OnDestroy");
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void ImpactRaisesTheEventAndDisabledOwnerStaysSilent()
        {
            StoneEffects fx = CreateEffects();
            int impacts = 0;
            fx.OnImpactRecorded.AddListener(position => impacts++);
            try
            {
                fx.RecordImpact(Vector3.zero, 1);
                Assert.AreEqual(1, impacts);
                Assert.AreEqual(5, fx.liveCount);

                fx.Advance(1f);
                fx.enabled = false;
                TestHelpers.InvokePrivate(fx, "OnDisable");
                fx.RecordImpact(Vector3.zero, 1);
                fx.EmitThrownContact(Vector3.zero, 1);
                Assert.AreEqual(1, impacts);
                Assert.AreEqual(0, fx.liveCount);
            }
            finally
            {
                TestHelpers.InvokePrivate(fx, "OnDestroy");
                Object.DestroyImmediate(fx.gameObject);
            }
        }

        [Test]
        public void HitCountsCapAndLifetime()
        {
            StoneEffects fx = CreateEffects();
            GameObject go = fx.gameObject;
            try
            {
                StoneImpact impact = new StoneImpact(Vector3.up, Vector3.up, Vector3.zero, true);
                fx.EmitHit(impact, false, 1);
                Assert.AreEqual(9, fx.liveCount);

                fx.Advance(0.6f);
                Assert.AreEqual(0, fx.liveCount);

                fx.EmitHit(impact, true, 1);
                Assert.AreEqual(14, fx.liveCount);

                for (uint i = 0; i < 40; i++)
                {
                    fx.EmitHit(impact, true, i);
                }
                Assert.AreEqual(StoneEffects.MaxLiveFragments, fx.liveCount); // 41 * 14 fragments asked, 256 kept

                fx.Advance(1f);
                Assert.AreEqual(0, fx.liveCount);
                Assert.AreEqual(0, go.GetComponentsInChildren<Collider>().Length);
                Assert.AreEqual(0, go.GetComponentsInChildren<Rigidbody>().Length);
            }
            finally
            {
                TestHelpers.InvokePrivate(fx, "OnDestroy");
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DetachedCopySurvivesSourceReleaseAndSplitsIntoThree()
        {
            StoneEffects fx = CreateEffects();
            GameObject go = fx.gameObject;
            Mesh mesh = StoneMesh.CreateMesh(1, StonePresets.Boulder);
            try
            {
                Matrix4x4 pose = Matrix4x4.TRS(Vector3.up, Quaternion.identity, Vector3.one);
                fx.EmitDetachedPart(mesh, null, pose, Vector3.zero, 0f, 1);
                Object.DestroyImmediate(mesh);
                fx.Advance(0.24f);
                Assert.AreEqual(1, fx.liveCount);
                Assert.IsNotNull(go.GetComponentInChildren<MeshFilter>().sharedMesh);

                fx.Advance(0.01f);
                Assert.AreEqual(3, fx.liveCount);

                fx.Advance(0.25f);
                Assert.AreEqual(0, fx.liveCount);
            }
            finally
            {
                if (mesh != null)
                {
                    Object.DestroyImmediate(mesh);
                }
                TestHelpers.InvokePrivate(fx, "OnDestroy");
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ShardComesFromThePoolAndGoesBack()
        {
            StoneEffects fx = CreateEffects();
            Mesh mesh = StoneMesh.CreateMesh(1, StonePresets.Boulder);
            try
            {
                Transform shard = fx.TakeShard(mesh, Color.white);
                Assert.IsTrue(shard.gameObject.activeSelf);
                Assert.AreSame(mesh, shard.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreEqual(0, fx.liveCount);

                fx.ReturnShard(shard);
                Assert.IsFalse(shard.gameObject.activeSelf);
                fx.EmitTrickle(Vector3.zero, 1);
                Assert.AreEqual(1, fx.transform.childCount);
            }
            finally
            {
                TestHelpers.InvokePrivate(fx, "OnDestroy");
                Object.DestroyImmediate(fx.gameObject);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void OneAnalyticBounceNeverFallsBelowGround()
        {
            for (int i = 0; i < 100; i++)
            {
                Vector3 position = StoneEffects.PositionAt(Vector3.up, Vector3.right, i * 0.02f, 0f, true);
                Assert.That(position.y, Is.GreaterThanOrEqualTo(0));
            }
            Assert.That(StoneEffects.PositionAt(Vector3.up, Vector3.right, 1f, 0f, false).y, Is.LessThan(0));
        }
    }
}
