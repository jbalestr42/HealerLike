using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class ChainContactVisualTests
{
    GameObject _host;
    GameObject _sinkHost;
    GameObject _first;
    GameObject _second;
    Projectile _projectile;
    ChainContactVisual _observer;

    [SetUp]
    public void SetUp()
    {
        _host = new GameObject("Contacts");
        _sinkHost = new GameObject("Sink");
        _first = new GameObject("First");
        _second = new GameObject("Second");
        _projectile = _host.AddComponent<Projectile>();
        _observer = _host.AddComponent<ChainContactVisual>();
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.InvokePrivate(_observer, "OnDestroy");
        SpellVisualSink sink = _sinkHost.GetComponent<SpellVisualSink>();
        if (sink)
        {
            TestHelpers.InvokePrivate(sink, "OnDestroy");
        }
        Object.DestroyImmediate(_host);
        Object.DestroyImmediate(_sinkHost);
        Object.DestroyImmediate(_first);
        Object.DestroyImmediate(_second);
    }

    [Test]
    public void OnHit_SecondContact_DrawsThreadFromFirstAndRebindForgetsIt()
    {
        SpellVisualSink sink = _sinkHost.AddComponent<SpellVisualSink>();
        sink.looks = AssetDatabase.LoadAssetAtPath<SpellLooks>("Assets/Render/Spells/Data/SpellLooks.asset");
        TestHelpers.InvokePrivate(sink, "OnEnable");
        _observer.Bind(_projectile, sink);
        _first.transform.position = Vector3.left;
        _second.transform.position = Vector3.right;

        _projectile.OnHit.Invoke(new OnHitData { target = _first });
        Assert.AreEqual(0, sink.impactCount);
        _projectile.OnHit.Invoke(new OnHitData { target = _second });

        Assert.AreEqual(1, sink.impactCount);
        SpellEffect thread = _sinkHost.GetComponentInChildren<SpellEffect>();
        Assert.IsTrue(thread.contactThread);
        Vector3 threadStart = thread.parts[0].position - thread.parts[0].up * thread.parts[0].localScale.y;
        Assert.Less(Vector3.Distance(_first.transform.position, threadStart), 0.0001f);

        _observer.Bind(_projectile, sink);
        _projectile.OnHit.Invoke(new OnHitData { target = _first });
        Assert.AreEqual(1, sink.impactCount);
        TestHelpers.InvokePrivate(_observer, "OnDisable");
        _projectile.OnHit.Invoke(new OnHitData { target = _second });
        Assert.AreEqual(1, sink.impactCount);
    }

    [Test]
    public void OnHit_InvalidOrSinklessContacts_AllocateNothing()
    {
        _observer.Bind(_projectile, null);
        OnHitData hit = new OnHitData { target = _first };
        _projectile.OnHit.Invoke(null);
        _projectile.OnHit.Invoke(new OnHitData());
        for (int i = 0; i < 32; i++)
        {
            _projectile.OnHit.Invoke(hit);
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 64; i++)
        {
            _projectile.OnHit.Invoke(hit);
        }
        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, allocated);
    }
}

}
