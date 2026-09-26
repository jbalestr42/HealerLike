using System.Collections.Generic;
using System.Text.RegularExpressions;
using HealerLike.Render.Creatures;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace HealerLike.Render.Grass
{
    public class GrassFieldLifetimeTests : AGrassFieldFixture
    {
        [Test]
        public void Release_CalledTwice_StaysReleased()
        {
            _field.Release();
            _field.Release();

            Assert.IsFalse(_field.isReady);
            Assert.AreEqual(0, _field.tuftCount);
            Assert.AreEqual(0, _field.activeZoneCount);
        }

        [Test]
        public void Build_OnGraphicsDevice_DrawsTuftsAndSoclesWithTheLookShader()
        {
            if (!HasGraphicsDevice())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }

            BuildOneCellField();

            Assert.IsTrue(_field.isReady);
            Assert.AreEqual(9, _field.tuftCount); // 3 by 3 at the wider spacing, below the budget of 65
            Assert.AreEqual("HL/Look/Primitive", _field.tuftDraw.material.shader.name);
            Assert.AreSame(_field.tuftDraw.material, _field.socleDraw.material);
            Assert.IsTrue(_field.tuftDraw.material.IsKeywordEnabled(instancedKeyword));
            uint[] data = new uint[5];
            _field.tuftDraw.arguments.GetData(data);
            Assert.AreEqual(GrassBladeMesh.Shared(_field.bladeSegments).GetIndexCount(0), data[0]); // four bent sides
            _field.socleDraw.arguments.GetData(data);
            Assert.AreEqual((uint)FacetedMeshes.SocleIndexCount, data[0]); // eight fan triangles
            Assert.AreEqual(ShadowCastingMode.On, _field.tuftDraw.shadowCastingMode);
            Assert.AreEqual(0f, _field.tuftDraw.properties.GetFloat("_HLGrassSpikeShadowsOnly"));
            Assert.AreEqual(ShadowCastingMode.Off, _field.socleDraw.shadowCastingMode);
            Assert.AreEqual(1f, _field.tuftDraw.properties.GetFloat("_HLTuftLean"));
            Assert.AreEqual(0f, _field.socleDraw.properties.GetFloat("_HLTuftLean"));
        }

        [Test]
        public void OnDisable_BuiltField_ReleasesItsBuffersAndKeepsBorrowedZonesAndMaterials()
        {
            if (!HasGraphicsDevice())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }

            for (int cycle = 0; cycle < 3; cycle++)
            {
                BuildOneCellField();
                List<GraphicsBuffer> buffers = OwnedBuffers();
                Material tuftMaterial = _field.tuftDraw.material;

                // Runtime messages do not run on their own in EditMode
                TestHelpers.InvokePrivate(_field, "OnDisable");

                foreach (GraphicsBuffer buffer in buffers)
                {
                    Assert.IsFalse(buffer.IsValid());
                }
                Assert.IsTrue(tuftMaterial != null);
                Assert.IsTrue(_borrowedZones.IsValid());
                Assert.IsFalse(_field.isReady);
                Assert.IsNull(_field.tuftDraw);
                Assert.IsNull(_field.socleDraw);
            }
        }

        [Test]
        public void OnDestroy_BuiltField_ReleasesItsBuffers()
        {
            if (!HasGraphicsDevice())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }
            BuildOneCellField();
            List<GraphicsBuffer> buffers = OwnedBuffers();

            TestHelpers.InvokePrivate(_field, "OnDestroy");
            Object.DestroyImmediate(_field);

            foreach (GraphicsBuffer buffer in buffers)
            {
                Assert.IsFalse(buffer.IsValid());
            }
            Assert.IsTrue(_borrowedZones.IsValid());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UpdateField_DisabledOwner_StaysReleasedUntilEnabled(bool disableObject)
        {
            if (!HasGraphicsDevice() || !GroundSimulation.IsSupported())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }

            ZoneRegistry registry = BuildWithRegistry(true);
            List<GraphicsBuffer> buffers = OwnedBuffers();
            GroundSimulation previous = _field.simulation;
            if (disableObject)
            {
                _go.SetActive(false);
            }
            else
            {
                _field.enabled = false;
            }
            TestHelpers.InvokePrivate(_field, "OnDisable");

            _field.UpdateField(registry, _ground);
            _field.UpdateField(registry.buffer, 0);

            Assert.IsNull(_field.simulation);
            Assert.IsFalse(_field.isReady);
            Assert.IsFalse(previous.isValid);
            foreach (GraphicsBuffer buffer in buffers)
            {
                Assert.IsFalse(buffer.IsValid());
            }
            Assert.IsTrue(registry.buffer.IsValid());

            _go.SetActive(true);
            _field.enabled = true;
            _field.UpdateField(registry, _ground);

            Assert.IsTrue(_field.isReady);
            Assert.IsNotNull(_field.simulation);
            Assert.AreNotSame(previous, _field.simulation);
        }

        [Test]
        public void UpdateField_InvalidAssets_DisablesOnceAndCanRebuildAfterRepair()
        {
            if (!HasGraphicsDevice())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }

            BuildOneCellField();
            List<GraphicsBuffer> buffers = OwnedBuffers();
            TestHelpers.InvokePrivate(_field, "OnDisable");
            TestHelpers.SetPrivateField(_field, "_meshes", null);
            LogAssert.Expect(LogType.Error, new Regex("\\[GrassField\\] Grass needs compute"));

            _field.UpdateField(_borrowedZones, 0);
            _field.UpdateField(_borrowedZones, 0);

            Assert.IsFalse(_field.enabled);
            Assert.IsFalse(_field.isReady);
            foreach (GraphicsBuffer buffer in buffers)
            {
                Assert.IsFalse(buffer.IsValid());
            }
            Assert.IsTrue(_borrowedZones.IsValid());

            SetAssets();
            _field.enabled = true;
            _field.UpdateField(_borrowedZones, 0);

            Assert.IsTrue(_field.isReady);
            Assert.IsTrue(_field.tuftDraw.arguments.IsValid());
        }

        [Test]
        public void UpdateField_ReleasedOwner_RequiresAnotherInit()
        {
            if (!HasGraphicsDevice())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }
            BuildOneCellField();

            _field.Release();
            _field.UpdateField(_borrowedZones, 0);
            _field.enabled = false;
            _field.enabled = true;
            _field.UpdateField(_borrowedZones, 0);

            Assert.IsFalse(_field.isReady);
            Assert.IsNull(_field.tuftDraw);
            Assert.IsTrue(_borrowedZones.IsValid());

            BuildOneCellField();
            Assert.IsTrue(_field.isReady);
        }
    }
}
