using System;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GroundTests
{
    class FakeBody : IGroundBody
    {
        public int AppendCapsules(BodyCapsule[] into, int start)
        {
            if (start >= into.Length)
            {
                return 0;
            }

            into[start] = new BodyCapsule
            {
                start = new Vector3(0f, 0.1f, 0f), end = new Vector3(1f, 0.1f, 0f), radius = 0.1f, press = 1f
            };
            return 1;
        }
    }

    Ground _ground;
    GroundEffect _burst;

    [SetUp]
    public void SetUp()
    {
        _ground = new Ground();
        _burst = new GroundEffect { lifetime = 0.5f, hold = 0.4f };
    }

    [TearDown]
    public void TearDown()
    {
        _ground.Dispose();
    }

    [Test]
    public void Play_OneShot_PlaysThenFadesAway()
    {
        _ground.Play(_burst, new Vector3(1f, 0f, 2f), 1.5f, 0.8f);

        Assert.AreEqual(1, _ground.Playing(_burst));
        Assert.IsTrue(_ground.Find(_burst, out Vector2 at, out _, out float radius, out float strength));
        Assert.AreEqual(new Vector2(1f, 2f), at);
        Assert.AreEqual(1.5f, radius);
        Assert.AreEqual(0.8f, strength);
        Assert.AreEqual(1, GroundProbe.Count(_ground, GroundStampKind.Disc));

        _ground.Advance(0.6f);

        Assert.AreEqual(0, _ground.oneShotCount);
        Assert.IsFalse(_ground.Find(_burst, out _, out _, out _, out _));
    }

    [Test]
    public void Play_Invalid_IsIgnored()
    {
        _ground.Play(null, Vector3.zero, 1f);
        _ground.Play(_burst, new Vector3(float.NaN, 0f, 0f), 1f);
        _ground.Play(_burst, Vector3.zero, 1f, 0f);
        _ground.Play(_burst, Vector3.zero, float.PositiveInfinity);

        Assert.AreEqual(0, _ground.oneShotCount);
    }

    [Test]
    public void Play_PastCapacity_ReplacesTheOldest()
    {
        GroundEffect lasting = new GroundEffect { lifetime = 10f, hold = 0.1f };
        for (int i = 0; i < Ground.OneShotCapacity; i++)
        {
            _ground.Play(lasting, Vector3.zero, 1f);
        }

        _ground.Advance(0.1f);
        _ground.Play(_burst, Vector3.zero, 1f);

        Assert.AreEqual(Ground.OneShotCapacity, _ground.oneShotCount);
        Assert.AreEqual(1, _ground.Playing(_burst));
    }

    [Test]
    public void Hold_ShownWhilePlacedHiddenOtherwiseAndForgottenOnRelease()
    {
        GroundHandle handle = _ground.Hold(_burst);

        Assert.AreEqual(1, _ground.heldCount);
        Assert.AreEqual(0, GroundProbe.Stamps(_ground).Count, "Hidden until shown.");

        handle.Show(Vector3.one, 1f, 1f);
        Assert.IsTrue(handle.isShown);
        Assert.AreEqual(1, GroundProbe.Stamps(_ground).Count);
        _ground.Advance(5f);
        Assert.AreEqual(1, GroundProbe.Stamps(_ground).Count, "A held effect outlasts its lifetime.");

        handle.Hide();
        Assert.AreEqual(0, GroundProbe.Stamps(_ground).Count);

        handle.Release();
        handle.Show(Vector3.one, 1f, 1f);
        Assert.AreEqual(0, _ground.heldCount);
        Assert.IsFalse(handle.isShown, "A released handle shows nothing again.");
    }

    [Test]
    public void Hold_ShownAgain_EasesInFromTheStart()
    {
        GroundHandle handle = _ground.Hold(new GroundEffect { fadeIn = 0.2f, hold = 0.5f });
        handle.Show(Vector3.zero, 1f, 1f);
        _ground.Advance(0.3f);
        Assert.AreEqual(1, GroundProbe.Stamps(_ground).Count);

        handle.Hide();
        handle.Show(Vector3.zero, 1f, 1f);

        Assert.AreEqual(0, GroundProbe.Stamps(_ground).Count, "Its age starts again, not yet eased in.");
    }

    [Test]
    public void Hold_ShowInvalid_Hides()
    {
        GroundHandle handle = _ground.Hold(_burst);
        handle.Show(Vector3.zero, 1f, 1f);

        handle.Show(Vector3.zero, 1f, 0f);

        Assert.IsFalse(handle.isShown);
    }

    [Test]
    public void Collect_Zone_MovesTheGrassThroughTheVocabulary()
    {
        Zone[] zones =
        {
            new Zone { kind = (int)ZoneKind.Heal, radius = 2f, strength = 1f, age = 1f },
            new Zone { kind = (int)ZoneKind.Hostile, radius = 2f, strength = 1f, age = 1f }
        };
        GroundStamp[] into = new GroundStamp[8];

        int count = _ground.Collect(into, 0, zones, null, 0f, 1f);

        Assert.AreEqual(3, count, "A heal holds, spins and marks; a hostile zone moves nothing.");
    }

    [Test]
    public void AddBody_Twice_PressesOnceUntilRemoved()
    {
        FakeBody body = new FakeBody();
        _ground.AddBody(body);
        _ground.AddBody(body);
        _ground.AddBody(null);

        Assert.AreEqual(1, _ground.bodyCount);
        Assert.AreEqual(1, GroundProbe.Count(_ground, GroundStampKind.Body));

        _ground.RemoveBody(body);

        Assert.AreEqual(0, GroundProbe.Count(_ground, GroundStampKind.Body));
    }

    [Test]
    public void Clear_DropsOneShotsHidesHeldKeepsBodies()
    {
        GroundHandle handle = _ground.Hold(_burst);
        handle.Show(Vector3.zero, 1f, 1f);
        _ground.Play(_burst, Vector3.zero, 1f);
        _ground.AddBody(new FakeBody());

        _ground.Clear();

        Assert.AreEqual(0, _ground.oneShotCount);
        Assert.IsFalse(handle.isShown);
        Assert.AreEqual(1, _ground.bodyCount);
    }

    [Test]
    public void Advance_PausedOrInvalid_ChangesNothing()
    {
        _ground.Play(_burst, Vector3.zero, 1f);

        _ground.Advance(0f);
        _ground.Advance(float.NaN);
        _ground.Advance(-1f);

        Assert.AreEqual(1, _ground.oneShotCount);
    }

    [Test]
    public void Collect_Steady_AllocatesNothing()
    {
        GroundHandle handle = _ground.Hold(_burst);
        handle.Show(Vector3.zero, 1f, 1f);
        _ground.Play(new GroundEffect { lifetime = 10f, hold = 0.2f }, Vector3.zero, 1f);
        _ground.AddBody(new FakeBody());
        GroundStamp[] into = new GroundStamp[64];
        BodyCapsule[] capsules = new BodyCapsule[16];
        Zone[] zones = { new Zone { kind = (int)ZoneKind.Heal, radius = 1f, strength = 1f, age = 1f } };
        _ground.Collect(into, 0, zones, capsules, 0f, 1f);
        _ground.Advance(0.01f);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            _ground.Advance(0.01f);
            _ground.Collect(into, 0, zones, capsules, 0f, 1f);
        }

        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}

}
