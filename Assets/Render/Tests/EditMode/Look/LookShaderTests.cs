using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Look
{
    public class LookShaderTests
    {
        [Test]
        public void DefaultMaterialUsesAllyGreenAndInstancing()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/HLLook_Default.mat");

            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader.name, Is.EqualTo("HL/Look/Primitive"));
            Assert.That(material.enableInstancing, Is.True);
            Assert.That(material.GetFloat("_HLOutlineWidthMultiplier"), Is.EqualTo(1));
            Assert.That(material.GetFloat("_HLGroundGrid"), Is.Zero);
            Assert.That(material.GetFloat("_HLSmoothOutlineNormals"), Is.Zero);
            Color color = material.GetColor("_BaseColor");
            Assert.That(color.r, Is.EqualTo(127 / 255f).Within(0.000001f));
            Assert.That(color.g, Is.EqualTo(201 / 255f).Within(0.000001f));
            Assert.That(color.b, Is.EqualTo(63 / 255f).Within(0.000001f));
            Assert.That(color.a, Is.EqualTo(1f));
        }

        [Test]
        public void PrimitivePassesImportAndCompileSupportedVariantsWhenGraphicsAvailable()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLLook.shader");
            Assert.That(shader, Is.Not.Null);
            Material material = new Material(shader);
            try
            {
                string[] passes = { "HLForward", "HLShadowCaster", "HLOutline", "HLDepthOnly", "HLDepthNormals" };
                foreach (string pass in passes)
                {
                    Assert.That(material.FindPass(pass), Is.GreaterThanOrEqualTo(0), pass);
                }

                Assert.That(shader.GetPropertyCount(), Is.EqualTo(5));
                Assert.That(shader.GetPropertyName(3), Is.EqualTo("_BaseColor"));
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    Assert.That(shader.isSupported, Is.True);
                    string[] mainKeywords =
                    {
                        "", "_MAIN_LIGHT_SHADOWS", "_MAIN_LIGHT_SHADOWS_CASCADE", "_MAIN_LIGHT_SHADOWS_SCREEN"
                    };
                    string[] softKeywords =
                    {
                        "", "_SHADOWS_SOFT", "_SHADOWS_SOFT_LOW", "_SHADOWS_SOFT_MEDIUM", "_SHADOWS_SOFT_HIGH"
                    };
                    foreach (bool instanced in new[] { false, true })
                    {
                        foreach (string main in mainKeywords)
                        {
                            foreach (string soft in softKeywords)
                            {
                                Compile(material, "HLForward", instanced, main, soft);
                            }
                        }

                        Compile(material, "HLShadowCaster", instanced);
                        Compile(material, "HLShadowCaster", instanced, "_CASTING_PUNCTUAL_LIGHT_SHADOW");
                        Compile(material, "HLOutline", instanced);
                        Compile(material, "HLDepthOnly", instanced);
                        Compile(material, "HLDepthNormals", instanced);
                        Compile(material, "HLDepthNormals", instanced, "_GBUFFER_NORMALS_OCT");
                    }

                    Debug.Log("HL primitive: synchronously compiled 52 pass/keyword combinations on "
                              + SystemInfo.graphicsDeviceType);
                }

                AssertNoErrors(shader);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ScreenEdgesImportAndCompileBothNormalEncodingsWhenGraphicsAvailable()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Look/HLOutlinesEdges.shader");
            Assert.That(shader, Is.Not.Null);
            Material material = new Material(shader);
            try
            {
                Assert.That(material.FindPass("HLDepthNormalEdges"), Is.GreaterThanOrEqualTo(0));
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    Assert.That(shader.isSupported, Is.True);
                    Compile(material, "HLDepthNormalEdges", false);
                    Compile(material, "HLDepthNormalEdges", false, "_GBUFFER_NORMALS_OCT");
                    Debug.Log("HL edges: synchronously compiled both normal encodings on "
                              + SystemInfo.graphicsDeviceType);
                }

                AssertNoErrors(shader);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CapturePortraitShadowsAndMaskedEdges(bool beauty)
        {
            string captureVariable = beauty ? "HL_B6_CAPTURE" : "HL_D5_CAPTURE";
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null
                || System.Environment.GetEnvironmentVariable(captureVariable) != "1")
            {
                Assert.Ignore("Opt-in: HL_D5_CAPTURE=1 with -force-metal.");
            }

            List<Object> owned = new List<Object>();
            RenderPipelineAsset previousPipeline = QualitySettings.renderPipeline;
            Light previousSun = RenderSettings.sun;
            RenderTexture previousTarget = RenderTexture.active;
            string[] beautyNames = { "_HLGridCell", "_HLGridStrength", "_HLTipLight" };
            float[] beautyValues = beautyNames.Select(Shader.GetGlobalFloat).ToArray();
            Vector4 previousGridOrigin = Shader.GetGlobalVector("_HLGridOrigin");
            Vector4 previousGridExtent = Shader.GetGlobalVector("_HLGridExtent");
            LookController[] disabled = Object.FindObjectsByType<LookController>(FindObjectsSortMode.None)
                .Where(c => c.enabled).ToArray();
            foreach (LookController controller in disabled)
            {
                controller.enabled = false;
            }

            try
            {
                RenderPipelineAsset pipeline = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
                    "Assets/Settings/Very High_PipelineAsset.asset"));
                owned.Add(pipeline);
                SerializedObject pipelineData = new SerializedObject(pipeline);
                SerializedProperty rendererProperty = pipelineData.FindProperty("m_RendererDataList")
                    .GetArrayElementAtIndex(0);
                Object renderer = Object.Instantiate(rendererProperty.objectReferenceValue);
                owned.Add(renderer);
                rendererProperty.objectReferenceValue = renderer;
                pipelineData.ApplyModifiedPropertiesWithoutUndo();
                Type featureType = typeof(LookSettings).Assembly.GetType("HealerLike.Render.Look.HLOutlines");
                ScriptableObject feature = ScriptableObject.CreateInstance(featureType);
                owned.Add(feature);
                featureType.GetField("layerMask").SetValue(feature, (LayerMask)0);
                featureType.GetField("depthNormalEdges").SetValue(feature, false);
                featureType.GetMethod("Create").Invoke(feature, null);
                SerializedObject rendererData = new SerializedObject(renderer);
                SerializedProperty features = rendererData.FindProperty("m_RendererFeatures");
                features.arraySize = 1;
                features.GetArrayElementAtIndex(0).objectReferenceValue = feature;
                rendererData.ApplyModifiedPropertiesWithoutUndo();
                QualitySettings.renderPipeline = pipeline;

                Camera camera = new GameObject("HL portrait camera").AddComponent<Camera>();
                owned.Add(camera.gameObject);
                camera.cullingMask = 1 << 30;
                camera.fieldOfView = 40f;
                camera.aspect = 1080f / 1920f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
                camera.transform.position = -camera.transform.forward * 31f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.75f, 0.82f, 0.88f);
                Light light = new GameObject("HL upper left key").AddComponent<Light>();
                owned.Add(light.gameObject);
                light.type = LightType.Directional;
                light.intensity = 1f;
                light.transform.rotation = Quaternion.Euler(50f, 40f, 0f);
                light.shadows = LightShadows.Soft;
                light.shadowBias = 0.03f;
                light.shadowNormalBias = 0.15f;
                RenderSettings.sun = light;
                LookController look = new GameObject("HL capture look").AddComponent<LookController>();
                owned.Add(look.gameObject);
                LookSettings settings = LookSettings.Default;
                settings.fogStart = 70f;
                settings.fogEnd = 100f;
                settings.inkStrength = 0f;
                settings.outlineWidthPixels = 1f;
                look.settings = settings;
                Material material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLLook.shader"));
                owned.Add(material);
                material.SetColor("_BaseColor", new Color(0.6f, 0.78f, 0.3f));

                GameObject Make(PrimitiveType shape, string name, Vector3 position, Vector3 scale, float normalMask)
                {
                    GameObject go = GameObject.CreatePrimitive(shape);
                    owned.Add(go);
                    go.name = name;
                    go.layer = 30;
                    go.transform.position = position;
                    go.transform.localScale = scale;
                    Renderer meshRenderer = go.GetComponent<Renderer>();
                    meshRenderer.sharedMaterial = material;
                    MaterialPropertyBlock properties = new MaterialPropertyBlock();
                    properties.SetFloat("_HLNormalEdges", normalMask);
                    meshRenderer.SetPropertyBlock(properties);
                    return go;
                }

                GameObject ground = Make(PrimitiveType.Plane, "HL receiving ground", Vector3.zero, Vector3.one * 3f,
                                         1f);
                GameObject sphere = Make(PrimitiveType.Sphere, "HL casting sphere", new Vector3(-1f, 2.1f, 0f),
                                         Vector3.one * 4f, 1f);
                MaterialPropertyBlock sphereProperties = new MaterialPropertyBlock();
                sphereProperties.SetColor("_BaseColor", new Color(0.55f, 0.58f, 0.64f));
                sphereProperties.SetFloat("_HLNormalEdges", 1f);
                sphere.GetComponent<Renderer>().SetPropertyBlock(sphereProperties);
                RenderTexture target = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
                owned.Add(target);
                target.Create();
                Texture2D texture = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                owned.Add(texture);
                string directory = beauty
                    ? "Assets/Render/Look/captures"
                    : "/Users/fc/Documents/healerlike-render-specs/captures";
                Directory.CreateDirectory(directory);

                byte[] Capture(string name)
                {
                    look.ApplyGlobals();
                    RenderPipeline.SubmitRenderRequest(camera,
                                                       new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0f, 0f, 1080f, 1920f), 0, 0);
                    texture.Apply();
                    byte[] bytes = texture.EncodeToPNG();
                    File.WriteAllBytes(Path.Combine(directory, name + ".png"), bytes);
                    Assert.That(bytes.Length, Is.GreaterThan(10000));
                    Debug.Log("HL D5 capture: " + name + " on " + SystemInfo.graphicsDeviceType);
                    return bytes;
                }

                if (beauty)
                {
                    camera.transform.rotation = Quaternion.Euler(73.7f, 0f, 0f);
                    camera.transform.position = -camera.transform.forward * 43.837f;
                    Material groundMaterial = new Material(material);
                    owned.Add(groundMaterial);
                    ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
                    groundMaterial.SetFloat("_HLGroundGrid", 1f);
                    Shader.SetGlobalVector("_HLGridOrigin", new Vector4(-8f, 0f, -8f, 0f));
                    Shader.SetGlobalVector("_HLGridExtent", new Vector4(16f, 0f, 16f, 0f));
                    Shader.SetGlobalFloat("_HLGridCell", 1f);
                    Shader.SetGlobalFloat("_HLGridStrength", 0f);
                    byte[] gridOff = Capture("beauty-look-grid-off");
                    Color32[] gridOffPixels = texture.GetPixels32();
                    Shader.SetGlobalFloat("_HLGridStrength", 0.16f);
                    byte[] gridOn = Capture("beauty-look-grid");
                    Assert.That(gridOn.SequenceEqual(gridOff), Is.False);

                    Color32[] gridOnPixels = texture.GetPixels32();
                    int changedGridPixels = 0;
                    int escapedGridPixels = 0;
                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                    for (int i = 0; i < gridOnPixels.Length; i++)
                    {
                        if (gridOnPixels[i].Equals(gridOffPixels[i]))
                        {
                            continue;
                        }

                        changedGridPixels++;
                        Vector3 viewport = new Vector3((i % 1080 + 0.5f) / 1080, (i / 1080 + 0.5f) / 1920, 0f);
                        Ray ray = camera.ViewportPointToRay(viewport);
                        if (!groundPlane.Raycast(ray, out float hit))
                        {
                            escapedGridPixels++;
                            continue;
                        }

                        Vector3 point = ray.GetPoint(hit);
                        if (Mathf.Abs(point.x) > 8.04f || Mathf.Abs(point.z) > 8.04f)
                        {
                            escapedGridPixels++;
                        }
                    }

                    Assert.That(changedGridPixels, Is.GreaterThan(1000));
                    Assert.That(escapedGridPixels, Is.Zero, "Grid must remain inside the battlefield rectangle.");
                    Debug.Log("HL beauty grid: " + changedGridPixels + " changed pixels, " + escapedGridPixels
                              + " outside bounds");

                    settings.fogStart = 43.837f;
                    settings.fogEnd = 50.356f;
                    look.settings = settings;
                    Capture("beauty-look-grid-fade");
                    Shader.SetGlobalFloat("_HLGridStrength", 0f);
                    Capture("beauty-look-fog");

                    settings.fogStart = 70f;
                    settings.fogEnd = 100f;
                    settings.inkStrength = 1f;
                    settings.inkWarp = 0f;
                    settings.dashAmount = 0f;
                    look.settings = settings;
                    foreach (float distance in new[] { 42.8f, 43.837f, 47.8f })
                    {
                        camera.transform.position = -camera.transform.forward * distance;
                        Capture("beauty-look-hatch-" + distance.ToString(CultureInfo.InvariantCulture));
                    }

                    settings.inkStrength = 0f;
                    look.settings = settings;
                    featureType.GetField("layerMask").SetValue(feature, (LayerMask)(1 << 30));
                    featureType.GetMethod("Create").Invoke(feature, null);
                    (float, bool)[] samples = { (26f, false), (42.8f, false), (47.8f, false), (43.837f, true) };
                    foreach ((float, bool) sample in samples)
                    {
                        float distance = sample.Item1;
                        camera.orthographic = sample.Item2;
                        camera.orthographicSize = 16f;
                        string label = distance.ToString(CultureInfo.InvariantCulture) + (sample.Item2 ? "-ortho" : "");
                        camera.transform.position = -camera.transform.forward * distance;
                        material.SetFloat("_HLOutlineWidthMultiplier", 0f);
                        byte[] noOutline = Capture("beauty-look-outline-" + label + "-off");
                        Color32[] uninked = texture.GetPixels32();
                        material.SetFloat("_HLOutlineWidthMultiplier", 1f);
                        byte[] one = Capture("beauty-look-outline-" + label + "-1px");
                        Color32[] onePixels = texture.GetPixels32();
                        material.SetFloat("_HLOutlineWidthMultiplier", 2f);
                        byte[] two = Capture("beauty-look-outline-" + label + "-2px");
                        Assert.That(one.SequenceEqual(noOutline), Is.False);
                        Assert.That(two.SequenceEqual(one), Is.False);

                        Color32[] twoPixels = texture.GetPixels32();
                        Vector3 center = camera.WorldToViewportPoint(sphere.transform.position);
                        int row = Mathf.RoundToInt(center.y * 1920);
                        int middle = Mathf.RoundToInt(center.x * 1080);
                        int[] widths = new int[4];
                        for (int x = 0; x < 1080; x++)
                        {
                            int index = row * 1080 + x;
                            int side = x < middle ? 0 : 1;
                            if (!uninked[index].Equals(onePixels[index]))
                            {
                                widths[side]++;
                            }

                            if (!uninked[index].Equals(twoPixels[index]))
                            {
                                widths[side + 2]++;
                            }
                        }

                        Assert.That(widths[0], Is.InRange(1, 2), "one pixel left " + label);
                        Assert.That(widths[1], Is.InRange(1, 2), "one pixel right " + label);
                        Assert.That(widths[2], Is.InRange(2, 3), "two pixels left " + label);
                        Assert.That(widths[3], Is.InRange(2, 3), "two pixels right " + label);
                        Debug.Log("HL beauty outline " + label + ": " + string.Join(",", widths));
                    }

                    Material probe = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Look/HLLookBeautyProbe.shader"));
                    owned.Add(probe);
                    Shader.SetGlobalFloat("_HLTipLight", 0.12f);
                    Graphics.Blit(Texture2D.whiteTexture, target, probe);
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0f, 0f, 1080f, 1920f), 0, 0);
                    texture.Apply();
                    File.WriteAllBytes(Path.Combine(directory, "beauty-look-tip.png"), texture.EncodeToPNG());
                    Assert.That(texture.GetPixel(800, 1800).g, Is.GreaterThan(texture.GetPixel(800, 100).g));
                    Assert.That(texture.GetPixel(200, 1800), Is.EqualTo(texture.GetPixel(200, 100)));
                    return;
                }

                Capture("wave5-shadow");
                Color32[] pixels = texture.GetPixels32();
                Assert.That(pixels.Count(c => c.b > c.g * 1.3f && c.b > c.r * 1.5f), Is.GreaterThan(1000));

                for (int z = 0; z < 12; z++)
                {
                    for (int x = 0; x < 18; x++)
                    {
                        Vector3 bladePosition = new Vector3((x - 9) * 0.38f, 0.3f, -2.8f - z * 0.38f);
                        GameObject blade = Make(PrimitiveType.Cube, "HL masked blade", bladePosition,
                                                new Vector3(0.045f, 0.6f, 0.09f), 0f);
                        blade.transform.rotation = Quaternion.Euler(0f, (x * 37 + z * 23) % 180, (x % 3 - 1) * 15);
                        blade.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    }
                }

                byte[] off = Capture("wave5-edges-off");
                featureType.GetField("depthNormalEdges").SetValue(feature, true);
                featureType.GetMethod("Create").Invoke(feature, null);
                byte[] on = Capture("wave5-edges-on");
                Assert.That(on.SequenceEqual(off), Is.False, "Screen pass must change the image.");

                Texture2D left = new Texture2D(2, 2);
                owned.Add(left);
                left.LoadImage(off);
                Texture2D pair = new Texture2D(2160, 1920, TextureFormat.RGB24, false);
                owned.Add(pair);
                pair.SetPixels(0, 0, 1080, 1920, left.GetPixels());
                pair.SetPixels(1080, 0, 1080, 1920, texture.GetPixels());
                pair.Apply();
                File.WriteAllBytes(Path.Combine(directory, "wave5-edges-side-by-side.png"), pair.EncodeToPNG());

                featureType.GetField("useNormalEdgeMask").SetValue(feature, false);
                featureType.GetMethod("ApplyEdgeSettings").Invoke(feature, null);
                byte[] unmasked = Capture("wave5-edges-unmasked");
                Assert.That(unmasked.SequenceEqual(on), Is.False,
                            "Mask must suppress normal edges independently of the depth threshold.");

                featureType.GetField("useNormalEdgeMask").SetValue(feature, true);
                featureType.GetMethod("ApplyEdgeSettings").Invoke(feature, null);
                settings.inkStrength = 1f;
                settings.inkWarp = 0f;
                settings.dashAmount = 0f;
                look.settings = settings;
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
                    for (int i = 0; i < beautyNames.Length; i++)
                    {
                        Shader.SetGlobalFloat(beautyNames[i], beautyValues[i]);
                    }

                    Shader.SetGlobalVector("_HLGridOrigin", previousGridOrigin);
                    Shader.SetGlobalVector("_HLGridExtent", previousGridExtent);
                }

                RenderTexture.active = previousTarget;
                QualitySettings.renderPipeline = previousPipeline;
                RenderSettings.sun = previousSun;
                foreach (Object item in owned.AsEnumerable().Reverse())
                {
                    if (item)
                    {
                        Object.DestroyImmediate(item);
                    }
                }

                foreach (LookController controller in disabled)
                {
                    if (controller)
                    {
                        controller.enabled = true;
                    }
                }
            }
        }

        static void Compile(Material material, string passName, bool instanced, params string[] keywords)
        {
            material.enableInstancing = instanced;
            material.shaderKeywords = keywords.Where(k => k.Length != 0)
                .Concat(instanced ? new[] { "INSTANCING_ON" } : new string[0]).ToArray();
            int pass = material.FindPass(passName);
            ShaderUtil.CompilePass(material, pass, true);
            Assert.That(ShaderUtil.IsPassCompiled(material, pass), Is.True, passName);
            AssertNoErrors(material.shader);
        }

        static void AssertNoErrors(Shader shader)
        {
            IEnumerable<ShaderMessage> errors = ShaderUtil.GetShaderMessages(shader)
                .Where(m => m.severity == ShaderCompilerMessageSeverity.Error);
            Assert.That(errors.Select(m => m.message + " at " + m.file + ":" + m.line), Is.Empty);
        }
    }
}
