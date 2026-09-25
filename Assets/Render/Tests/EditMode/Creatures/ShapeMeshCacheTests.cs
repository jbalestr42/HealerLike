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

        [Test]
        public void Get_BowAndRidgeEdits_ProduceIndependentCachedGeometry()
        {
            ShapeProfile leaf = ShapeProfile.Leaf();
            ShapeProfile bowed = leaf;
            bowed.bow = -0.9f;
            Assert.AreNotEqual(leaf, bowed);
            Assert.AreNotSame(_cache.Get(leaf), _cache.Get(bowed));
            CollectionAssert.AreNotEqual(_cache.Get(leaf).vertices, _cache.Get(bowed).vertices);
            ShapeProfile block = ShapeProfile.Block(fracture: 0.75f);
            ShapeProfile ridged = block;
            ridged.ridge = 0.7f;
            Assert.AreNotEqual(block, ridged);
            Assert.AreNotSame(_cache.Get(block, 17), _cache.Get(ridged, 17));
            CollectionAssert.AreNotEqual(_cache.Get(block, 17).vertices, _cache.Get(ridged, 17).vertices);
            Assert.AreSame(_cache.Get(ridged, 17), _cache.Get(ridged, 17));
        }
    }
}
