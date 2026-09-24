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

    // What Init(RenderManager) hands, without a manager, then what the projectile's own Init does
    void Observe(ChainContactVisual observer, SpellVisualSink sink, GameObject source)
    {
        TestHelpers.SetPrivateField(observer, "_sink", sink);
        observer.projectile = _projectile;
        observer.Init(source);
    }

    SpellVisualSink CreateSink()
    {
        SpellVisualSink sink = SpellSinkFixture.Add(_sinkHost);
        TestHelpers.InvokePrivate(sink, "OnEnable");
        Observe(_observer, sink, _host);
        _first.transform.position = Vector3.left;
        _second.transform.position = Vector3.right;
        return sink;
    }

    [Test]
    public void OnHit_FirstContact_DrawsNothing()
    {
        SpellVisualSink sink = CreateSink();

        _projectile.OnHit.Invoke(new OnHitData { target = _first });

        Assert.AreEqual(0, sink.impactCount);
    }

    [Test]
    public void OnHit_SecondContact_DrawsThreadFromFirst()
    {
        SpellVisualSink sink = CreateSink();
        _projectile.OnHit.Invoke(new OnHitData { target = _first });

        _projectile.OnHit.Invoke(new OnHitData { target = _second });

        Assert.AreEqual(1, sink.impactCount);
        SpellEffect thread = _sinkHost.GetComponentInChildren<SpellEffect>();
        Assert.IsTrue(thread.isContactThread);
        Transform stalk = thread.stalks[0];
        Vector3 threadStart = stalk.position - stalk.up * stalk.localScale.y * 0.5f;
        Assert.Less(Vector3.Distance(_first.transform.position, threadStart), 0.0001f);
    }

    [Test]
    public void Init_Rebound_ForgetsThePreviousContact()
    {
        SpellVisualSink sink = CreateSink();
        _projectile.OnHit.Invoke(new OnHitData { target = _first });

        Observe(_observer, sink, _host);
        _projectile.OnHit.Invoke(new OnHitData { target = _second });

        Assert.AreEqual(0, sink.impactCount);
    }

    [Test]
    public void OnDisable_ThenSecondContact_DrawsNothing()
    {
        SpellVisualSink sink = CreateSink();
        _projectile.OnHit.Invoke(new OnHitData { target = _first });

        TestHelpers.InvokePrivate(_observer, "OnDisable");
        _projectile.OnHit.Invoke(new OnHitData { target = _second });

        Assert.AreEqual(0, sink.impactCount);
    }

    [Test]
    public void OnHit_InvalidOrSinklessContacts_AllocateNothing()
    {
        Observe(_observer, null, _host);
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
