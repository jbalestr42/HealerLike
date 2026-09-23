using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class HLStoneEffectsTests
    {
        [Test]
        public void DustRisesFadesExpiresAndReusesWithoutAllocating()
        {
            GameObject go = new GameObject("HLDustTest");
            HLStoneEffects fx = go.AddComponent<HLStoneEffects>();
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
            GameObject go = new GameObject("HLDisableEffects");
            HLStoneEffects fx = go.AddComponent<HLStoneEffects>();
            GameObject source = new GameObject("HLSourceVisual");
            HLStoneEnemyVisual visual = source.AddComponent<HLStoneEnemyVisual>();
            Mesh mesh = HLStoneMesh.CreateMesh(1, HLStonePresets.Boulder);
            GameObject other = new GameObject("HLOtherEffects");
            HLStoneEffects second = other.AddComponent<HLStoneEffects>();
            int baseline = HLStoneEffects.globalLiveCount;
            try
            {
                visual.Initialize(null, 1, fx);
                second.EmitThrownContact(Vector3.zero, 1);
                int otherCount = second.liveCount;
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
                Assert.AreEqual(baseline + otherCount, HLStoneEffects.globalLiveCount);
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

                TestHelpers.InvokePrivate(fx, "OnDestroy");
                Object.DestroyImmediate(go);
                Assert.AreEqual(baseline + otherCount, HLStoneEffects.globalLiveCount);
            }
            finally
            {
                if (fx != null)
                {
                    TestHelpers.InvokePrivate(fx, "OnDestroy");
                }
                TestHelpers.InvokePrivate(second, "OnDestroy");
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(other);
                Object.DestroyImmediate(mesh);
            }
            Assert.AreEqual(baseline, HLStoneEffects.globalLiveCount);
        }

        [Test]
        public void SceneLookupPreservesDisabledOwnerWithoutSpawning()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            HLStoneEffects fx = HLStoneEffects.ForScene(scene, null);
            try
            {
                fx.enabled = false;
                TestHelpers.InvokePrivate(fx, "OnDisable");
                Assert.AreSame(fx, HLStoneEffects.ForScene(fx.gameObject.scene, null));

                fx.EmitThrownContact(Vector3.zero, 1);
                Assert.AreEqual(0, fx.liveCount);
                Assert.AreEqual(0, fx.transform.childCount);
            }
            finally
            {
                TestHelpers.InvokePrivate(fx, "OnDestroy");
                Object.DestroyImmediate(fx.gameObject);
            }
        }

        [Test]
        public void HitCountsGlobalCapAndLifetime()
        {
            GameObject go = new GameObject("HLEffectsTest");
            HLStoneEffects fx = go.AddComponent<HLStoneEffects>();
            GameObject other = new GameObject("HLEffectsOther");
            HLStoneEffects second = other.AddComponent<HLStoneEffects>();
            try
            {
                HLStoneImpact impact = new HLStoneImpact(Vector3.up, Vector3.up, Vector3.zero, true);
                fx.EmitHit(impact, false, 1);
                Assert.AreEqual(9, fx.liveCount);

                fx.Advance(0.6f);
                Assert.AreEqual(0, fx.liveCount);

                fx.EmitHit(impact, true, 1);
                Assert.AreEqual(14, fx.liveCount);

                for (uint i = 0; i < 40; i++)
                {
                    HLStoneEffects owner = i % 2 == 0 ? fx : second;
                    owner.EmitHit(impact, true, i);
                }
                Assert.AreEqual(256, HLStoneEffects.globalLiveCount);

                fx.Advance(1f);
                second.Advance(1f);
                Assert.AreEqual(0, HLStoneEffects.globalLiveCount);
                Assert.AreEqual(0, go.GetComponentsInChildren<Collider>().Length);
                Assert.AreEqual(0, go.GetComponentsInChildren<Rigidbody>().Length);
            }
            finally
            {
                TestHelpers.InvokePrivate(fx, "OnDestroy");
                TestHelpers.InvokePrivate(second, "OnDestroy");
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void DetachedCopySurvivesSourceReleaseAndSplitsIntoThree()
        {
            GameObject go = new GameObject("HLEffectsTest");
            HLStoneEffects fx = go.AddComponent<HLStoneEffects>();
            Mesh mesh = HLStoneMesh.CreateMesh(1, HLStonePresets.Boulder);
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
        public void OneAnalyticBounceNeverFallsBelowGround()
        {
            for (int i = 0; i < 100; i++)
            {
                Vector3 position = HLStoneEffects.PositionAt(Vector3.up, Vector3.right, i * 0.02f, 0f, true);
                Assert.That(position.y, Is.GreaterThanOrEqualTo(0));
            }
            Assert.That(HLStoneEffects.PositionAt(Vector3.up, Vector3.right, 1f, 0f, false).y, Is.LessThan(0));
        }
    }
}
