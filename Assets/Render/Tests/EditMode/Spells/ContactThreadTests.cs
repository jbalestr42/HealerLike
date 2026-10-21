using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Spells
{

public class ContactThreadTests
{
    GameObject _sinkHost;
    GameObject _first;
    GameObject _second;
    SpellVisualSink _sink;
    ContactThread _thread;

    [SetUp]
    public void SetUp()
    {
        _sinkHost = new GameObject("Sink");
        _first = new GameObject("First");
        _second = new GameObject("Second");
        _first.transform.position = Vector3.left;
        _second.transform.position = Vector3.right;
        _sink = SpellSinkFixture.Add(_sinkHost);
        TestHelpers.InvokePrivate(_sink, "OnEnable");
        _thread = new ContactThread();
        _thread.Init(_sink);
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.InvokePrivate(_sink, "OnDestroy");
        Object.DestroyImmediate(_sinkHost);
        Object.DestroyImmediate(_first);
        Object.DestroyImmediate(_second);
    }

    [Test]
    public void Contact_First_DrawsNothing()
    {
        _thread.Contact(_first);

        Assert.AreEqual(0, _sink.impactCount);
    }

    [Test]
    public void Contact_Second_DrawsThreadFromFirst()
    {
        _thread.Contact(_first);

        _thread.Contact(_second);

        Assert.AreEqual(1, _sink.impactCount);
        SpellEffect thread = _sinkHost.GetComponentInChildren<SpellEffect>();
        Assert.IsTrue(thread.isContactThread);
        Transform stalk = thread.stalks[0];
        Vector3 threadStart = stalk.position - stalk.up * stalk.localScale.y * 0.5f;
        Assert.Less(Vector3.Distance(_first.transform.position, threadStart), 0.0001f);
    }

    [Test]
    public void Clear_ThenSecondContact_DrawsNothing()
    {
        _thread.Contact(_first);

        _thread.Clear();
        _thread.Contact(_second);

        Assert.AreEqual(0, _sink.impactCount);
    }

    [Test]
    public void Init_Again_ForgetsThePreviousContact()
    {
        _thread.Contact(_first);

        _thread.Init(_sink);
        _thread.Contact(_second);

        Assert.AreEqual(0, _sink.impactCount);
    }

    [Test]
    public void Contact_NoTargetOrNoSink_AllocatesNothing()
    {
        _thread.Init(null);
        _thread.Contact(null);
        for (int i = 0; i < 32; i++)
        {
            _thread.Contact(_first);
        }

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 64; i++)
        {
            _thread.Contact(_first);
        }
        long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, allocated);
    }
}

}
