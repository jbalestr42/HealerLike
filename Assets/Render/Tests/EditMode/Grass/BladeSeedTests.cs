using System.Runtime.InteropServices;
using NUnit.Framework;

namespace HealerLike.Render.Grass
{

public class BladeSeedTests
{
    [Test]
    public void Stride_Struct_MatchesMarshalledSize()
    {
        Assert.AreEqual(BladeSeed.Stride, Marshal.SizeOf<BladeSeed>());
    }
}

}
