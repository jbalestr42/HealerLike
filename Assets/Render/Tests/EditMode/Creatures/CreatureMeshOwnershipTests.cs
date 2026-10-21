using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureMeshOwnershipTests : CreatureMeshOwnershipFixture
    {
        [Test]
        public void BodyAndRoots_BorrowSourceOutsideTheGameplayHierarchy()
        {
            Mesh mesh = BodyMesh();
            Assert.IsFalse(_rig.root.IsChildOf(_owner.transform));
            Assert.IsEmpty(_owner.GetComponentsInChildren<Renderer>(true));
            Assert.AreSame(_source, mesh);
            CollectionAssert.AreEqual(_source.vertices, mesh.vertices);
            CollectionAssert.AreEqual(_source.triangles, mesh.triangles);
            foreach (MeshFilter filter in _rig.root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.name != "LianaArm")
                {
                    Assert.AreSame(mesh, filter.sharedMesh, filter.name);
                }
            }

            _pool.Dispose();
            _rig.Dispose();
            _rig.Dispose();
            Assert.IsTrue(mesh);
            Assert.IsTrue(_source);
        }

        [Test]
        public void Recompose_SameSourceEditedInPlace_SeesTheCurrentMeshWithoutStaleCopies()
        {
            for (int i = 0; i < 3; i++)
            {
                Mesh previous = BodyMesh();
                EditSource();
                Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, _meshes));
                Assert.IsTrue(previous);
                Assert.AreSame(_source, BodyMesh());
                CollectionAssert.AreEqual(_source.vertices, BodyMesh().vertices);
            }
        }

        [Test]
        public void Recompose_MissingBorrowedMesh_KeepsCurrentGeometryAlive()
        {
            Mesh previous = BodyMesh();
            CreaturePart missing = _recipe.parts[0];
            missing.id = "Missing";
            missing.parent = 0;
            missing.primitive = Primitive.Capsule;
            _recipe.parts = new[] { _recipe.parts[0], missing };
            Assert.IsFalse(_rig.Recompose(_recipe, _material, _material, _meshes));
            Assert.AreSame(previous, BodyMesh());
            Assert.IsTrue(previous);
            Assert.IsTrue(_source);
            Assert.AreEqual(1, _rig.parts.Count);
        }

        [Test]
        public void ProceduralParts_KeepTheirExistingSharedShapeOwner()
        {
            CreaturePart part = _recipe.parts[0];
            part.shape = ShapeProfile.Bulb();
            CreaturePart second = part;
            second.id = "Second";
            second.parent = 0;
            _recipe.parts = new[] { part, second };
            Assert.IsTrue(CreatureValidator.TryValidate(_recipe, out string error), error);
            Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, _meshes));
            Mesh shape = BodyMesh();
            Assert.AreSame(shape, _rig.partTransforms[1].GetComponent<MeshFilter>().sharedMesh);
            Assert.AreNotSame(_source, shape);
            _pool.Dispose();
            _rig.Dispose();
            Assert.IsFalse(shape);
            Assert.IsTrue(_source);
        }

        [Test]
        public void LiveBuilder_DetachesGeometryAndReadsSameAssetEditsOnRebuild()
        {
            _pool.Dispose();
            _rig.Dispose();
            Entity entity = RenderTestAssets.CreateStoneEntity(_owner, null);
            CreatureBuilder builder = _owner.AddComponent<CreatureBuilder>();
            RenderTestAssets.SetRecipe(builder, _recipe, _material, _meshes);
            builder.Init(entity);
            Mesh mesh = builder.rig.partTransforms[0].GetComponent<MeshFilter>().sharedMesh;
            Assert.AreSame(_source, mesh);
            Assert.IsFalse(builder.rig.root.IsChildOf(_owner.transform));
            EditSource();
            Assert.IsTrue(builder.Rebuild(null));
            Assert.IsTrue(mesh);
            Mesh next = builder.rig.partTransforms[0].GetComponent<MeshFilter>().sharedMesh;
            CollectionAssert.AreEqual(_source.vertices, next.vertices);
            TestHelpers.InvokePrivate(builder, "OnDestroy");
            Assert.IsTrue(next);
            Assert.IsTrue(_source);
        }
    }
}
