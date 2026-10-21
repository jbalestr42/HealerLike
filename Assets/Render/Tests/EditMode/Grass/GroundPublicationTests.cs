using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grass
{
    public class GroundPublicationTests
    {
        GroundSimulation _first;
        GroundSimulation _second;
        GroundTestGlobals _globals;

        [SetUp]
        public void SetUp()
        {
            if (!GroundSimulation.IsSupported())
            {
                Assert.Ignore("Requires a graphics device; run with -force-metal.");
            }
            _globals = new GroundTestGlobals();
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/GroundSimulation.shader");
            GroundVolume volume = GroundVolume.Create(new Rect(0f, 0f, 1f, 1f), 0.125f);
            _first = new GroundSimulation(shader, volume, GroundSpringSettings.Default);
            _second = new GroundSimulation(shader, volume, GroundSpringSettings.Default);
            Assert.IsTrue(_first.isValid);
            Assert.IsTrue(_second.isValid);
        }

        [TearDown]
        public void TearDown()
        {
            _first?.Dispose();
            _second?.Dispose();
            _globals?.Dispose();
            _globals = null;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Dispose_CurrentPublisher_ReleasesTexturesAndUnpublishes(bool advanceAfterPublish)
        {
            _first.Publish();
            RenderTexture motion = _first.motion;
            RenderTexture crush = _first.crush;
            RenderTexture state = _first.state;

            if (advanceAfterPublish)
            {
                _first.Step(1f / 120f, Vector4.zero);
                Assert.AreNotSame(motion, _first.motion);
                Assert.AreSame(motion, Shader.GetGlobalTexture(GroundSimulation.MotionId));
            }

            _first.Dispose();
            _first.Dispose();

            Assert.IsFalse(_first.isValid);
            Assert.IsNull(_first.motion);
            Assert.IsTrue(motion == null);
            Assert.IsTrue(crush == null);
            Assert.IsTrue(state == null);
            Assert.AreEqual(0f, Shader.GetGlobalFloat(GroundSimulation.ActiveId));
        }

        [Test]
        public void Dispose_OlderPublisher_KeepsTheNewOwnerPublished()
        {
            _first.Publish();
            _second.Publish();

            _first.Dispose();

            Assert.AreEqual(1f, Shader.GetGlobalFloat(GroundSimulation.ActiveId));
            Assert.AreSame(_second.motion, Shader.GetGlobalTexture(GroundSimulation.MotionId));
            Assert.AreSame(_second.crush, Shader.GetGlobalTexture(GroundSimulation.CrushId));
            Assert.AreSame(_second.state, Shader.GetGlobalTexture(GroundSimulation.StateId));
            Assert.IsTrue(_second.motion.IsCreated());
        }
    }
}
