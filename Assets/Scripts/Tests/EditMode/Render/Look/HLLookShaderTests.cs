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
            Assert.That(material.GetFloat("_HLOutlineWidthMultiplier"), Is.EqualTo(1));
            Assert.That(material.GetFloat("_HLGroundGrid"), Is.Zero);
            Assert.That(material.GetFloat("_HLSmoothOutlineNormals"), Is.Zero);
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
                Assert.That(shader.GetPropertyCount(), Is.EqualTo(5));
                Assert.That(shader.GetPropertyName(3), Is.EqualTo("_BaseColor"));
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
        [TestCase(false)]
        [TestCase(true)]
        public void CapturePortraitShadowsAndMaskedEdges(bool beauty)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ||
                System.Environment.GetEnvironmentVariable(beauty ? "HL_B6_CAPTURE" : "HL_D5_CAPTURE") != "1")
                Assert.Ignore("Opt-in: HL_D5_CAPTURE=1 with -force-metal.");
            var owned = new List<Object>();
            var previousPipeline = QualitySettings.renderPipeline;
            var previousSun = RenderSettings.sun;
            var previousTarget = RenderTexture.active;
            var beautyNames = new[] { "_HLGridCell", "_HLGridStrength", "_HLTipLight" };
            var beautyValues = beautyNames.Select(Shader.GetGlobalFloat).ToArray();
            var previousGridOrigin = Shader.GetGlobalVector("_HLGridOrigin");
            var previousGridExtent = Shader.GetGlobalVector("_HLGridExtent");
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
                var ground = Make(PrimitiveType.Plane, "HL receiving ground", Vector3.zero, Vector3.one * 3, 1);
                var sphere = Make(PrimitiveType.Sphere, "HL casting sphere", new Vector3(-1, 2.1f, 0), Vector3.one * 4, 1);
                var sphereProperties = new MaterialPropertyBlock(); sphereProperties.SetColor("_BaseColor", new Color(.55f, .58f, .64f));
                sphereProperties.SetFloat("_HLNormalEdges", 1); sphere.GetComponent<Renderer>().SetPropertyBlock(sphereProperties);
                var target = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32); owned.Add(target); target.Create();
                var texture = new Texture2D(1080, 1920, TextureFormat.RGB24, false); owned.Add(texture);
                var directory = beauty ? "Assets/Render/Look/captures" : "/Users/fc/Documents/healerlike-render-specs/captures"; Directory.CreateDirectory(directory);
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
                if (beauty)
                {
                    camera.transform.rotation = Quaternion.Euler(73.7f, 0, 0);
                    camera.transform.position = -camera.transform.forward * 43.837f;
                    var groundMaterial = new Material(material); owned.Add(groundMaterial);
                    ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
                    groundMaterial.SetFloat("_HLGroundGrid", 1);
                    Shader.SetGlobalVector("_HLGridOrigin", new Vector4(-8, 0, -8, 0));
                    Shader.SetGlobalVector("_HLGridExtent", new Vector4(16, 0, 16, 0));
                    Shader.SetGlobalFloat("_HLGridCell", 1);
                    Shader.SetGlobalFloat("_HLGridStrength", 0);
                    var gridOff = Capture("beauty-look-grid-off");
                    var gridOffPixels = texture.GetPixels32();
                    Shader.SetGlobalFloat("_HLGridStrength", .16f);
                    var gridOn = Capture("beauty-look-grid");
                    Assert.That(gridOn.SequenceEqual(gridOff), Is.False);
                    var gridOnPixels = texture.GetPixels32();
                    int changedGridPixels = 0, escapedGridPixels = 0;
                    var groundPlane = new Plane(Vector3.up, Vector3.zero);
                    for (int i = 0; i < gridOnPixels.Length; i++)
                    {
                        if (gridOnPixels[i].Equals(gridOffPixels[i])) continue;
                        changedGridPixels++;
                        var ray = camera.ViewportPointToRay(new Vector3((i % 1080 + .5f) / 1080,
                            (i / 1080 + .5f) / 1920, 0));
                        if (!groundPlane.Raycast(ray, out float hit)) { escapedGridPixels++; continue; }
                        var point = ray.GetPoint(hit);
                        if (Mathf.Abs(point.x) > 8.04f || Mathf.Abs(point.z) > 8.04f) escapedGridPixels++;
                    }
                    Assert.That(changedGridPixels, Is.GreaterThan(1000));
                    Assert.That(escapedGridPixels, Is.Zero, "Grid must remain inside the battlefield rectangle.");
                    Debug.Log("HL beauty grid: " + changedGridPixels + " changed pixels, " + escapedGridPixels + " outside bounds");
                    settings.FogStart = 43.837f; settings.FogEnd = 50.356f;
                    look.Settings = settings;
                    Capture("beauty-look-grid-fade");
                    Shader.SetGlobalFloat("_HLGridStrength", 0);
                    Capture("beauty-look-fog");
                    settings.FogStart = 70; settings.FogEnd = 100;
                    settings.InkStrength = 1; settings.InkWarp = 0; settings.DashAmount = 0;
                    look.Settings = settings;
                    foreach (float distance in new[] { 42.8f, 43.837f, 47.8f })
                    {
                        camera.transform.position = -camera.transform.forward * distance;
                        Capture("beauty-look-hatch-" + distance.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    settings.InkStrength = 0; look.Settings = settings;
                    featureType.GetField("LayerMask").SetValue(feature, (LayerMask)(1 << 30));
                    featureType.GetMethod("Create").Invoke(feature, null);
                    foreach (var sample in new[] { (26f, false), (42.8f, false), (47.8f, false), (43.837f, true) })
                    {
                        float distance = sample.Item1;
                        camera.orthographic = sample.Item2; camera.orthographicSize = 16;
                        string label = distance.ToString(System.Globalization.CultureInfo.InvariantCulture) + (sample.Item2 ? "-ortho" : "");
                        camera.transform.position = -camera.transform.forward * distance;
                        material.SetFloat("_HLOutlineWidthMultiplier", 0);
                        var noOutline = Capture("beauty-look-outline-" + label + "-off");
                        var uninked = texture.GetPixels32();
                        material.SetFloat("_HLOutlineWidthMultiplier", 1);
                        var one = Capture("beauty-look-outline-" + label + "-1px");
                        var onePixels = texture.GetPixels32();
                        material.SetFloat("_HLOutlineWidthMultiplier", 2);
                        var two = Capture("beauty-look-outline-" + label + "-2px");
                        Assert.That(one.SequenceEqual(noOutline), Is.False);
                        Assert.That(two.SequenceEqual(one), Is.False);
                        var twoPixels = texture.GetPixels32();
                        var center = camera.WorldToViewportPoint(sphere.transform.position);
                        int row = Mathf.RoundToInt(center.y * 1920), middle = Mathf.RoundToInt(center.x * 1080);
                        int[] widths = new int[4];
                        for (int x = 0; x < 1080; x++)
                        {
                            int index = row * 1080 + x, side = x < middle ? 0 : 1;
                            if (!uninked[index].Equals(onePixels[index])) widths[side]++;
                            if (!uninked[index].Equals(twoPixels[index])) widths[side + 2]++;
                        }
                        Assert.That(widths[0], Is.InRange(1, 2), "one pixel left " + label);
                        Assert.That(widths[1], Is.InRange(1, 2), "one pixel right " + label);
                        Assert.That(widths[2], Is.InRange(2, 3), "two pixels left " + label);
                        Assert.That(widths[3], Is.InRange(2, 3), "two pixels right " + label);
                        Debug.Log("HL beauty outline " + label + ": " + string.Join(",", widths));
                    }
                    var probe = new Material(Shader.Find("Hidden/HL/Look/BeautyProbe")); owned.Add(probe);
                    Shader.SetGlobalFloat("_HLTipLight", .12f);
                    Graphics.Blit(Texture2D.whiteTexture, target, probe);
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); texture.Apply();
                    File.WriteAllBytes(Path.Combine(directory, "beauty-look-tip.png"), texture.EncodeToPNG());
                    // Left half is shadow, right is lit. Only lit tips brighten.
                    Assert.That(texture.GetPixel(800, 1800).g, Is.GreaterThan(texture.GetPixel(800, 100).g));
                    Assert.That(texture.GetPixel(200, 1800), Is.EqualTo(texture.GetPixel(200, 100)));
                    return;
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
                if (beauty)
                {
                    for (int i = 0; i < beautyNames.Length; i++) Shader.SetGlobalFloat(beautyNames[i], beautyValues[i]);
                    Shader.SetGlobalVector("_HLGridOrigin", previousGridOrigin);
                    Shader.SetGlobalVector("_HLGridExtent", previousGridExtent);
                }
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
