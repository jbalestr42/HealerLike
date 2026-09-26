using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class ZoneStampsTests
{
    static Zone Make(ZoneKind kind, Vector3 position, float radius, float age, float strength = 1f, uint heading = 0)
    {
        return new Zone
        {
            position = position, radius = radius, kind = (int)kind, strength = strength, age = age, reserved = heading
        };
    }

    [Test]
    public void TryCreate_Heal_LeansOutwardOnceBloomed()
    {
        Assert.IsTrue(ZoneStamps.TryCreate(Make(ZoneKind.Heal, new Vector3(1f, 0.5f, 2f), 2f, 1f), out GroundStamp stamp));

        Vector3 east = stamp.Sample(new Vector2(2f, 2f));
        Assert.That(east.x, Is.EqualTo(ZoneStamps.HealOutward).Within(1e-5f));
        Assert.AreEqual(0f, east.z, "Heals never flatten.");
    }

    [Test]
    public void TryCreate_HealBlooming_GrowsItsRadius()
    {
        ZoneStamps.TryCreate(Make(ZoneKind.Heal, Vector3.zero, 2f, 0.15f), out GroundStamp half);
        ZoneStamps.TryCreate(Make(ZoneKind.Heal, Vector3.zero, 2f, 1f), out GroundStamp full);

        Assert.AreEqual(1f, half.centreRadius.z, 1e-5f);
        Assert.AreEqual(2f, full.centreRadius.z, 1e-5f);
        Assert.IsFalse(ZoneStamps.TryCreate(Make(ZoneKind.Heal, Vector3.zero, 2f, 0f), out _));
    }

    [Test]
    public void TryCreate_Trample_FlattensOutwardWithAWobblingRim()
    {
        Assert.IsTrue(ZoneStamps.TryCreate(Make(ZoneKind.Trample, Vector3.zero, 1f, 1f), out GroundStamp stamp));

        Vector3 knee = stamp.Sample(new Vector2(0.5f, 0f));
        Assert.AreEqual(1f, knee.z);
        Assert.Greater(knee.x, 0f);
        Assert.AreEqual(ZoneStamps.TrampleWobble, stamp.shape.z);
    }

    [Test]
    public void TryCreate_Launch_SendsAFrontAlongTheHeading()
    {
        uint north = ZonePacker.EncodeDirection(Vector3.forward);
        Zone launch = Make(ZoneKind.Launch, Vector3.zero, 4f, 0.2f, heading: north);

        Assert.IsTrue(ZoneStamps.TryCreate(launch, out GroundStamp stamp));

        Assert.AreEqual(2f, stamp.centreRadius.z, 1e-4f, "Half way across the radius at half the crossing time.");
        Vector3 front = stamp.Sample(new Vector2(0f, 2f));
        Assert.That(front.y, Is.EqualTo(ZoneStamps.LaunchLean).Within(0.001f));
        Assert.AreEqual(Vector3.zero, stamp.Sample(new Vector2(0f, -2f)));
    }

    [Test]
    public void TryCreate_HeightOnlyKinds_WriteNoStamp()
    {
        Assert.IsFalse(ZoneStamps.TryCreate(Make(ZoneKind.Hostile, Vector3.zero, 1f, 1f), out _));
        Assert.IsFalse(ZoneStamps.TryCreate(Make(ZoneKind.Bruise, Vector3.zero, 1f, 1f), out _));
        Assert.IsFalse(ZoneStamps.TryCreate(Make(ZoneKind.None, Vector3.zero, 1f, 1f), out _));
    }

    [Test]
    public void TryCreate_FadedOrEmptyZone_WritesNoStamp()
    {
        Assert.IsFalse(ZoneStamps.TryCreate(Make(ZoneKind.Trample, Vector3.zero, 1f, 1f, strength: 0f), out _));
        Assert.IsFalse(ZoneStamps.TryCreate(Make(ZoneKind.Trample, Vector3.zero, 0f, 1f), out _));
        Assert.IsFalse(ZoneStamps.TryCreate(Make(ZoneKind.Trample, Vector3.zero, 1f, 0f), out _));
    }

    [Test]
    public void TryCreate_Onset_ScalesThePush()
    {
        ZoneStamps.TryCreate(Make(ZoneKind.Range, Vector3.zero, 2f, 1f, strength: 0.5f), out GroundStamp stamp);

        Assert.AreEqual(0.5f * ZoneStamps.RangeOutward, stamp.push.w, 1e-6f);
    }

    [Test]
    public void Append_Zones_SkipsTheHeightOnlyOnesAndStopsAtCapacity()
    {
        Zone[] zones =
        {
            Make(ZoneKind.Hostile, Vector3.zero, 1f, 1f),
            Make(ZoneKind.Trample, Vector3.one, 1f, 1f),
            Make(ZoneKind.Heal, Vector3.zero, 1f, 1f),
            Make(ZoneKind.Range, Vector3.zero, 1f, 1f)
        };
        GroundStamp[] into = new GroundStamp[3];

        int written = ZoneStamps.Append(zones, into, 1);

        Assert.AreEqual(2, written);
        Assert.AreEqual(1f, into[1].shape.x, "The trample first.");
        Assert.AreEqual(ZoneStamps.HealOutward, into[2].push.w, 1e-6f);
        Assert.AreEqual(0, ZoneStamps.Append(zones, null, 0));
    }

    [Test]
    public void Heading_MatchesTheZonePackerEncoding()
    {
        Vector3 direction = new Vector3(-0.6f, 0f, 0.8f);

        Vector2 heading = ZoneStamps.Heading(ZonePacker.EncodeDirection(direction));

        Assert.That(Vector2.Distance(heading, new Vector2(-0.6f, 0.8f)), Is.LessThan(1e-4f));
    }
}

}
