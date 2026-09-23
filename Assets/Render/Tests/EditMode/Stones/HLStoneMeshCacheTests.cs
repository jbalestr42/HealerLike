using NUnit.Framework;

namespace HealerLike.Render.Stones
{
    public class HLStoneMeshCacheTests
    {
        [Test]
        public void SharedUntilLastLeaseAndDisposalIdempotent()
        {
            HLStoneMeshCache.Lease a = HLStoneMeshCache.Acquire(23, HLStonePresets.Boulder);
            HLStoneMeshCache.Lease b = HLStoneMeshCache.Acquire(23, HLStonePresets.Boulder);
            HLStoneMeshCache.Lease c = HLStoneMeshCache.Acquire(24, HLStonePresets.Boulder);
            Assert.AreSame(a.mesh, b.mesh);
            Assert.AreNotSame(a.mesh, c.mesh);

            a.Dispose();
            a.Dispose();
            Assert.IsTrue(b.mesh != null);

            b.Dispose();
            Assert.IsTrue(b.mesh == null);
            c.Dispose();
        }
    }
}
