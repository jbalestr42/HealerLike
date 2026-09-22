using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Look
{
    public class HLLookShaderTests
    {
        [Test]
        public void DefaultMaterialUsesAllyGreenAndInstancing()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/HLLook_Default.mat");
            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader.name, Is.EqualTo("HL/Look/Primitive"));
            Assert.That(material.enableInstancing, Is.True);
            var color = material.GetColor("_BaseColor");
            Assert.That(color.r, Is.EqualTo(127/255f).Within(1e-6f));
            Assert.That(color.g, Is.EqualTo(201/255f).Within(1e-6f));
            Assert.That(color.b, Is.EqualTo(63/255f).Within(1e-6f));
            Assert.That(color.a, Is.EqualTo(1f));
        }

        [Test]
        public void PrimitivePassesImportAndCompileSupportedVariantsWhenGraphicsAvailable()
        {
            var shader = Shader.Find("HL/Look/Primitive");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                foreach (string pass in new[] { "HLForward", "HLShadowCaster", "HLOutline", "HLDepthOnly", "HLDepthNormals" })
                    Assert.That(material.FindPass(pass), Is.GreaterThanOrEqualTo(0), pass);
                Assert.That(shader.GetPropertyCount(), Is.EqualTo(2));
                Assert.That(shader.GetPropertyName(0), Is.EqualTo("_BaseColor"));
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    Assert.That(shader.isSupported, Is.True);
                    foreach (bool instanced in new[] { false, true })
                    {
                        foreach (string main in new[] { "", "_MAIN_LIGHT_SHADOWS", "_MAIN_LIGHT_SHADOWS_CASCADE", "_MAIN_LIGHT_SHADOWS_SCREEN" })
                        foreach (string soft in new[] { "", "_SHADOWS_SOFT", "_SHADOWS_SOFT_LOW", "_SHADOWS_SOFT_MEDIUM", "_SHADOWS_SOFT_HIGH" })
                            Compile(material, "HLForward", instanced, main, soft);
                        Compile(material, "HLShadowCaster", instanced);
                        Compile(material, "HLShadowCaster", instanced, "_CASTING_PUNCTUAL_LIGHT_SHADOW");
                        Compile(material, "HLOutline", instanced);
                        Compile(material, "HLDepthOnly", instanced);
                        Compile(material, "HLDepthNormals", instanced);
                        Compile(material, "HLDepthNormals", instanced, "_GBUFFER_NORMALS_OCT");
                    }
                    Debug.Log("HL primitive: synchronously compiled 52 pass/keyword combinations on " + SystemInfo.graphicsDeviceType);
                }
                AssertNoErrors(shader);
            }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void ScreenEdgesImportAndCompileBothNormalEncodingsWhenGraphicsAvailable()
        {
            var shader = Shader.Find("Hidden/HL/Look/DepthNormalOutline");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                Assert.That(material.FindPass("HLDepthNormalEdges"), Is.GreaterThanOrEqualTo(0));
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    Assert.That(shader.isSupported, Is.True);
                    Compile(material, "HLDepthNormalEdges", false);
                    Compile(material, "HLDepthNormalEdges", false, "_GBUFFER_NORMALS_OCT");
                    Debug.Log("HL edges: synchronously compiled both normal encodings on " + SystemInfo.graphicsDeviceType);
                }
                AssertNoErrors(shader);
            }
            finally { Object.DestroyImmediate(material); }
        }

        // Opt-in graphics integration fixture. Builds an isolated in-memory test scene;
        // renderer and pipeline assets are cloned, never saved or dirtied on disk.
        [Test]
        public void CapturePortraitShadowsAndMaskedEdges()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                System.Environment.GetEnvironmentVariable("HL_D5_CAPTURE") != "1")
                Assert.Ignore("Opt-in: HL_D5_CAPTURE=1 with -force-metal.");
            var owned = new List<Object>();
            var previousPipeline = QualitySettings.renderPipeline;
            var previousSun = RenderSettings.sun;
            var previousTarget = RenderTexture.active;
            var disabled = Object.FindObjectsByType<HLLookController>(FindObjectsSortMode.None)
                .Where(c => c.enabled).ToArray();
            foreach (var c in disabled) c.enabled = false;
            try
            {
                var pipeline = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
                    "Assets/Settings/Very High_PipelineAsset.asset")); owned.Add(pipeline);
                var pipelineData = new SerializedObject(pipeline);
                var rendererProperty = pipelineData.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0);
                var renderer = Object.Instantiate(rendererProperty.objectReferenceValue); owned.Add(renderer);
                rendererProperty.objectReferenceValue = renderer; pipelineData.ApplyModifiedPropertiesWithoutUndo();
                var featureType = typeof(HLLookSettings).Assembly.GetType("HealerLike.Render.Look.HLOutlines");
                var feature = ScriptableObject.CreateInstance(featureType); owned.Add(feature);
                featureType.GetField("LayerMask").SetValue(feature, (LayerMask)0); // isolate screen edges
                featureType.GetField("DepthNormalEdges").SetValue(feature, false);
                featureType.GetMethod("Create").Invoke(feature, null);
                var rendererData = new SerializedObject(renderer);
                var features = rendererData.FindProperty("m_RendererFeatures"); features.arraySize = 1;
                features.GetArrayElementAtIndex(0).objectReferenceValue = feature;
                rendererData.ApplyModifiedPropertiesWithoutUndo();
                QualitySettings.renderPipeline = pipeline;

                var camera = new GameObject("HL portrait camera").AddComponent<Camera>();
                owned.Add(camera.gameObject);
                camera.cullingMask = 1 << 30; camera.fieldOfView = 40; camera.aspect = 1080f / 1920f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 100;
                camera.transform.rotation = Quaternion.Euler(50, 0, 0);
                camera.transform.position = -camera.transform.forward * 31;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.75f, .82f, .88f);
                var light = new GameObject("HL upper left key").AddComponent<Light>();
                owned.Add(light.gameObject);
                light.type = LightType.Directional; light.intensity = 1;
                light.transform.rotation = Quaternion.Euler(50, 40, 0);
                light.shadows = LightShadows.Soft; light.shadowBias = .03f; light.shadowNormalBias = .15f;
                RenderSettings.sun = light;
                var look = new GameObject("HL capture look").AddComponent<HLLookController>();
                owned.Add(look.gameObject);
                var settings = HLLookSettings.Default; settings.FogStart = 70; settings.FogEnd = 100;
                settings.InkStrength = 0; settings.OutlineWidthPixels = 1;
                look.Settings = settings;
                var material = new Material(Shader.Find("HL/Look/Primitive")); owned.Add(material);
                material.SetColor("_BaseColor", new Color(.6f, .78f, .3f));
                GameObject Make(PrimitiveType shape, string name, Vector3 position, Vector3 scale, float normalMask)
                {
                    var go = GameObject.CreatePrimitive(shape); owned.Add(go); go.name = name; go.layer = 30;
                    go.transform.position = position; go.transform.localScale = scale;
                    var r = go.GetComponent<Renderer>(); r.sharedMaterial = material;
                    var properties = new MaterialPropertyBlock(); properties.SetFloat("_HLNormalEdges", normalMask);
                    r.SetPropertyBlock(properties); return go;
                }
                Make(PrimitiveType.Plane, "HL receiving ground", Vector3.zero, Vector3.one * 3, 1);
                var sphere = Make(PrimitiveType.Sphere, "HL casting sphere", new Vector3(-1, 2.1f, 0), Vector3.one * 4, 1);
                var sphereProperties = new MaterialPropertyBlock(); sphereProperties.SetColor("_BaseColor", new Color(.55f, .58f, .64f));
                sphereProperties.SetFloat("_HLNormalEdges", 1); sphere.GetComponent<Renderer>().SetPropertyBlock(sphereProperties);
                var target = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32); owned.Add(target); target.Create();
                var texture = new Texture2D(1080, 1920, TextureFormat.RGB24, false); owned.Add(texture);
                var directory = "/Users/fc/Documents/healerlike-render-specs/captures"; Directory.CreateDirectory(directory);
                byte[] Capture(string name)
                {
                    look.ApplyGlobals();
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); texture.Apply();
                    var bytes = texture.EncodeToPNG(); File.WriteAllBytes(Path.Combine(directory, name + ".png"), bytes);
                    Assert.That(bytes.Length, Is.GreaterThan(10000));
                    Debug.Log("HL D5 capture: " + name + " on " + SystemInfo.graphicsDeviceType);
                    return bytes;
                }
                Capture("wave5-shadow");
                // The ground shadow must contain the actual authored blue, rather than black or green-grey.
                var pixels = texture.GetPixels32();
                Assert.That(pixels.Count(c => c.b > c.g * 1.3f && c.b > c.r * 1.5f), Is.GreaterThan(1000));
                // Dense blade-shaped primitives exercise the same zero-alpha normal-mask seam as grass.
                for (int z = 0; z < 12; z++) for (int x = 0; x < 18; x++)
                {
                    var blade = Make(PrimitiveType.Cube, "HL masked blade", new Vector3((x - 9) * .38f, .3f, -2.8f - z * .38f),
                        new Vector3(.045f, .6f, .09f), 0);
                    blade.transform.rotation = Quaternion.Euler(0, (x * 37 + z * 23) % 180, (x % 3 - 1) * 15);
                    blade.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                var off = Capture("wave5-edges-off");
                featureType.GetField("DepthNormalEdges").SetValue(feature, true);
                featureType.GetMethod("Create").Invoke(feature, null);
                var on = Capture("wave5-edges-on");
                Assert.That(on.SequenceEqual(off), Is.False, "Screen pass must change the image.");
                var left = new Texture2D(2, 2); owned.Add(left); left.LoadImage(off);
                var pair = new Texture2D(2160, 1920, TextureFormat.RGB24, false); owned.Add(pair);
                pair.SetPixels(0, 0, 1080, 1920, left.GetPixels());
                pair.SetPixels(1080, 0, 1080, 1920, texture.GetPixels()); pair.Apply();
                File.WriteAllBytes(Path.Combine(directory, "wave5-edges-side-by-side.png"), pair.EncodeToPNG());
                featureType.GetField("UseNormalEdgeMask").SetValue(feature, false);
                featureType.GetMethod("ApplyEdgeSettings").Invoke(feature, null);
                var unmasked = Capture("wave5-edges-unmasked");
                Assert.That(unmasked.SequenceEqual(on), Is.False, "Mask must suppress normal edges independently of the depth threshold.");
                featureType.GetField("UseNormalEdgeMask").SetValue(feature, true);
                featureType.GetMethod("ApplyEdgeSettings").Invoke(feature, null);
                settings.InkStrength = 1; settings.InkWarp = 0; settings.DashAmount = 0;
                look.Settings = settings;
                Capture("wave5-hatch-31");
                foreach (float distance in new[] { 26f, 40f })
                {
                    camera.transform.position = -camera.transform.forward * distance;
                    Capture("wave5-hatch-" + distance);
                }
            }
            finally
            {
                RenderTexture.active = previousTarget; QualitySettings.renderPipeline = previousPipeline;
                RenderSettings.sun = previousSun;
                foreach (var item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item);
                foreach (var c in disabled) if (c) c.enabled = true;
            }
        }

        private static void Compile(Material material, string passName, bool instanced, params string[] keywords)
        {
            material.enableInstancing = instanced;
            material.shaderKeywords = keywords.Where(k => k.Length != 0)
                .Concat(instanced ? new[] { "INSTANCING_ON" } : new string[0]).ToArray();
            int pass = material.FindPass(passName);
            ShaderUtil.CompilePass(material, pass, true);
            Assert.That(ShaderUtil.IsPassCompiled(material, pass), Is.True, passName);
            AssertNoErrors(material.shader);
        }

        private static void AssertNoErrors(Shader shader)
        {
            var errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity == ShaderCompilerMessageSeverity.Error);
            Assert.That(errors.Select(m => m.message + " at " + m.file + ":" + m.line), Is.Empty);
        }
    }
}
