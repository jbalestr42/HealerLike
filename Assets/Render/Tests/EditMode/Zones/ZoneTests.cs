using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Zones
{

public class ZoneTests
{
    IntPtr _buffer;

    [SetUp]
    public void SetUp()
    {
        _buffer = Marshal.AllocHGlobal(Zone.Stride);
    }

    [TearDown]
    public void TearDown()
    {
        Marshal.FreeHGlobal(_buffer);
    }

    [Test]
    public void Stride_Struct_IsThirtyTwoBytes()
    {
        Assert.AreEqual(32, Zone.Stride);
        Assert.AreEqual(32, Marshal.SizeOf<Zone>());
    }

    [Test]
    public void Stride_Struct_MatchesTheFrozenShaderLayout()
    {
        string[] names = { "position", "radius", "kind", "strength", "age", "reserved" };
        int[] offsets = { 0, 12, 16, 20, 24, 28 };

        Assert.AreEqual(Zone.Stride, Marshal.SizeOf<Zone>());
        Assert.AreEqual(32, Zone.Stride);
        for (int i = 0; i < names.Length; i++)
        {
            Assert.AreEqual(offsets[i], Marshal.OffsetOf<Zone>(names[i]).ToInt32());
        }
    }

    [Test]
    public void StructureToPtr_Zone_WritesEachFieldAtItsWireOffset()
    {
        Zone zone = new Zone
        {
            position = new Vector3(1f, 2f, 3f),
            radius = 4f,
            kind = (int)ZoneKind.Hostile,
            strength = 0.5f,
            age = 6f,
            reserved = 0u
        };
        byte[] bytes = new byte[Zone.Stride];

        Marshal.StructureToPtr(zone, _buffer, false);
        Marshal.Copy(_buffer, bytes, 0, Zone.Stride);

        Assert.AreEqual(1f, BitConverter.ToSingle(bytes, 0), 0f, "position.x at byte 0");
        Assert.AreEqual(2f, BitConverter.ToSingle(bytes, 4), 0f, "position.y at byte 4");
        Assert.AreEqual(3f, BitConverter.ToSingle(bytes, 8), 0f, "position.z at byte 8");
        Assert.AreEqual(4f, BitConverter.ToSingle(bytes, 12), 0f, "radius at byte 12");
        Assert.AreEqual(2, BitConverter.ToInt32(bytes, 16), "kind at byte 16");
        Assert.AreEqual(0.5f, BitConverter.ToSingle(bytes, 20), 0f, "strength at byte 20");
        Assert.AreEqual(6f, BitConverter.ToSingle(bytes, 24), 0f, "age at byte 24");
        Assert.AreEqual(0u, BitConverter.ToUInt32(bytes, 28), "reserved at byte 28");
    }
}

}
