using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class ShapeMeshCacheTests
    {
        ShapeMeshCache _cache;

        [SetUp]
        public void SetUp() => _cache = new ShapeMeshCache();

        [TearDown]
        public void TearDown() => _cache.Dispose();

        [Test]
        public void Get_SameProfile_ReusesMeshAndReleasesItOnce()
        {
            Mesh a = _cache.Get(ShapeProfile.Leaf());
            Assert.AreSame(a, _cache.Get(ShapeProfile.Leaf(), 48));
            Mesh b = _cache.Get(ShapeProfile.Block(), 12);
            Assert.AreSame(b, _cache.Get(ShapeProfile.Block(), 12));
            Assert.AreNotSame(b, _cache.Get(ShapeProfile.Block(), 13));
            Assert.AreEqual(3, _cache.count);
            _cache.Dispose();
            _cache.Dispose();
            Assert.IsFalse(a);
            Assert.IsFalse(b);
            Assert.AreEqual(0, _cache.count);
        }

        [Test]
        public void Get_ChangedProfile_HasIndependentGeometryAndInvalidProfilesAreNotCached()
        {
            Mesh a = _cache.Get(ShapeProfile.Leaf(0.2f));
            Mesh b = _cache.Get(ShapeProfile.Leaf(0.8f));
            Assert.AreNotSame(a, b);
            CollectionAssert.AreNotEqual(a.vertices, b.vertices);
            ShapeProfile invalid = ShapeProfile.Bulb();
            invalid.fullness = float.NaN;
            Assert.IsNull(_cache.Get(invalid));
            Assert.IsNull(_cache.Get(default));
            Assert.AreEqual(2, _cache.count);
        }
    }
}
