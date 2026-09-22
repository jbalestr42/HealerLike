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
            Assert.That(writes.Count, Is.EqualTo(22));
            Assert.That(writes[21], Is.EqualTo(Applied));
            Assert.That(scalarWrites[Applied], Is.EqualTo(1f));
            Assert.That(scalarWrites.Count, Is.EqualTo(19));
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
