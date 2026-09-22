using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HealerLike.Render.Look
{
    public class HLLookControllerTests
    {
        private readonly Dictionary<int, float> floats = new Dictionary<int, float>();
        private readonly Dictionary<int, Vector4> vectors = new Dictionary<int, Vector4>();
        private readonly List<GameObject> objects = new List<GameObject>();
        private static readonly int Applied = Shader.PropertyToID("_HLLookApplied");

        [SetUp]
        public void SaveGlobalState()
        {
            foreach (var field in typeof(HLLookSettings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                int id = Shader.PropertyToID("_HL" + field.Name);
                if (field.FieldType == typeof(Color)) vectors[id] = Shader.GetGlobalVector(id);
                else floats[id] = Shader.GetGlobalFloat(id);
            }
            floats[Applied] = Shader.GetGlobalFloat(Applied);
            vectors[Shader.PropertyToID("_HLKeyLightDir")] = Shader.GetGlobalVector("_HLKeyLightDir");
        }

        [TearDown]
        public void RestoreGlobalState()
        {
            foreach (var go in objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            objects.Clear();
            foreach (var pair in floats) Shader.SetGlobalFloat(pair.Key, pair.Value);
            foreach (var pair in vectors) Shader.SetGlobalVector(pair.Key, pair.Value);
            floats.Clear();
            vectors.Clear();
        }

        private HLLookController CreateController(bool active = true)
        {
            var go = new GameObject("HL look test");
            objects.Add(go);
            go.SetActive(false);
            var controller = go.AddComponent<HLLookController>();
            if (active) go.SetActive(true);
            return controller;
        }

        [Test]
        public void KeyDirectionSelectsSunThenBrightestAndClearsOnDisable()
        {
            var controller = CreateController();
            var go = new GameObject("HL key"); objects.Add(go);
            var sun = go.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(45, -35, 0);
            var otherGo = new GameObject("HL brighter"); objects.Add(otherGo);
            var other = otherGo.AddComponent<Light>(); other.type = LightType.Directional; other.intensity = 4;
            Vector3 expected = -sun.transform.forward;
            Assert.That(HLLookController.SelectKeyLightDirection(new[] { other, sun }, sun, ~0),
                Is.EqualTo(new Vector4(expected.x, expected.y, expected.z, 0)));
            Assert.That(HLLookController.SelectKeyLightDirection(new[] { sun, other }, null, ~0),
                Is.EqualTo(new Vector4(0, 0, -1, 0)));
            other.enabled = false; sun.gameObject.layer = 7;
            Assert.That(HLLookController.SelectKeyLightDirection(new[] { sun, other }, sun, ~(1 << 7)), Is.EqualTo(Vector4.zero));
            var previousSun = RenderSettings.sun;
            try
            {
                RenderSettings.sun = sun;
                controller.ApplyGlobals();
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(new Vector4(expected.x, expected.y, expected.z, 0)));
                HLLookController.PublishMainLightDirection(null);
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(Vector4.zero));
                HLLookController.PublishMainLightDirection(sun);
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(new Vector4(expected.x, expected.y, expected.z, 0)));
                controller.enabled = false;
                HLLookController.PublishMainLightDirection(sun);
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(Vector4.zero));
            }
            finally { RenderSettings.sun = previousSun; }
        }

        [Test]
        public void SteadyPublicationAllocatesNothingAndDisabledSunClearsFallback()
        {
            var controller = CreateController();
            var go = new GameObject("HL allocation sun"); objects.Add(go);
            var sun = go.AddComponent<Light>(); sun.type = LightType.Directional;
            var previous = RenderSettings.sun;
            try
            {
                RenderSettings.sun = sun;
                var cameraGo = new GameObject("HL allocation camera"); objects.Add(cameraGo);
                var camera = cameraGo.AddComponent<Camera>();
                var callback = (Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>)Delegate.CreateDelegate(
                    typeof(Action<UnityEngine.Rendering.ScriptableRenderContext, Camera>), controller,
                    typeof(HLLookController).GetMethod("OnBeginCameraRendering", BindingFlags.Instance | BindingFlags.NonPublic));
                for (int i = 0; i < 8; i++) { controller.ApplyGlobals(); callback(default, camera); }
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 128; i++) { controller.ApplyGlobals(); callback(default, camera); }
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.That(allocated, Is.Zero);
                camera.cullingMask = 0;
                callback(default, camera);
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(Vector4.zero));
                sun.enabled = false;
                controller.ApplyGlobals();
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(Vector4.zero));
            }
            finally { RenderSettings.sun = previous; }
        }

        [TestCase(ColorSpace.Gamma)]
        [TestCase(ColorSpace.Linear)]
        public void ColorConversionIsExplicitAndAlphaIsOne(ColorSpace space)
        {
            var color = new Color(.2f, .5f, .8f, .3f);
            var expected = space == ColorSpace.Linear ? color.linear : color;
            Assert.That(HLLookController.ToWorkingColor(color, space),
                Is.EqualTo(new Vector4(expected.r, expected.g, expected.b, 1f)));
        }

        [Test]
        public void UploadPublishesAllValidatedValuesAndFlagLast()
        {
            var input = HLLookSettings.Default;
            input.ShadowStrength = float.NaN;
            input.FogBands = 0;
            var validated = input.Validated();
            var writes = new List<int>();
            var scalarWrites = new Dictionary<int, float>();
            var colorWrites = new Dictionary<int, Vector4>();
            Action<int, float> scalar = (id, value) => { writes.Add(id); scalarWrites.Add(id, value); };
            Action<int, Vector4> color = (id, value) => { writes.Add(id); colorWrites.Add(id, value); };
            typeof(HLLookController).GetMethod("PublishGlobals", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { input, ColorSpace.Linear, scalar, color });
            Assert.That(writes.Count, Is.EqualTo(23));
            Assert.That(writes[22], Is.EqualTo(Applied));
            Assert.That(scalarWrites[Applied], Is.EqualTo(1f));
            Assert.That(scalarWrites.Count, Is.EqualTo(20));
            Assert.That(colorWrites.Count, Is.EqualTo(3));
            foreach (var field in typeof(HLLookSettings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                int id = Shader.PropertyToID("_HL" + field.Name);
                if (field.FieldType == typeof(Color))
                    Assert.That(colorWrites[id], Is.EqualTo(HLLookController.ToWorkingColor((Color)field.GetValue(validated), ColorSpace.Linear)));
                else Assert.That(scalarWrites[id], Is.EqualTo(Convert.ToSingle(field.GetValue(validated))));
            }
            HLLookController.UploadGlobals(in input);
            Assert.That(Shader.GetGlobalFloat(Applied), Is.EqualTo(1f));
            foreach (var field in typeof(HLLookSettings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                int id = Shader.PropertyToID("_HL" + field.Name);
                if (field.FieldType == typeof(Color))
                    Assert.That(Shader.GetGlobalVector(id), Is.EqualTo(HLLookController.ToWorkingColor((Color)field.GetValue(validated), QualitySettings.activeColorSpace)));
                else Assert.That(Shader.GetGlobalFloat(id), Is.EqualTo(Convert.ToSingle(field.GetValue(validated))));
            }
        }

        [Test]
        public void ConflictingControllerCannotPublishOrClearOwnerGlobals()
        {
            var first = CreateController();
            first.ApplyGlobals();
            LogAssert.Expect(LogType.Warning, "HLLookController already has an active owner; this controller remains inactive.");
            var second = CreateController();
            var other = HLLookSettings.Default;
            other.FogBands = 13;
            second.Settings = other;
            second.ApplyGlobals();
            Assert.That(Shader.GetGlobalFloat("_HLFogBands"), Is.EqualTo(6f));
            second.enabled = false;
            Assert.That(Shader.GetGlobalFloat(Applied), Is.EqualTo(1f));
            first.enabled = false;
            Assert.That(Shader.GetGlobalFloat(Applied), Is.Zero);
            second.enabled = true;
            second.ApplyGlobals();
            Assert.That(Shader.GetGlobalFloat("_HLFogBands"), Is.EqualTo(13f));
        }

        [Test]
        public void OwnerDisableSelectsFallbackAndReplacementMustExplicitlyEnable()
        {
            var first = CreateController();
            LogAssert.Expect(LogType.Warning, "HLLookController already has an active owner; this controller remains inactive.");
            var second = CreateController();
            first.ApplyGlobals();
            first.enabled = false;
            Assert.That(Shader.GetGlobalFloat(Applied), Is.Zero);
            first.ApplyGlobals();
            second.ApplyGlobals();
            Assert.That(Shader.GetGlobalFloat(Applied), Is.Zero);
            second.enabled = false;
            second.enabled = true;
            second.ApplyGlobals();
            Assert.That(Shader.GetGlobalFloat(Applied), Is.EqualTo(1f));
        }

        [Test]
        public void ValidationIsLocalUntilExplicitPublication()
        {
            var controller = CreateController();
            controller.ApplyGlobals();
            var input = HLLookSettings.Default;
            input.FogBands = 0;
            TestHelpers.SetPrivateField(controller, "settings", input);
            TestHelpers.InvokePrivate(controller, "OnValidate");
            Assert.That(controller.Settings.FogBands, Is.EqualTo(1));
            Assert.That(Shader.GetGlobalFloat("_HLFogBands"), Is.EqualTo(6f));
            controller.ApplyGlobals();
            Assert.That(Shader.GetGlobalFloat("_HLFogBands"), Is.EqualTo(1f));
        }
    }
}
