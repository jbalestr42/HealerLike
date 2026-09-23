using NUnit.Framework;

namespace HealerLike.Render.Stones
{
    public class StoneMeshCacheTests
    {
        [Test]
        public void SharedUntilLastLeaseAndDisposalIdempotent()
        {
            StoneMeshCache cache = new StoneMeshCache();
            StoneMeshCache.Lease a = cache.Acquire(23, StonePresets.Boulder);
            StoneMeshCache.Lease b = cache.Acquire(23, StonePresets.Boulder);
            StoneMeshCache.Lease c = cache.Acquire(24, StonePresets.Boulder);
            Assert.AreSame(a.mesh, b.mesh);
            Assert.AreNotSame(a.mesh, c.mesh);
            Assert.AreEqual(2, cache.count);

            a.Dispose();
            a.Dispose();
            Assert.IsTrue(b.mesh != null);

            b.Dispose();
            Assert.IsTrue(b.mesh == null);
            Assert.AreEqual(1, cache.count);

            cache.Clear();
            Assert.IsTrue(c.mesh == null);
            Assert.AreEqual(0, cache.count);
            c.Dispose();
        }

        [Test]
        public void TwoCachesNeverShareMeshes()
        {
            StoneMeshCache first = new StoneMeshCache();
            StoneMeshCache second = new StoneMeshCache();
            StoneMeshCache.Lease a = first.Acquire(23, StonePresets.Boulder);
            StoneMeshCache.Lease b = second.Acquire(23, StonePresets.Boulder);

            Assert.AreNotSame(a.mesh, b.mesh);

            first.Clear();
            Assert.IsTrue(b.mesh != null);
            second.Clear();
        }
    }
}
