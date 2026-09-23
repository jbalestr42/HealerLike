using System;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{

public class StoneLifeStateTests
{
    [Test]
    public void PollHealth_BelowHalf_TricklesBoundedAndHealingRearms()
    {
        StoneLifeState state = new StoneLifeState();
        Assert.False(state.PollHealth(0.5f, 0.1f));
        Assert.True(state.PollHealth(0.49f, 0.1f));

        int count = 0;
        for (int i = 0; i < 50; i++)
        {
            if (state.PollHealth(0.4f, 0.1f))
            {
                count++;
            }
        }
        Assert.That(count, Is.InRange(10, 16));
        Assert.False(state.PollHealth(0.3f, 1f));

        state.PollHealth(1f, 0f);
        Assert.True(state.PollHealth(0.4f, 0.1f));
        Assert.False(state.PollHealth(0f, 0.1f));
        Assert.False(state.PollHealth(float.NaN, 0.1f));
    }

    [Test]
    public void PollZones_HostileOverlap_CountsOncePerPulseDespiteCompaction()
    {
        StoneLifeState state = new StoneLifeState();
        Zone zone = new Zone
        {
            kind = (int)ZoneKind.Hostile,
            position = Vector3.zero,
            radius = 1f,
            strength = 1f,
            age = 0f
        };
        Zone heal = zone;
        heal.kind = (int)ZoneKind.Heal;
        Assert.AreEqual(1, state.PollZones(new Zone[] { heal, zone }, Vector3.right * 1.2f, 0.3f));

        zone.age = 0.1f;
        zone.strength = 0.8f;
        Assert.AreEqual(0, state.PollZones(new Zone[] { zone }, Vector3.right * 1.2f, 0.3f));

        zone.age = 0f;
        zone.strength = 1f;
        Assert.AreEqual(1, state.PollZones(new Zone[] { zone }, Vector3.right * 1.2f, 0.3f));

        state.PollZones(ReadOnlySpan<Zone>.Empty, Vector3.zero, 0.3f);
        Assert.AreEqual(0, state.PollZones(new Zone[] { zone }, Vector3.right * 3f, 0.3f));
        Assert.AreEqual(0, state.PollZones(new Zone[] { heal }, Vector3.zero, 0.3f));
    }

    [Test]
    public void PollZones_TwoConcurrentPulses_EachCountOnceThenStayQuiet()
    {
        StoneLifeState state = new StoneLifeState();
        Zone[] zones =
        {
            new Zone { kind = 2, radius = 1f, strength = 1f },
            new Zone { kind = 2, radius = 1f, strength = 1f }
        };
        Assert.AreEqual(2, state.PollZones(zones, Vector3.zero, 0.2f));
        Assert.AreEqual(0, state.PollZones(zones, Vector3.zero, 0.2f));
    }

    [Test]
    public void Wobble_AfterImpact_KicksThenSettles()
    {
        Assert.AreEqual(0, StoneLifeState.Wobble(0f));
        Assert.AreEqual(0, StoneLifeState.Wobble(1.2f));
        Assert.Greater(Mathf.Abs(StoneLifeState.Wobble(0.05f)), 3);
    }

    [Test]
    public void Ochre_HundredSeeds_PicksOneInFive()
    {
        int count = 0;

        for (uint i = 0; i < 100; i++)
        {
            if (StoneLifeState.Ochre(i))
            {
                count++;
            }
        }

        Assert.AreEqual(20, count);
    }

    [Test]
    public void PollZonesAndHealth_Repeated_AllocateNothing()
    {
        StoneLifeState state = new StoneLifeState();
        Zone[] zones = { new Zone { kind = 2, radius = 1f, strength = 1f } };
        state.PollZones(zones, Vector3.zero, 1f);
        state.PollHealth(1f, 0.016f);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            state.PollZones(zones, Vector3.zero, 1f);
            state.PollHealth(1f, 0.016f);
        }
        Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}

}
