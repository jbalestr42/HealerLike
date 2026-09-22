using NUnit.Framework;
namespace HealerLike.Render.Stones
{
    public class HLStoneMeshCacheTests
    {
        [Test] public void SharedUntilLastLeaseAndDisposalIdempotent()
        {
            var a=HLStoneMeshCache.Acquire(23,HLStonePresets.Boulder); var b=HLStoneMeshCache.Acquire(23,HLStonePresets.Boulder);
            var c=HLStoneMeshCache.Acquire(24,HLStonePresets.Boulder);
            Assert.AreSame(a.Mesh,b.Mesh); Assert.AreNotSame(a.Mesh,c.Mesh);
            a.Dispose(); a.Dispose(); Assert.IsTrue(b.Mesh!=null); b.Dispose(); Assert.IsTrue(b.Mesh==null); c.Dispose();
        }
    }
}
