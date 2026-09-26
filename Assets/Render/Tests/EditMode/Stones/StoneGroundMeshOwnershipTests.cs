using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class StoneGroundMeshOwnershipTests : CreatureMeshOwnershipFixture
    {
        [Test]
        public void Init_CopiesThePrefabMeshAndRefreshesSameAssetEditsWithoutLeakingTheOldCopy()
        {
            StoneGroundDisc disc = RenderTestAssets.CreateGroundDisc(_owner.transform, true);
            MeshFilter filter = disc.GetComponent<MeshFilter>();
            filter.sharedMesh = _source;
            disc.Init(new Bounds(Vector3.zero, Vector3.one), Vector3.up, null);
            Mesh first = filter.sharedMesh;
            Assert.AreNotSame(_source, first);
            EditSource();
            disc.Init(new Bounds(Vector3.zero, Vector3.one), Vector3.up, null);
            Mesh next = filter.sharedMesh;
            Assert.IsFalse(first);
            CollectionAssert.AreEqual(_source.vertices, next.vertices);
            TestHelpers.InvokePrivate(disc, "OnDestroy");
            Assert.IsFalse(next);
            Assert.AreSame(_source, filter.sharedMesh);
            Assert.IsTrue(_source);
        }
    }
}
