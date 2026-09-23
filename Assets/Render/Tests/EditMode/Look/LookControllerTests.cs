using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using HealerLike.Render.Stage;

namespace HealerLike.Render.Look
{
    public class LookControllerTests
    {
        static readonly int applied = Shader.PropertyToID("_HLLookApplied");
        static readonly string ownerWarning = "LookController already has an active owner; "
                                              + "this controller remains inactive.";

        readonly Dictionary<int, float> _floats = new Dictionary<int, float>();
        readonly Dictionary<int, Vector4> _vectors = new Dictionary<int, Vector4>();
        readonly List<GameObject> _objects = new List<GameObject>();

        // The settings fields are camelCase, their shader globals are _HL plus the PascalCase name
        static int GlobalId(FieldInfo field)
        {
            string name = char.ToUpperInvariant(field.Name[0]) + field.Name.Substring(1);
            return Shader.PropertyToID("_HL" + name);
        }

        static FieldInfo[] SettingsFields()
        {
            return typeof(LookSettings).GetFields(BindingFlags.Public | BindingFlags.Instance);
        }

        [SetUp]
        public void SaveGlobalState()
        {
            foreach (FieldInfo field in SettingsFields())
            {
                int id = GlobalId(field);
                if (field.FieldType == typeof(Color))
                {
                    _vectors[id] = Shader.GetGlobalVector(id);
                }
                else
                {
                    _floats[id] = Shader.GetGlobalFloat(id);
                }
            }

            _floats[applied] = Shader.GetGlobalFloat(applied);
            _vectors[Shader.PropertyToID("_HLKeyLightDir")] = Shader.GetGlobalVector("_HLKeyLightDir");
        }

        [TearDown]
        public void RestoreGlobalState()
        {
            foreach (GameObject go in _objects)
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            _objects.Clear();
            foreach (KeyValuePair<int, float> pair in _floats)
            {
                Shader.SetGlobalFloat(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<int, Vector4> pair in _vectors)
            {
                Shader.SetGlobalVector(pair.Key, pair.Value);
            }

            _floats.Clear();
            _vectors.Clear();
        }

        LookController CreateController(bool active = true)
        {
            GameObject go = new GameObject("Look test");
            _objects.Add(go);
            go.SetActive(false);
            LookController controller = go.AddComponent<LookController>();
            if (active)
            {
                go.SetActive(true);
            }

            return controller;
        }

        Light CreateDirectionalLight(string name)
        {
            GameObject go = new GameObject(name);
            _objects.Add(go);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            return light;
        }

        [Test]
        public void KeyDirectionSelectsSunThenBrightestAndClearsOnDisable()
        {
            LookController controller = CreateController();
            Light sun = CreateDirectionalLight("Key");
            sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            Light other = CreateDirectionalLight("Brighter");
            other.intensity = 4f;
            Vector3 expected = -sun.transform.forward;
            Vector4 expectedDirection = new Vector4(expected.x, expected.y, expected.z, 0f);

            Assert.That(LookController.SelectKeyLightDirection(new[] { other, sun }, sun, ~0),
                        Is.EqualTo(expectedDirection));
            Assert.That(LookController.SelectKeyLightDirection(new[] { sun, other }, null, ~0),
                        Is.EqualTo(new Vector4(0f, 0f, -1f, 0f)));

            other.enabled = false;
            sun.gameObject.layer = 7;
            Assert.That(LookController.SelectKeyLightDirection(new[] { sun, other }, sun, ~(1 << 7)),
                        Is.EqualTo(Vector4.zero));

            Light previousSun = RenderSettings.sun;
            try
            {
                RenderSettings.sun = sun;
                controller.ApplyGlobals();
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(expectedDirection));

                LookController.PublishMainLightDirection(null);
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(Vector4.zero));

                LookController.PublishMainLightDirection(sun);
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(expectedDirection));

