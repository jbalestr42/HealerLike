using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public class StoneGroundMeshOwnershipTests : CreatureMeshOwnershipFixture
    {
        [Test]
        public void Attach_TransfersShadowInstanceButKeepsItsMeshBorrowedAcrossSameAssetEdits()
        {
            StoneGroundDisc disc = RenderTestAssets.CreateGroundDisc(_owner.transform, true);
            MeshFilter filter = disc.GetComponent<MeshFilter>();
            filter.sharedMesh = _source;
            disc.Attach(_attachment);
            disc.Init(new Bounds(Vector3.zero, Vector3.one), Vector3.up, null);
            Assert.IsFalse(disc.transform.IsChildOf(_owner.transform));
            Assert.AreSame(_source, filter.sharedMesh);
            EditSource();
            disc.Init(new Bounds(Vector3.zero, Vector3.one), Vector3.up, null);
            CollectionAssert.AreEqual(_source.vertices, filter.sharedMesh.vertices);
            _attachment.Dispose();
            Assert.IsFalse(disc);
            Assert.IsTrue(_source);
        }
    }
}
