using System.Runtime.InteropServices;
using NUnit.Framework;

namespace HealerLike.Render.Grass
{

public class TuftStateTests
{
    [Test]
    public void Stride_Struct_MatchesMarshalledSize()
    {
        Assert.AreEqual(TuftState.Stride, Marshal.SizeOf<TuftState>());
    }
}

}
