using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class ZonePackerTests
{
    static Zone Raw(Vector3 position, float radius, ZoneKind kind, float strength, float age = 1f, uint reserved = 0u)
    {
        return new Zone
        {
            position = position,
            radius = radius,
            kind = (int)kind,
            strength = strength,
            age = age,
            reserved = reserved
        };
    }

    [TestCase(ZoneKind.Range, 3)]
    [TestCase(ZoneKind.Bruise, 4)]
    public void TryCreate_LaterKinds_KeepTheirWireValuesAndAreAccepted(ZoneKind kind, int value)
    {
        Assert.AreEqual(value, (int)kind);
        Assert.IsTrue(ZonePacker.TryCreate(Vector3.zero, 1f, kind, 1f, 0f, out _));
    }

    [Test]
    public void Pack_Reserved_IsAlwaysCleared()
    {
        Zone[] source = { Raw(Vector3.zero, 2f, ZoneKind.Range, 1f, reserved: 123u) };
        Zone[] destination = new Zone[1];

        ZonePacker.Pack(source, destination, out _, out _);

        Assert.AreEqual(0u, destination[0].reserved);
    }

    [Test]
    public void TryCreate_ValidZone_CopiesEveryField()
    {
        bool created = ZonePacker.TryCreate(new Vector3(3f, 1f, -2f), 2.5f, ZoneKind.Heal, 0.25f, 4f,
                                              out Zone zone);

        Assert.IsTrue(created);
        Assert.AreEqual(new Vector3(3f, 1f, -2f), zone.position);
        Assert.AreEqual(2.5f, zone.radius);
        Assert.AreEqual((int)ZoneKind.Heal, zone.kind);
        Assert.AreEqual(0.25f, zone.strength);
        Assert.AreEqual(4f, zone.age);
        Assert.AreEqual(0u, zone.reserved);
    }

    [Test]
    public void TryCreate_OutOfRangeStrengthAndAge_ClampsAndClearsReserved()
    {
        Assert.IsTrue(ZonePacker.TryCreate(Vector3.zero, 1f, ZoneKind.Heal, 7f, -3f, out Zone high));
        Assert.AreEqual(1f, high.strength, "strength clamps to 1");
        Assert.AreEqual(0f, high.age, "negative age clamps to 0");
        Assert.AreEqual(0u, high.reserved);

        Assert.IsTrue(ZonePacker.TryCreate(Vector3.zero, 1f, ZoneKind.Heal, -2f, 0f, out Zone low));
        Assert.AreEqual(0f, low.strength, "strength clamps to 0");
    }

    [Test]
    public void TryCreate_NonPositiveRadius_ReturnsFalse()
    {
        Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, 0f, ZoneKind.Heal, 1f, 0f, out _));
        Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, -1f, ZoneKind.Heal, 1f, 0f, out _));
    }

    [Test]
    public void TryCreate_NoneOrUnknownKind_ReturnsFalse()
    {
        Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, 1f, ZoneKind.None, 1f, 0f, out _));
        Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, 1f, (ZoneKind)99, 1f, 0f, out _));
    }

    [Test]
    public void TryCreate_NonFiniteField_ReturnsFalse()
    {
        Vector3 nanPosition = new Vector3(float.NaN, 0f, 0f);
        Vector3 infinitePosition = new Vector3(0f, 0f, float.PositiveInfinity);
        float infinity = float.PositiveInfinity;

        Assert.IsFalse(ZonePacker.TryCreate(nanPosition, 1f, ZoneKind.Heal, 1f, 0f, out _), "NaN position");
        Assert.IsFalse(ZonePacker.TryCreate(infinitePosition, 1f, ZoneKind.Heal, 1f, 0f, out _), "infinite position");
        Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, float.NaN, ZoneKind.Heal, 1f, 0f, out _), "NaN radius");
        Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, infinity, ZoneKind.Heal, 1f, 0f, out _), "infinite radius");
        Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, 1f, ZoneKind.Heal, float.NaN, 0f, out _), "NaN strength");
        Assert.IsFalse(ZonePacker.TryCreate(Vector3.zero, 1f, ZoneKind.Heal, 1f, float.NaN, out _), "NaN age");
    }

    [Test]
    public void TryCreate_Rejected_OutputsDefaultZone()
    {
        bool created = ZonePacker.TryCreate(new Vector3(5f, 5f, 5f), -1f, ZoneKind.Heal, 1f, 2f,
                                              out Zone zone);

        Assert.IsFalse(created);
        Assert.AreEqual(default(Zone), zone);
    }

    [Test]
    public void Pack_ValidZones_KeepsRegistrationOrder()
    {
        Zone[] source =
        {
            Raw(new Vector3(1f, 0f, 0f), 1f, ZoneKind.Heal, 1f),
            Raw(new Vector3(2f, 0f, 0f), 1f, ZoneKind.Hostile, 1f),
            Raw(new Vector3(3f, 0f, 0f), 1f, ZoneKind.Heal, 1f)
        };
        Zone[] destination = new Zone[8];

        int written = ZonePacker.Pack(source, destination, out int rejected, out int overflow);

        Assert.AreEqual(3, written);
        Assert.AreEqual(0, rejected);
        Assert.AreEqual(0, overflow);
        Assert.AreEqual(1f, destination[0].position.x);
        Assert.AreEqual(2f, destination[1].position.x);
        Assert.AreEqual(3f, destination[2].position.x);
    }

    [Test]
    public void Pack_OutOfRangeItem_ClampsAndClearsReserved()
    {
        Zone[] source = { Raw(Vector3.zero, 2f, ZoneKind.Heal, 5f, -1f, reserved: 0xDEADBEEF) };
        Zone[] destination = new Zone[4];

        Assert.AreEqual(1, ZonePacker.Pack(source, destination, out _, out _));
        Assert.AreEqual(1f, destination[0].strength);
        Assert.AreEqual(0f, destination[0].age);
        Assert.AreEqual(0u, destination[0].reserved, "reserved is always cleared on the wire");
    }

    [Test]
    public void Pack_InvalidItems_CountsThemRejected()
    {
        Zone[] source =
        {
            Raw(Vector3.zero, 1f, ZoneKind.Heal, 1f),
            Raw(Vector3.zero, 0f, ZoneKind.Heal, 1f),
            Raw(Vector3.zero, 1f, ZoneKind.None, 1f),
            Raw(new Vector3(float.NaN, 0f, 0f), 1f, ZoneKind.Heal, 1f)
        };
        Zone[] destination = new Zone[8];

        int written = ZonePacker.Pack(source, destination, out int rejected, out int overflow);

        Assert.AreEqual(1, written);
        Assert.AreEqual(3, rejected);
        Assert.AreEqual(0, overflow);
    }

    [Test]
    public void Pack_ZeroStrengthItems_OmitsThemWithoutRejecting()
    {
        Zone[] source =
        {
            Raw(Vector3.zero, 1f, ZoneKind.Heal, 0f),
            Raw(Vector3.zero, 1f, ZoneKind.Heal, -0.5f),
            Raw(Vector3.zero, 1f, ZoneKind.Heal, 0.1f)
        };
        Zone[] destination = new Zone[8];

        int written = ZonePacker.Pack(source, destination, out int rejected, out int overflow);

        Assert.AreEqual(1, written);
        Assert.AreEqual(0, rejected, "an inactive zone is not an error");
        Assert.AreEqual(0, overflow);
        Assert.AreEqual(0.1f, destination[0].strength);
    }

    [Test]
    public void Pack_SeventyZones_StopsAtSixtyFourAndReportsOverflow()
    {
        Zone[] source = new Zone[70];
        for (int i = 0; i < source.Length; i++)
        {
            source[i] = Raw(new Vector3(i, 0f, 0f), 1f, ZoneKind.Heal, 1f);
        }
        Zone[] destination = new Zone[ZonePacker.MaxZones];

        int written = ZonePacker.Pack(source, destination, out int rejected, out int overflow);

        Assert.AreEqual(64, written);
        Assert.AreEqual(0, rejected);
        Assert.AreEqual(6, overflow);
        Assert.AreEqual(0f, destination[0].position.x, "the first valid entries win");
        Assert.AreEqual(63f, destination[63].position.x);
    }

    [Test]
    public void Pack_SmallDestination_NeverWritesPastIt()
    {
        Zone[] source = new Zone[10];
        for (int i = 0; i < source.Length; i++)
        {
            source[i] = Raw(new Vector3(i, 0f, 0f), 1f, ZoneKind.Hostile, 1f);
        }
        Zone[] destination = new Zone[4];

        int written = ZonePacker.Pack(source, destination, out int rejected, out int overflow);

        Assert.AreEqual(4, written);
        Assert.AreEqual(0, rejected);
        Assert.AreEqual(6, overflow);
    }

    [Test]
    public void Pack_FewerZones_ZeroesTheUnusedTail()
    {
        Zone[] destination = new Zone[4];
        for (int i = 0; i < destination.Length; i++)
        {
            destination[i] = Raw(new Vector3(9f, 9f, 9f), 9f, ZoneKind.Hostile, 1f, 9f);
        }
        Zone[] source = { Raw(Vector3.one, 1f, ZoneKind.Heal, 1f) };

        int written = ZonePacker.Pack(source, destination, out _, out _);

        Assert.AreEqual(1, written);
        for (int i = written; i < destination.Length; i++)
        {
            Assert.AreEqual(default(Zone), destination[i], "stale slot " + i + " was not cleared");
        }
    }

    [Test]
    public void Pack_EmptySource_WritesNothingAndClears()
    {
        Zone[] destination = new Zone[2];
        destination[0] = Raw(Vector3.one, 1f, ZoneKind.Heal, 1f);

        int written = ZonePacker.Pack(ReadOnlySpan<Zone>.Empty, destination, out int rejected,
                                        out int overflow);

        Assert.AreEqual(0, written);
        Assert.AreEqual(0, rejected);
        Assert.AreEqual(0, overflow);
        Assert.AreEqual(default(Zone), destination[0]);
    }

    [Test]
    public void Pack_SameInput_IsDeterministic()
    {
        Zone[] source =
        {
            Raw(new Vector3(1f, 0f, 1f), 2f, ZoneKind.Hostile, 0.7f, 3f),
            Raw(new Vector3(0f, 0f, 0f), 0f, ZoneKind.Heal, 1f),
            Raw(new Vector3(4f, 0f, 2f), 1f, ZoneKind.Heal, 0.3f, 1f)
        };
        Zone[] first = new Zone[8];
        Zone[] second = new Zone[8];

        int a = ZonePacker.Pack(source, first, out int rejectedA, out int overflowA);
        int b = ZonePacker.Pack(source, second, out int rejectedB, out int overflowB);

        Assert.AreEqual(a, b);
        Assert.AreEqual(rejectedA, rejectedB);
        Assert.AreEqual(overflowA, overflowB);
        for (int i = 0; i < first.Length; i++)
        {
            Assert.AreEqual(first[i], second[i], "slot " + i);
        }
    }

    [Test]
    public void MaxZones_ZoneDataInclude_EqualsTheShaderCapacity()
    {
        string include = File.ReadAllText("Assets/Render/Shaders/ZoneData.hlsl");

        Match define = Regex.Match(include, @"#define\s+HL_MAX_ZONES\s+(\d+)");

        Assert.IsTrue(define.Success);
        Assert.AreEqual(ZonePacker.MaxZones, int.Parse(define.Groups[1].Value));
    }
}

}
