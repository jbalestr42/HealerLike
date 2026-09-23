using System.Runtime.InteropServices;
using NUnit.Framework;

namespace HealerLike.Render.Grass
{

public class BladeStateTests
{
    [Test]
    public void Stride_Struct_MatchesMarshalledSize()
    {
        Assert.AreEqual(BladeState.Stride, Marshal.SizeOf<BladeState>());
    }
}

}