                controller.enabled = false;
                TestHelpers.InvokePrivate(controller, "OnDisable");
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(Vector4.zero));
            }
            finally
            {
                RenderSettings.sun = previousSun;
            }
        }

        [Test]
        public void SteadyPublicationAllocatesNothingAndDisabledSunClearsFallback()
        {
            LookController controller = CreateController();
            Light sun = CreateDirectionalLight("Allocation sun");
            Light previous = RenderSettings.sun;
            try
            {
                RenderSettings.sun = sun;
                GameObject cameraGo = new GameObject("Allocation camera");
                _objects.Add(cameraGo);
                Camera camera = cameraGo.AddComponent<Camera>();
                MethodInfo method = typeof(LookController).GetMethod("OnBeginCameraRendering",
                                                                       BindingFlags.Instance | BindingFlags.NonPublic);
                Action<ScriptableRenderContext, Camera> callback = (Action<ScriptableRenderContext, Camera>)
                    Delegate.CreateDelegate(typeof(Action<ScriptableRenderContext, Camera>), controller, method);
                for (int i = 0; i < 8; i++)
                {
                    controller.ApplyGlobals();
                    callback(default, camera);
                }

                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 128; i++)
                {
                    controller.ApplyGlobals();
                    callback(default, camera);
                }

                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.That(allocated, Is.Zero);

                camera.cullingMask = 0;
                callback(default, camera);
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(Vector4.zero));

                sun.enabled = false;
                controller.ApplyGlobals();
                Assert.That(Shader.GetGlobalVector("_HLKeyLightDir"), Is.EqualTo(Vector4.zero));
            }
            finally
            {
                RenderSettings.sun = previous;
            }
        }

        [TestCase(ColorSpace.Gamma)]
        [TestCase(ColorSpace.Linear)]
        public void ColorConversionIsExplicitAndAlphaIsOne(ColorSpace space)
        {
            Color color = new Color(0.2f, 0.5f, 0.8f, 0.3f);
            Color expected = space == ColorSpace.Linear ? color.linear : color;

            Assert.That(LookController.ToWorkingColor(color, space),
                        Is.EqualTo(new Vector4(expected.r, expected.g, expected.b, 1f)));
        }

        [Test]
        public void UploadPublishesAllValidatedValuesAndFlagLast()
        {
            LookSettings input = LookSettings.Default;
            input.shadowStrength = float.NaN;
            input.fogBands = 0;
            LookSettings validated = input.Validated();
            List<int> writes = new List<int>();
            Dictionary<int, float> scalarWrites = new Dictionary<int, float>();
            Dictionary<int, Vector4> colorWrites = new Dictionary<int, Vector4>();
            Action<int, float> scalar = (id, value) =>
            {
                writes.Add(id);
                scalarWrites.Add(id, value);
            };
            Action<int, Vector4> color = (id, value) =>
            {
                writes.Add(id);
                colorWrites.Add(id, value);
            };

            typeof(LookController).GetMethod("PublishGlobals", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { input, ColorSpace.Linear, scalar, color });

            Assert.That(writes.Count, Is.EqualTo(23));
            Assert.That(writes[22], Is.EqualTo(applied));
            Assert.That(scalarWrites[applied], Is.EqualTo(1f));
            Assert.That(scalarWrites.Count, Is.EqualTo(20));
            Assert.That(colorWrites.Count, Is.EqualTo(3));
            foreach (FieldInfo field in SettingsFields())
            {
                int id = GlobalId(field);
                if (field.FieldType == typeof(Color))
                {
                    Vector4 expected = LookController.ToWorkingColor((Color)field.GetValue(validated),
                                                                       ColorSpace.Linear);
                    Assert.That(colorWrites[id], Is.EqualTo(expected));
                }
                else
                {
                    Assert.That(scalarWrites[id], Is.EqualTo(Convert.ToSingle(field.GetValue(validated))));
                }
            }

            LookController.UploadGlobals(in input);

            Assert.That(Shader.GetGlobalFloat(applied), Is.EqualTo(1f));
            foreach (FieldInfo field in SettingsFields())
            {
                int id = GlobalId(field);
                if (field.FieldType == typeof(Color))
                {
                    Vector4 expected = LookController.ToWorkingColor((Color)field.GetValue(validated),
                                                                       QualitySettings.activeColorSpace);
                    Assert.That(Shader.GetGlobalVector(id), Is.EqualTo(expected));
                }
                else
                {
                    Assert.That(Shader.GetGlobalFloat(id), Is.EqualTo(Convert.ToSingle(field.GetValue(validated))));
                }
            }
        }

        [Test]
        public void OnDisable_AppliedLook_ClearsTheAppliedFlag()
        {
            LookController controller = CreateController();
            controller.ApplyGlobals();

            controller.enabled = false;
            TestHelpers.InvokePrivate(controller, "OnDisable");

            Assert.That(Shader.GetGlobalFloat(applied), Is.Zero);
        }

        [Test]
        public void ValidationIsLocalUntilExplicitPublication()
        {
            LookController controller = CreateController();
            controller.ApplyGlobals();
            LookSettings input = LookSettings.Default;
            input.fogBands = 0;

            TestHelpers.SetPrivateField(controller, "_settings", input);
            TestHelpers.InvokePrivate(controller, "OnValidate");

            Assert.That(controller.settings.fogBands, Is.EqualTo(1));
            Assert.That(Shader.GetGlobalFloat("_HLFogBands"), Is.EqualTo(6f));

            controller.ApplyGlobals();
            Assert.That(Shader.GetGlobalFloat("_HLFogBands"), Is.EqualTo(1f));
        }

        Camera CreatePortraitCamera()
        {
            GameObject go = new GameObject("Look camera");
            _objects.Add(go);
            Camera camera = go.AddComponent<Camera>();
            camera.fieldOfView = 40f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 20f, -15f), Quaternion.Euler(52f, 0f, 0f));
            return camera;
        }

        [Test]
        public void Init_CameraAndBoard_SetsFogFromTheBoardCorners()
        {
            LookController controller = CreateController();
            Camera camera = CreatePortraitCamera();
            Bounds board = new Bounds(Vector3.zero, new Vector3(6f, 0f, 10f));

            controller.Init(camera, board);

            Vector2 fog = StageCalibration.BackgroundFog(camera.transform.position, board);
            Assert.AreEqual(fog.x, controller.settings.fogStart);
            Assert.AreEqual(fog.y, controller.settings.fogEnd);
            Assert.AreEqual(fog.x, controller.settings.inkDistStart);
        }

        [Test]
        public void Init_CameraAndBoard_ScalesTheHatchingToTheBoardCentre()
        {
            LookController controller = CreateController();
            Camera camera = CreatePortraitCamera();
            Bounds board = new Bounds(Vector3.zero, new Vector3(6f, 0f, 10f));

            controller.Init(camera, board);

            float depth = Vector3.Distance(camera.transform.position, board.center);
            float spacing = StageCalibration.HatchSpacing(camera, depth, 1920);
            Assert.AreEqual(spacing, controller.settings.inkScale, 0.00001f);
            Assert.AreEqual(spacing * 0.04f, controller.settings.inkWidth, 0.00001f);
            Assert.AreEqual(spacing * 1.2f, controller.settings.inkFarSpacing, 0.00001f);
        }

        [Test]
        public void Init_CameraAndBoard_KeepsTheAuthoredColours()
        {
            LookController controller = CreateController();
            LookSettings authored = LookSettings.Default;
            authored.shadowTint = new Color(63f / 255f, 91f / 255f, 148f / 255f, 1f);
            authored.inkStrength = 0.75f;
            controller.settings = authored;

            controller.Init(CreatePortraitCamera(), new Bounds(Vector3.zero, new Vector3(6f, 0f, 10f)));

            Assert.AreEqual(authored.shadowTint, controller.settings.shadowTint);
            Assert.AreEqual(0.75f, controller.settings.inkStrength);
        }

        [Test]
        public void Init_NoCamera_LogsAndKeepsTheSettings()
        {
            LookController controller = CreateController();
            LookSettings before = controller.settings;

            LogAssert.Expect(LogType.Error, "[LookController] Init needs the camera the look is calibrated for.");
            controller.Init(null, new Bounds(Vector3.zero, Vector3.one));

            Assert.AreEqual(before.fogStart, controller.settings.fogStart);
            Assert.AreEqual(before.inkScale, controller.settings.inkScale);
        }
    }
}
