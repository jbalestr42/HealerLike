using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Grass;
using HealerLike.Render.Look;
using HealerLike.Render.Zones;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Stage
{
    public class GrassLabSceneTests
    {
        GameObject _managerObject;
        RenderManager _manager;
        LookShaderProperties.Snapshot _look;
        Texture _motion;
        Texture _crush;
        Texture _state;
        Vector4 _rect;
        float _active;
        int _zoneCount;

        [SetUp]
        public void SetUp()
        {
            _look = LookShaderProperties.Capture();
            _motion = Shader.GetGlobalTexture(GroundSimulation.MotionId);
            _crush = Shader.GetGlobalTexture(GroundSimulation.CrushId);
            _state = Shader.GetGlobalTexture(GroundSimulation.StateId);
            _rect = Shader.GetGlobalVector(GroundSimulation.RectId);
            _active = Shader.GetGlobalFloat(GroundSimulation.ActiveId);
            _zoneCount = Shader.GetGlobalInt("_HLZoneCount");
            _managerObject = new GameObject("Lab owner fixture");
            _manager = _managerObject.AddComponent<RenderManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_managerObject);
            _look.Restore();
            Shader.SetGlobalTexture(GroundSimulation.MotionId, _motion);
            Shader.SetGlobalTexture(GroundSimulation.CrushId, _crush);
            Shader.SetGlobalTexture(GroundSimulation.StateId, _state);
            Shader.SetGlobalVector(GroundSimulation.RectId, _rect);
            Shader.SetGlobalFloat(GroundSimulation.ActiveId, _active);
            Shader.SetGlobalInt("_HLZoneCount", _zoneCount);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Dispose_FailedCapture_RestoresManagerAndGroundPublication(bool wasEnabled)
        {
            _manager.enabled = wasEnabled;
            Texture2D texture = new Texture2D(1, 1);
            Vector4 rectangle = new Vector4(-2f, 3f, 0.1f, 0.2f);
            GameObject fixture = null;
            try
            {
                Shader.SetGlobalTexture(GroundSimulation.MotionId, texture);
                Shader.SetGlobalTexture(GroundSimulation.CrushId, texture);
                Shader.SetGlobalTexture(GroundSimulation.StateId, texture);
                Shader.SetGlobalVector(GroundSimulation.RectId, rectangle);
                Shader.SetGlobalFloat(GroundSimulation.ActiveId, 1f);
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using (GrassLabScene scene = new GrassLabScene(_manager))
                    {
                        Assert.IsFalse(_manager.enabled);
                        fixture = scene.Fixture("Failed lab fixture");
                        GroundSimulation.Unpublish();
                        throw new InvalidOperationException("Lab failed");
                    }
                });
                Assert.AreEqual(wasEnabled, _manager.enabled);
                Assert.IsTrue(fixture == null);
                Assert.AreSame(texture, Shader.GetGlobalTexture(GroundSimulation.MotionId));
                Assert.AreSame(texture, Shader.GetGlobalTexture(GroundSimulation.CrushId));
                Assert.AreSame(texture, Shader.GetGlobalTexture(GroundSimulation.StateId));
                Assert.AreEqual(rectangle, Shader.GetGlobalVector(GroundSimulation.RectId));
                Assert.AreEqual(1f, Shader.GetGlobalFloat(GroundSimulation.ActiveId));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void Dispose_RejectedCreatures_ReleasesPartialSceneAndGround()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Requires the lab's zone graphics buffer.");
            }
            GrassLabScene scene = new GrassLabScene(_manager);
            GroundVocabulary vocabulary = null;
            GraphicsBuffer zones = null;
            GameObject camera = null;
            try
            {
                Assert.IsFalse(scene.Init(), "A manager without creature looks must reject all four creatures.");
                vocabulary = scene.ground.vocabulary;
                zones = scene.registry.buffer;
                camera = scene.camera.gameObject;
                Assert.AreEqual(4, scene.creatures.Count);
            }
            finally
            {
                scene.Dispose();
            }
            Assert.IsTrue(camera == null);
            Assert.IsTrue(vocabulary == null);
            Assert.IsFalse(zones.IsValid());
            Assert.AreEqual(0, scene.ground.bodyCount);
            Assert.AreEqual(0, scene.ground.heldCount);
            Assert.IsTrue(_manager.enabled);
        }
    }
}
