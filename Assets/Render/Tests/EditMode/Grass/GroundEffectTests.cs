using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GroundEffectTests
{
    static GroundStamp[] Write(GroundEffect effect, float age, float radius = 2f, bool isOneShot = false,
                               Vector2? to = null)
    {
        GroundStamp[] into = new GroundStamp[4];
        int count = effect.Write(into, 0, Vector2.zero, to ?? Vector2.zero, radius, 1f, age, isOneShot);
        GroundStamp[] written = new GroundStamp[count];
        System.Array.Copy(into, written, count);
        return written;
    }

    [Test]
    public void Write_Disc_HoldsFlattensAndMarksTheGrass()
    {
        GroundEffect effect = new GroundEffect { edge = 0.2f, hold = 0.5f, flatten = 1f, ash = 1f };

        GroundStamp[] stamps = Write(effect, 1f);

        Assert.AreEqual(2, stamps.Length, "What it holds and what it leaves; it throws nothing.");
        Assert.AreEqual(new Vector3(0.5f, 0f, 1f), stamps[0].Target(Vector2.right));
        Assert.AreEqual(1f, stamps[1].State(Vector2.zero).x);
    }

    [Test]
    public void Write_DiscKick_ThrowsTurnedFromOutward()
    {
        GroundEffect swirl = new GroundEffect { kick = 30f, kickTurn = Mathf.PI * 0.5f };

        GroundStamp[] stamps = Write(swirl, 1f);

        Assert.AreEqual(1, stamps.Length);
        Assert.That(stamps[0].Force(Vector2.right).y, Is.EqualTo(30f).Within(1e-3f), "A quarter turn spins.");
    }

    [Test]
    public void Write_GrowAndFadeIn_BloomTheDiscAndEaseItsStrength()
    {
        GroundEffect effect = new GroundEffect { grow = 0.4f, fadeIn = 0.2f, hold = 0.5f };

        GroundStamp[] early = Write(effect, 0.1f);
        GroundStamp[] half = Write(effect, 0.2f);

        Assert.AreEqual(0.5f, early[0].centreRadius.z, 1e-5f, "A quarter of the way to its radius.");
        Assert.AreEqual(0.25f, early[0].push.w, 1e-5f, "Half eased in.");
        Assert.AreEqual(1f, half[0].centreRadius.z, 1e-5f);
        Assert.AreEqual(0.5f, half[0].push.w, 1e-5f);
        Assert.AreEqual(0, Write(effect, 0f).Length, "Nothing before it starts.");
    }

    [Test]
    public void Write_Shiver_ReversesTheThrow()
    {
        GroundEffect warning = new GroundEffect { kick = 30f, shiver = 5f };

        float outward = Write(warning, 0.05f)[0].Force(Vector2.right).x;
        float inward = Write(warning, 0.15f)[0].Force(Vector2.right).x;

        Assert.AreEqual(30f, outward, 1e-2f);
        Assert.AreEqual(-30f, inward, 1e-2f);
    }

    [Test]
    public void Write_Ring_TravelsOutThenLetsGo()
    {
        GroundEffect ring = new GroundEffect
        {
            shape = GroundShape.Ring, grow = 0.35f, release = 0.1f, lifetime = 0.6f, band = 0.15f, minBand = 0.2f,
            kick = 170f
        };

        GroundStamp[] halfway = Write(ring, 0.175f);

        Assert.AreEqual(GroundStampKind.Front, halfway[0].kind);
        Assert.AreEqual(1f, halfway[0].centreRadius.z, 1e-4f);
        Assert.AreEqual(0.3f, halfway[0].centreRadius.w, 1e-5f, "Its band is a share of the radius, at least minBand.");
        Assert.AreEqual(170f, halfway[0].Force(Vector2.right).x, 0.5f);
        Assert.AreEqual(0, Write(ring, 0.5f).Length, "Arrived and let go.");
        Assert.IsTrue(ring.IsOver(0.5f));
        Assert.IsFalse(ring.IsOver(0.3f));
    }

    [Test]
    public void Write_Line_ThrowsAsideAndMarksAlongIt()
    {
        GroundEffect bolt = new GroundEffect { shape = GroundShape.Line, width = 0.2f, kick = 50f, ash = 1f };

        GroundStamp[] stamps = Write(bolt, 1f, 0f, false, new Vector2(3f, 0f));

        Assert.AreEqual(2, stamps.Length);
        Assert.Greater(stamps[0].Force(new Vector2(1f, 0.05f)).y, 1f);
        Assert.AreEqual(1f, stamps[1].State(new Vector2(1f, 0f)).x, 1e-5f);
        Assert.AreEqual(0, Write(bolt, 1f, 0f, false, Vector2.zero).Length, "A line of no length writes nothing.");
    }

    [Test]
    public void StrengthAt_OneShot_FadesOverItsLifetimeAndHeldDoesNot()
    {
        GroundEffect effect = new GroundEffect { lifetime = 0.6f };

        Assert.AreEqual(0.5f, effect.StrengthAt(0.3f, 1f, true), 1e-5f);
        Assert.AreEqual(1f, effect.StrengthAt(0.3f, 1f, false));
        Assert.AreEqual(0f, effect.StrengthAt(0.3f, float.NaN, true));
        Assert.IsTrue(effect.IsOver(0.6f));
    }

    [Test]
    public void Write_NoRoomLeft_WritesWhatFits()
    {
        GroundEffect effect = new GroundEffect { hold = 0.5f, ash = 1f };
        GroundStamp[] into = new GroundStamp[3];

        Assert.AreEqual(1, effect.Write(into, 2, Vector2.zero, Vector2.zero, 1f, 1f, 1f, false));
        Assert.AreEqual(0, effect.Write(into, 3, Vector2.zero, Vector2.zero, 1f, 1f, 1f, false));
        Assert.AreEqual(0, effect.Write(null, 0, Vector2.zero, Vector2.zero, 1f, 1f, 1f, false));
    }

    [Test]
    public void Clone_IsIndependent()
    {
        GroundEffect effect = new GroundEffect { kick = 10f };

        GroundEffect copy = effect.Clone();
        copy.kick = 99f;

        Assert.AreEqual(10f, effect.kick);
    }
}

}
