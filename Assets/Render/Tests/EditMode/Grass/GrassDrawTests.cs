using HealerLike.Render.Creatures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{
    public class GrassDrawTests : AGrassDrawFixture
    {
        [Test]
        public void Show_OwnerDestroyed_StopsDrawingInsteadOfThrowing()
        {
            _scene.BuildKeyLight(20f, 4f);
            GameObject owner = new GameObject("owner");
            GraphicsBuffer arguments = _draw.arguments;
            _draw.Show(_scene.camera, () => true, owner);

            Object.DestroyImmediate(owner);

            Assert.DoesNotThrow(() => _scene.Render());
            Assert.IsFalse(arguments.IsValid(), "A destroyed owner cannot leave the draw buffer allocated.");
            Assert.IsNull(_draw.arguments);
            Assert.DoesNotThrow(() => _scene.Render());
        }

        [Test]
        public void Constructor_Mesh_WritesIndexAndInstanceCounts()
        {
            uint[] data = new uint[5];

            _draw.arguments.GetData(data);

            Assert.AreEqual(_mesh.GetIndexCount(0), data[0]);
            Assert.AreEqual(7u, data[1]);
            Assert.AreEqual((uint)FacetedMeshes.TuftIndexCount, data[0]); // four sides
        }

        [Test]
        public void Constructor_Material_KeepsMaterialAndCreatesProperties()
        {
            Assert.AreSame(_material, _draw.material);
            Assert.IsNotNull(_draw.properties);
        }

        [Test]
        public void Constructor_Default_CastsNoShadow()
        {
            Assert.AreEqual(ShadowCastingMode.Off, _draw.shadowCastingMode);
        }

        [Test]
        public void ShadowCastingMode_On_IsKept()
        {
            _draw.shadowCastingMode = ShadowCastingMode.On;

            Assert.AreEqual(ShadowCastingMode.On, _draw.shadowCastingMode);
        }

        [Test]
        public void Release_OwnedResources_DisposesArgumentsButNotMeshOrMaterial()
        {
            GraphicsBuffer arguments = _draw.arguments;

            _draw.Release();

            Assert.IsFalse(arguments.IsValid());
            Assert.IsTrue(_material != null);
            Assert.IsTrue(_mesh != null);
            Assert.IsNull(_draw.arguments);
        }
    }
}
