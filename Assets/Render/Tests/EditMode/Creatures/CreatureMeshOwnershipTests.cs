using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class CreatureMeshOwnershipTests : CreatureMeshOwnershipFixture
    {
        [Test]
        public void BodyAndRoots_ReuseOneCopyAndReleaseItWithoutReleasingTheSource()
        {
            Mesh copy = BodyMesh();
            Assert.AreNotSame(_source, copy);
            CollectionAssert.AreEqual(_source.vertices, copy.vertices);
            CollectionAssert.AreEqual(_source.triangles, copy.triangles);
            foreach (MeshFilter filter in _rig.root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.name != "LianaArm")
                {
                    Assert.AreSame(copy, filter.sharedMesh, filter.name);
                }
            }

            _pool.Dispose();
            _rig.Dispose();
            _rig.Dispose();
            Assert.IsFalse(copy);
            Assert.IsTrue(_source);
        }

        [Test]
        public void Recompose_SameSourceEditedInPlace_RefreshesCopyAndReleasesPreviousGeneration()
        {
            for (int i = 0; i < 3; i++)
            {
                Mesh previous = BodyMesh();
                EditSource();
                Assert.IsTrue(_rig.Recompose(_recipe, _material, _material, _meshes));
                Assert.IsFalse(previous);
                Assert.AreNotSame(_source, BodyMesh());
                CollectionAssert.AreEqual(_source.vertices, BodyMesh().vertices);
            }
        }

        [Test]
        public void Recompose_MissingBorrowedMesh_KeepsCurrentGenerationAlive()
        {
            Mesh previous = BodyMesh();
            int copies = Copies();
            CreaturePart missing = _recipe.parts[0];
            missing.id = "Missing";
            missing.primitive = Primitive.Capsule;
            _recipe.parts = new[] { _recipe.parts[0], missing };
            Assert.IsFalse(_rig.Recompose(_recipe, _material, _material, _meshes));
            Assert.AreSame(previous, BodyMesh());
            Assert.IsTrue(previous);
            Assert.IsTrue(_source);
            Assert.AreEqual(copies, Copies(), "A rejected candidate must release any copies it already created.");
        }

        [Test]
        public void ProceduralParts_KeepTheirExistingSharedShapeOwner()
        {
            CreaturePart part = _recipe.parts[0];
            part.shape = ShapeProfile.Bulb();
            CreaturePart second = part;
            second.id = "Second";
            _recipe.parts = new[] { part, second };
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
        public void LiveBuilder_AlwaysProtectsBorrowedMeshesAndRefreshesThemOnRebuild()
        {
            _pool.Dispose();
            _rig.Dispose();
            Entity entity = RenderTestAssets.CreateStoneEntity(_owner, null);
            CreatureBuilder builder = _owner.AddComponent<CreatureBuilder>();
            RenderTestAssets.SetRecipe(builder, _recipe, _material, _meshes);
            builder.Init(entity);
            Mesh copy = builder.rig.partTransforms[0].GetComponent<MeshFilter>().sharedMesh;
            Assert.AreNotSame(_source, copy);
            EditSource();
            Assert.IsTrue(builder.Rebuild(null));
            Assert.IsFalse(copy);
            Mesh next = builder.rig.partTransforms[0].GetComponent<MeshFilter>().sharedMesh;
            CollectionAssert.AreEqual(_source.vertices, next.vertices);
            TestHelpers.InvokePrivate(builder, "OnDestroy");
            Assert.IsFalse(next);
            Assert.IsTrue(_source);
        }

        int Copies()
        {
            int count = 0;
            foreach (Mesh mesh in Resources.FindObjectsOfTypeAll<Mesh>())
            {
                if (mesh.name == _source.name && mesh != _source)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
