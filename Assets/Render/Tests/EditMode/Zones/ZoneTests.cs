using System.Runtime.InteropServices;
using NUnit.Framework;

namespace HealerLike.Render.Zones
{

public class ZoneTests
{
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
}

}
