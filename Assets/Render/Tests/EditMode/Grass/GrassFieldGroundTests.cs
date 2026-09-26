using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    public class GrassFieldGroundTests : AGrassFieldFixture
    {
        [Test]
        public void UpdateField_GroundShader_OwnsAndPublishesTheGroundAroundTheField()
        {
            if (!HasGraphicsDevice() || !GroundSimulation.IsSupported())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }

            BuildWithRegistry(true);

            Assert.IsNotNull(_field.simulation);
            Assert.IsTrue(_field.simulation.isValid);
            Assert.AreEqual(1, _field.simulation.stampCount, "The obstacle became a stamp.");
            Rect area = _field.simulation.volume.area;
            Assert.AreEqual(-3.5f, area.xMin, 1e-5f, "Three cells of margin around the one-cell field.");
            Assert.AreEqual(7f, area.width, 1e-5f);
            Assert.AreEqual(1f, Shader.GetGlobalFloat(GroundSimulation.ActiveId));
            Assert.AreSame(_field.simulation.motion, Shader.GetGlobalTexture(GroundSimulation.MotionId));

            TestHelpers.InvokePrivate(_field, "OnDisable");

            Assert.IsNull(_field.simulation);
            Assert.AreEqual(0f, Shader.GetGlobalFloat(GroundSimulation.ActiveId), "A released ground is unpublished.");
        }

        [Test]
        public void UpdateField_TrampleOverTime_FlattensTheTuftsUnderIt()
        {
            if (!HasGraphicsDevice() || !GroundSimulation.IsSupported())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }

            ZoneRegistry registry = BuildWithRegistry(true);
            for (int frame = 1; frame < 30; frame++)
            {
                registry.PublishFrame(1f / 60f);
                _ground.Advance(1f / 60f);
                _field.UpdateField(registry, _ground, 1f / 60f, frame / 60f);
            }

            GraphicsBuffer seeds = Batch().seeds;
            GraphicsBuffer states = Batch().states;
            TuftSeed[] seedData = new TuftSeed[_field.tuftCount];
            TuftState[] stateData = new TuftState[_field.tuftCount];
            seeds.GetData(seedData);
            states.GetData(stateData);
            int flattened = 0;
            for (int i = 0; i < seedData.Length; i++)
            {
                Vector4 root = seedData[i].positionYaw;
                if (new Vector2(root.x, root.z).magnitude < 0.3f)
                {
                    float height = seedData[i].heightWidthLean.x * stateData[i].leanHeightSpike.z;
                    Assert.Less(height, 0.2f, $"Tuft {i} at {root} still stands {height} tall.");
                    flattened++;
                }
            }

            Assert.Greater(flattened, 0);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UpdateField_NewLayout_RebuildsTheTuftsButKeepsTheGround(bool changeSegments)
        {
            if (!HasGraphicsDevice() || !GroundSimulation.IsSupported())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }

            ZoneRegistry registry = BuildWithRegistry(true);
            GroundSimulation ground = _field.simulation;

            GraphicsBuffer previous = _field.tuftDraw.arguments;
            if (changeSegments)
            {
                _field.bladeSegments = 2;
            }
            else
            {
                _field.tuftBudget = 20;
            }
            _field.UpdateField(registry, _ground, 1f / 60f, 1f);

            Assert.AreSame(ground, _field.simulation, "Its motion and state live on.");
            Assert.IsFalse(previous.IsValid());
            Assert.IsTrue(_field.isReady);
            Assert.AreEqual(1f, Shader.GetGlobalFloat(GroundSimulation.ActiveId));
        }

        [Test]
        public void UpdateField_NoGroundShader_OnlyReadsTheGround()
        {
            if (!HasGraphicsDevice())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }

            GroundSimulation.Unpublish();
            BuildWithRegistry(false);

            Assert.IsNull(_field.simulation);
            Assert.IsTrue(_field.isReady);
            Assert.AreEqual(0f, Shader.GetGlobalFloat(GroundSimulation.ActiveId));
        }
    }
}
