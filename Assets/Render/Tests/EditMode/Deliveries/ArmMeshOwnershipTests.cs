using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Deliveries
{
    public class ArmMeshOwnershipTests : CreatureMeshOwnershipFixture
    {
        [Test]
        public void Recompose_HeldArmKeepsItsCopyUntilRestAndTheNextArmReadsEditedSource()
        {
            Tick(0f);
            Assert.IsTrue(_pool.BeginDelivery(41, DeliveryStyle.Direct, null, Vector3.one));
            Tick(0.2f);
            Mesh held = TipMesh();
            Assert.IsTrue(held);
            Assert.AreNotSame(_source, held);
            Vector3[] original = held.vertices;
            EditSource();
            Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, _meshes));
            _pool.Refresh();
            Tick(0.016f);
            Assert.AreSame(held, TipMesh());
            CollectionAssert.AreEqual(original, held.vertices);
            CollectionAssert.AreEqual(_source.vertices, BodyMesh().vertices);

            _pool.EndDelivery(41);
            Tick(0.3f);
            Tick(0f);
            Assert.IsFalse(held, "The refreshed arm releases its own obsolete tip copy at rest.");
            Assert.IsTrue(_pool.BeginDelivery(42, DeliveryStyle.Direct, null, Vector3.one));
            Tick(0.2f);
            Mesh next = TipMesh();
            Assert.IsTrue(next);
            Assert.AreNotSame(_source, next);
            CollectionAssert.AreEqual(_source.vertices, next.vertices);
            _pool.Dispose();
            Assert.IsFalse(next);
            Assert.IsTrue(_source);
        }
    }
}
