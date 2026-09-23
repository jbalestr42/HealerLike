using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{
    public class ChainContactVisualTests
    {
        [Test]
        public void ThreadsUseContactOrderAndRebindClearsPreviousContact()
        {
            GameObject host = new GameObject("HLContacts");
            GameObject sinkHost = new GameObject("HLSink");
            GameObject a = new GameObject("HLFirst");
            GameObject b = new GameObject("HLSecond");
            ChainContactVisual observer = null;
            SpellVisualSink sink = null;
            try
            {
                Projectile projectile = host.AddComponent<Projectile>();
                observer = host.AddComponent<ChainContactVisual>();
                sink = sinkHost.AddComponent<SpellVisualSink>();
                sink.looks = UnityEditor.AssetDatabase.LoadAssetAtPath<SpellLooks>(
                    "Assets/Render/Spells/Data/SpellLooks.asset"
                );
                TestHelpers.InvokePrivate(sink, "OnEnable");
                observer.Bind(projectile, sink);
                a.transform.position = Vector3.left;
                b.transform.position = Vector3.right;
                projectile.OnHit.Invoke(new OnHitData { target = a });
                Assert.AreEqual(0, sink.impactCount);
                projectile.OnHit.Invoke(new OnHitData { target = b });
                Assert.AreEqual(1, sink.impactCount);
                SpellEffect thread = sinkHost.GetComponentInChildren<SpellEffect>();
                Assert.IsTrue(thread.contactThread);
                Assert.Less(
                    Vector3.Distance(
                        a.transform.position,
                        thread.parts[0].position - thread.parts[0].up * thread.parts[0].localScale.y
                    ),
                    0.0001f
                );
                observer.Bind(projectile, sink);
                projectile.OnHit.Invoke(new OnHitData { target = a });
                Assert.AreEqual(1, sink.impactCount);
                TestHelpers.InvokePrivate(observer, "OnDisable");
                projectile.OnHit.Invoke(new OnHitData { target = b });
                Assert.AreEqual(1, sink.impactCount);
            }
            finally
            {
                if (observer)
                {
                    TestHelpers.InvokePrivate(observer, "OnDestroy");
                }
                if (sink)
                {
                    TestHelpers.InvokePrivate(sink, "OnDestroy");
                }
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(sinkHost);
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void InvalidContactsAndRepeatedInactiveSinkContactsAllocateNothing()
        {
            GameObject host = new GameObject("HLContacts");
            GameObject target = new GameObject("HLTarget");
            ChainContactVisual observer = null;
            try
            {
                Projectile projectile = host.AddComponent<Projectile>();
                observer = host.AddComponent<ChainContactVisual>();
                observer.Bind(projectile, null);
                OnHitData hit = new OnHitData { target = target };
                projectile.OnHit.Invoke(null);
                projectile.OnHit.Invoke(new OnHitData());
                for (int i = 0; i < 32; i++)
                {
                    projectile.OnHit.Invoke(hit);
                }
                long before = System.GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 64; i++)
                {
                    projectile.OnHit.Invoke(hit);
                }
                long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0, allocated);
            }
            finally
            {
                if (observer)
                {
                    TestHelpers.InvokePrivate(observer, "OnDestroy");
                }
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(target);
            }
        }
    }
}
