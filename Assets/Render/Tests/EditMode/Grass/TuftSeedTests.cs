using System.Runtime.InteropServices;
using NUnit.Framework;

namespace HealerLike.Render.Grass
{

public class TuftSeedTests
{
    [Test]
    public void Stride_Struct_MatchesMarshalledSize()
    {
        Assert.AreEqual(TuftSeed.Stride, Marshal.SizeOf<TuftSeed>());
    }
}

}
