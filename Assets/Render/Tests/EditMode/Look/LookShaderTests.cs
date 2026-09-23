using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HealerLike.Render.Grass;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Look
{

// Look.shader and its companions are assets, not classes: these tests import, compile and render them
public class LookShaderTests
{
    // Globals the tests overwrite: the beauty capture's grid and tip light, then the readbacks' look
    static readonly string[] savedGlobals =
    {
        "_HLGridCell", "_HLGridStrength", "_HLTipLight",
        "_HLLookApplied", "_HLToonThreshold", "_HLToonSoftness", "_HLFogStart", "_HLFogEnd", "_HLFogBands",
        "_HLInkStrength", "_HLContrast", "_HLInkScale", "_HLInkWidth", "_HLInkStart", "_HLInkRange",
        "_HLDensityMul", "_HLInkWarp", "_HLDashAmount", "_HLDashScale", "_HLInkDistStart", "_HLInkFarSpacing"
    };

    readonly List<Object> _owned = new List<Object>();
    readonly List<LookController> _disabledLooks = new List<LookController>();
    RenderPipelineAsset _previousPipeline;
    Light _previousSun;
    RenderTexture _previousTarget;
    float[] _previousGlobals;
    Vector4 _previousGridOrigin;
    Vector4 _previousGridExtent;

    // The capture scene, built only by the opt-in capture tests
    Outlines _outlines;
    Camera _camera;
    LookController _look;
    LookSettings _settings;
    Material _material;
    GameObject _ground;
    GameObject _sphere;
    RenderTexture _target;
    Texture2D _texture;
    string _directory;

    [SetUp]
    public void SetUp()
    {
        _previousPipeline = QualitySettings.renderPipeline;
        _previousSun = RenderSettings.sun;
        _previousTarget = RenderTexture.active;
        _previousGlobals = savedGlobals.Select(Shader.GetGlobalFloat).ToArray();
        _previousGridOrigin = Shader.GetGlobalVector("_HLGridOrigin");
        _previousGridExtent = Shader.GetGlobalVector("_HLGridExtent");
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < savedGlobals.Length; i++)
        {
            Shader.SetGlobalFloat(savedGlobals[i], _previousGlobals[i]);
        }

        Shader.SetGlobalVector("_HLGridOrigin", _previousGridOrigin);
        Shader.SetGlobalVector("_HLGridExtent", _previousGridExtent);
        RenderTexture.active = _previousTarget;
        QualitySettings.renderPipeline = _previousPipeline;
        RenderSettings.sun = _previousSun;
        for (int i = _owned.Count - 1; i >= 0; i--)
        {
            if (_owned[i])
            {
                Object.DestroyImmediate(_owned[i]);
            }
        }
        _owned.Clear();

        foreach (LookController controller in _disabledLooks)
        {
            if (controller)
            {
                controller.enabled = true;
            }
        }
        _disabledLooks.Clear();
    }

    T Track<T>(T item) where T : Object
    {
        _owned.Add(item);
        return item;
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        return Track(ScriptableObject.CreateInstance<T>());
    }

    [Test]
    public void DefaultMaterial_ShippedAsset_UsesAllyGreenAndInstancing()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");

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
    public void CompilePass_PrimitiveShader_ImportsAndCompilesSupportedVariants()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/Look.shader");
        Assert.That(shader, Is.Not.Null);
        Material material = Track(new Material(shader));

        string[] passes = { "HLForward", "HLShadowCaster", "HLOutline", "HLDepthOnly", "HLDepthNormals" };
        foreach (string pass in passes)
        {
            Assert.That(material.FindPass(pass), Is.GreaterThanOrEqualTo(0), pass);
        }

        Assert.That(shader.GetPropertyCount(), Is.EqualTo(6));
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

            Debug.Log("[LookShaderTests] Primitive: synchronously compiled 52 pass/keyword combinations on "
                      + SystemInfo.graphicsDeviceType);
        }

        AssertNoErrors(shader);
    }

    [Test]
    public void DrawMesh_WorkingSpaceColourVector_ReachesPrimitiveShaderUnchanged()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Requires graphics readback");
        }

        Material material = Track(new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/Look.shader")));
        // Built-in asset, not tracked: TearDown must not destroy it
        Mesh mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        RenderTexture target = Track(new RenderTexture(16, 16, 0, RenderTextureFormat.ARGBFloat,
                                                       RenderTextureReadWrite.Linear));
        target.Create();
        Texture2D texture = Track(new Texture2D(16, 16, TextureFormat.RGBAFloat, false, true));
        // Below any illumination the fill stays lit, and a contrast of 1 leaves the colour as it is
        Shader.SetGlobalFloat("_HLLookApplied", 1f);
        Shader.SetGlobalFloat("_HLToonThreshold", -1f);
        Shader.SetGlobalFloat("_HLToonSoftness", 0f);
        Shader.SetGlobalFloat("_HLFogStart", 10000f);
        Shader.SetGlobalFloat("_HLFogEnd", 20000f);
        Shader.SetGlobalFloat("_HLFogBands", 6f);
        Shader.SetGlobalFloat("_HLInkStrength", 0f);
        Shader.SetGlobalFloat("_HLContrast", 1f);
        Color artist = new Color(0.4f, 0.6f, 0.8f, 1f);
        Color linear = artist.linear;

        MaterialPropertyBlock artistColor = new MaterialPropertyBlock();
        artistColor.SetColor("_BaseColor", artist);
        MaterialPropertyBlock linearColor = new MaterialPropertyBlock();
        linearColor.SetColor("_BaseColor", linear);
        MaterialPropertyBlock linearVector = new MaterialPropertyBlock();
        linearVector.SetVector("_BaseColor", linear);
        Color a = ReadCentre(material, mesh, target, texture, artistColor);
        Color b = ReadCentre(material, mesh, target, texture, linearColor);
        Color c = ReadCentre(material, mesh, target, texture, linearVector);

        Assert.That(((Vector4)a - (Vector4)c).magnitude, Is.LessThan(0.004f),
                    "Artist SetColor and working SetVector must agree");
        Assert.That(((Vector4)b - (Vector4)c).magnitude, Is.GreaterThan(0.2f),
                    "Double conversion must be detected by GPU readback");
        Assert.That(((Vector4)c - (Vector4)linear).magnitude, Is.LessThan(0.004f),
                    "Explicit working vector should reach GPU unchanged");
        Debug.Log("[LookShaderTests] Colour: active=" + QualitySettings.activeColorSpace + " artist=" + artist.ToString("F5")
                  + " linear=" + linear.ToString("F5") + " A=" + a.ToString("F5") + " B=" + b.ToString("F5")
                  + " C=" + c.ToString("F5"));
    }

    [Test]
    public void DrawMesh_SubPixelHatch_MergesIntoInkInsteadOfFading()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Requires graphics readback");
        }

        Material material = Track(new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/Look.shader")));
        // Built-in asset, not tracked: TearDown must not destroy it
        Mesh mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        RenderTexture target = Track(new RenderTexture(16, 16, 0, RenderTextureFormat.ARGBFloat,
                                                       RenderTextureReadWrite.Linear));
        target.Create();
        Texture2D texture = Track(new Texture2D(16, 16, TextureFormat.RGBAFloat, false, true));
        MaterialPropertyBlock white = new MaterialPropertyBlock();
        white.SetVector("_BaseColor", Vector4.one);
        // Strokes a thousand times finer than a pixel, in full shade, no dashes and no fog
        Shader.SetGlobalFloat("_HLLookApplied", 1f);
        Shader.SetGlobalFloat("_HLToonThreshold", -1f);
        Shader.SetGlobalFloat("_HLToonSoftness", 0f);
        Shader.SetGlobalFloat("_HLFogStart", 10000f);
        Shader.SetGlobalFloat("_HLFogEnd", 20000f);
        Shader.SetGlobalFloat("_HLFogBands", 6f);
        Shader.SetGlobalFloat("_HLContrast", 1f);
        Shader.SetGlobalFloat("_HLInkScale", 0.0001f);
        Shader.SetGlobalFloat("_HLInkWidth", 1f);
        Shader.SetGlobalFloat("_HLInkStart", -1f);
        Shader.SetGlobalFloat("_HLInkRange", 1f);
        Shader.SetGlobalFloat("_HLDensityMul", 1f);
        Shader.SetGlobalFloat("_HLInkWarp", 0f);
        Shader.SetGlobalFloat("_HLDashAmount", 0f);
        Shader.SetGlobalFloat("_HLDashScale", 1f);
        Shader.SetGlobalFloat("_HLInkDistStart", 10000f);
        Shader.SetGlobalFloat("_HLInkFarSpacing", 0f);

        Shader.SetGlobalFloat("_HLInkStrength", 0f);
        ReadCentre(material, mesh, target, texture, white);
        float paper = MeanBrightness(texture);
        Shader.SetGlobalFloat("_HLInkStrength", 1f);
        ReadCentre(material, mesh, target, texture, white);
        float inked = MeanBrightness(texture);

        Assert.That(paper - inked, Is.GreaterThan(0.1f), "Unresolved strokes must darken, not fade out");
        Debug.Log("[LookShaderTests] Sub-pixel hatch: paper=" + paper.ToString("F4") + " inked=" + inked.ToString("F4"));
    }

    // The quad covers the middle eight by eight pixels, the rest is the clear colour
    static float MeanBrightness(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels(4, 4, 8, 8);
        float sum = 0f;
        foreach (Color pixel in pixels)
        {
            sum += (pixel.r + pixel.g + pixel.b) / 3f;
        }

        return sum / pixels.Length;
    }

    [Test]
    public void CompilePass_GrassInstancingKeywords_CompilesLookAndRingShaders()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Shader compilation needs a graphics device.");
        }

        foreach (string path in new[] { "Assets/Render/Shaders/Look.shader", "Assets/Render/Shaders/GrassRing.shader" })
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.NotNull(shader);
            Material material = Track(new Material(shader) { enableInstancing = true });
            foreach (string shadow in new[] { "", "_MAIN_LIGHT_SHADOWS", "_MAIN_LIGHT_SHADOWS_CASCADE", "_MAIN_LIGHT_SHADOWS_SCREEN" })
            {
                foreach (bool isOctahedral in new[] { false, true })
                {
                    string normals = isOctahedral ? "_GBUFFER_NORMALS_OCT" : "";
                    material.shaderKeywords = new[] { "PROCEDURAL_INSTANCING_ON", GrassPalette.InstancedKeyword, shadow, normals, "_SHADOWS_SOFT" };
                    for (int pass = 0; pass < material.passCount; pass++)
                    {
                        ShaderUtil.CompilePass(material, pass, true);
                        Assert.IsTrue(ShaderUtil.IsPassCompiled(material, pass), path + " pass " + pass);
                    }
                }
            }

            foreach (ShaderMessage message in ShaderUtil.GetShaderMessages(shader))
            {
                Assert.AreNotEqual(ShaderCompilerMessageSeverity.Error, message.severity, message.message);
            }
        }
    }

    [Test]
    public void CompilePass_ScreenEdgesShader_CompilesBothNormalEncodings()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Look/OutlinesEdges.shader");
        Assert.That(shader, Is.Not.Null);
        Material material = Track(new Material(shader));

        Assert.That(material.FindPass("HLDepthNormalEdges"), Is.GreaterThanOrEqualTo(0));
        if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
        {
            Assert.That(shader.isSupported, Is.True);
            Compile(material, "HLDepthNormalEdges", false);
            Compile(material, "HLDepthNormalEdges", false, "_GBUFFER_NORMALS_OCT");
            Debug.Log("[LookShaderTests] Edges: synchronously compiled both normal encodings on "
                      + SystemInfo.graphicsDeviceType);
        }

        AssertNoErrors(shader);
    }

    [Test]
    public void Capture_BeautyScene_KeepsGridInsideBoardAndOutlinesAtPixelWidth()
    {
        IgnoreUnlessCapturing("RENDER_CAPTURE_BEAUTY");
        BuildCaptureScene("Assets/Render/Look/captures");
        _camera.transform.rotation = Quaternion.Euler(73.7f, 0f, 0f);
        _camera.transform.position = -_camera.transform.forward * 43.837f;
        Material groundMaterial = Track(new Material(_material));
        _ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
        groundMaterial.SetFloat("_HLGroundGrid", 1f);
        Shader.SetGlobalVector("_HLGridOrigin", new Vector4(-8f, 0f, -8f, 0f));
        Shader.SetGlobalVector("_HLGridExtent", new Vector4(16f, 0f, 16f, 0f));
        Shader.SetGlobalFloat("_HLGridCell", 1f);
        Shader.SetGlobalFloat("_HLGridStrength", 0f);

        byte[] gridOff = Capture("beauty-look-grid-off");
        Color32[] gridOffPixels = _texture.GetPixels32();
        Shader.SetGlobalFloat("_HLGridStrength", 0.16f);
        byte[] gridOn = Capture("beauty-look-grid");
        Assert.That(gridOn.SequenceEqual(gridOff), Is.False);

        Color32[] gridOnPixels = _texture.GetPixels32();
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
            Ray ray = _camera.ViewportPointToRay(viewport);
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
        Debug.Log("[LookShaderTests] Beauty grid: " + changedGridPixels + " changed pixels, " + escapedGridPixels
                  + " outside bounds");

        _settings.fogStart = 43.837f;
        _settings.fogEnd = 50.356f;
        _look.settings = _settings;
        Capture("beauty-look-grid-fade");
        Shader.SetGlobalFloat("_HLGridStrength", 0f);
        Capture("beauty-look-fog");

        _settings.fogStart = 70f;
        _settings.fogEnd = 100f;
        _settings.inkStrength = 1f;
        _settings.inkWarp = 0f;
        _settings.dashAmount = 0f;
        _look.settings = _settings;
        foreach (float distance in new[] { 42.8f, 43.837f, 47.8f })
        {
            _camera.transform.position = -_camera.transform.forward * distance;
            Capture("beauty-look-hatch-" + distance.ToString(CultureInfo.InvariantCulture));
        }

        _settings.inkStrength = 0f;
        _look.settings = _settings;
        _outlines.layerMask = 1 << 30;
        _outlines.Create();
        float[] distances = { 26f, 42.8f, 47.8f, 43.837f };
        bool[] orthographic = { false, false, false, true };
        for (int s = 0; s < distances.Length; s++)
        {
            AssertOutlineWidths(distances[s], orthographic[s]);
        }

        Material probe = Track(new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Look/LookBeautyProbe.shader")));
        Shader.SetGlobalFloat("_HLTipLight", 0.12f);
        Graphics.Blit(Texture2D.whiteTexture, _target, probe);
        RenderTexture.active = _target;
        _texture.ReadPixels(new Rect(0f, 0f, 1080f, 1920f), 0, 0);
        _texture.Apply();
        File.WriteAllBytes(Path.Combine(_directory, "beauty-look-tip.png"), _texture.EncodeToPNG());
        Assert.That(_texture.GetPixel(800, 1800).g, Is.GreaterThan(_texture.GetPixel(800, 100).g));
        Assert.That(_texture.GetPixel(200, 1800), Is.EqualTo(_texture.GetPixel(200, 100)));
    }

    [Test]
    public void Capture_PortraitScene_ShowsShadowsAndMasksNormalEdges()
    {
        IgnoreUnlessCapturing("RENDER_CAPTURE_PORTRAIT");
        BuildCaptureScene("/Users/fc/Documents/healerlike-render-specs/captures");

        Capture("render-look-shadow");
        Color32[] pixels = _texture.GetPixels32();
        Assert.That(pixels.Count(c => c.b > c.g * 1.3f && c.b > c.r * 1.5f), Is.GreaterThan(1000));

        for (int z = 0; z < 12; z++)
        {
            for (int x = 0; x < 18; x++)
            {
                Vector3 bladePosition = new Vector3((x - 9) * 0.38f, 0.3f, -2.8f - z * 0.38f);
                GameObject blade = CreatePrimitive(PrimitiveType.Cube, "Masked blade", bladePosition,
                                                   new Vector3(0.045f, 0.6f, 0.09f), 0f);
                blade.transform.rotation = Quaternion.Euler(0f, (x * 37 + z * 23) % 180, (x % 3 - 1) * 15);
                blade.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        byte[] off = Capture("render-look-edges-off");
        _outlines.depthNormalEdges = true;
        _outlines.Create();
        byte[] on = Capture("render-look-edges-on");
        Assert.That(on.SequenceEqual(off), Is.False, "Screen pass must change the image.");

        Texture2D left = Track(new Texture2D(2, 2));
        left.LoadImage(off);
        Texture2D pair = Track(new Texture2D(2160, 1920, TextureFormat.RGB24, false));
        pair.SetPixels(0, 0, 1080, 1920, left.GetPixels());
        pair.SetPixels(1080, 0, 1080, 1920, _texture.GetPixels());
        pair.Apply();
        File.WriteAllBytes(Path.Combine(_directory, "render-look-edges-side-by-side.png"), pair.EncodeToPNG());

        _outlines.useNormalEdgeMask = false;
        _outlines.ApplyEdgeSettings();
        byte[] unmasked = Capture("render-look-edges-unmasked");
        Assert.That(unmasked.SequenceEqual(on), Is.False,
                    "Mask must suppress normal edges independently of the depth threshold.");

        _outlines.useNormalEdgeMask = true;
        _outlines.ApplyEdgeSettings();
        _settings.inkStrength = 1f;
        _settings.inkWarp = 0f;
        _settings.dashAmount = 0f;
        _look.settings = _settings;
        Capture("render-look-hatch-31");
        foreach (float distance in new[] { 26f, 40f })
        {
            _camera.transform.position = -_camera.transform.forward * distance;
            Capture("render-look-hatch-" + distance);
        }
    }

    static void IgnoreUnlessCapturing(string captureVariable)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null
            || System.Environment.GetEnvironmentVariable(captureVariable) != "1")
        {
            Assert.Ignore("Opt-in: " + captureVariable + "=1 with -force-metal.");
        }
    }

    // A private pipeline copy with only our outline feature, a key light, a look, a ground and a casting sphere
    void BuildCaptureScene(string directory)
    {
        foreach (LookController controller in Object.FindObjectsByType<LookController>(FindObjectsSortMode.None))
        {
            if (controller.enabled)
            {
                controller.enabled = false;
                _disabledLooks.Add(controller);
            }
        }

        RenderPipelineAsset pipeline = Track(Object.Instantiate(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(
            "Assets/Settings/Very High_PipelineAsset.asset")));
        SerializedObject pipelineData = new SerializedObject(pipeline);
        SerializedProperty rendererProperty = pipelineData.FindProperty("m_RendererDataList")
            .GetArrayElementAtIndex(0);
        Object renderer = Track(Object.Instantiate(rendererProperty.objectReferenceValue));
        rendererProperty.objectReferenceValue = renderer;
        pipelineData.ApplyModifiedPropertiesWithoutUndo();
        _outlines = CreateTracked<Outlines>();
        _outlines.layerMask = 0;
        _outlines.depthNormalEdges = false;
        _outlines.Create();
        SerializedObject rendererData = new SerializedObject(renderer);
        SerializedProperty features = rendererData.FindProperty("m_RendererFeatures");
        features.arraySize = 1;
        features.GetArrayElementAtIndex(0).objectReferenceValue = _outlines;
        rendererData.ApplyModifiedPropertiesWithoutUndo();
        QualitySettings.renderPipeline = pipeline;

        _camera = Track(new GameObject("Portrait camera")).AddComponent<Camera>();
        _camera.cullingMask = 1 << 30;
        _camera.fieldOfView = 40f;
        _camera.aspect = 1080f / 1920f;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 100f;
        _camera.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
        _camera.transform.position = -_camera.transform.forward * 31f;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0.75f, 0.82f, 0.88f);
        Light light = Track(new GameObject("Upper left key")).AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.transform.rotation = Quaternion.Euler(50f, 40f, 0f);
        light.shadows = LightShadows.Soft;
        light.shadowBias = 0.03f;
        light.shadowNormalBias = 0.15f;
        RenderSettings.sun = light;
        _look = Track(new GameObject("Capture look")).AddComponent<LookController>();
        _settings = LookSettings.Default;
        _settings.fogStart = 70f;
        _settings.fogEnd = 100f;
        _settings.inkStrength = 0f;
        _settings.outlineWidthPixels = 1f;
        _look.settings = _settings;
        _material = Track(new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/Look.shader")));
        _material.SetColor("_BaseColor", new Color(0.6f, 0.78f, 0.3f));

        _ground = CreatePrimitive(PrimitiveType.Plane, "Receiving ground", Vector3.zero, Vector3.one * 3f, 1f);
        _sphere = CreatePrimitive(PrimitiveType.Sphere, "Casting sphere", new Vector3(-1f, 2.1f, 0f),
                                  Vector3.one * 4f, 1f);
        MaterialPropertyBlock sphereProperties = new MaterialPropertyBlock();
        sphereProperties.SetColor("_BaseColor", new Color(0.55f, 0.58f, 0.64f));
        sphereProperties.SetFloat("_HLNormalEdges", 1f);
        _sphere.GetComponent<Renderer>().SetPropertyBlock(sphereProperties);
        _target = Track(new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32));
        _target.Create();
        _texture = Track(new Texture2D(1080, 1920, TextureFormat.RGB24, false));
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    GameObject CreatePrimitive(PrimitiveType shape, string name, Vector3 position, Vector3 scale, float normalMask)
    {
        GameObject go = Track(GameObject.CreatePrimitive(shape));
        go.name = name;
        go.layer = 30;
        go.transform.position = position;
        go.transform.localScale = scale;
        Renderer meshRenderer = go.GetComponent<Renderer>();
        meshRenderer.sharedMaterial = _material;
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        properties.SetFloat("_HLNormalEdges", normalMask);
        meshRenderer.SetPropertyBlock(properties);
        return go;
    }

    byte[] Capture(string name)
    {
        _look.ApplyGlobals();
        RenderPipeline.SubmitRenderRequest(_camera, new RenderPipeline.StandardRequest { destination = _target });
        RenderTexture.active = _target;
        _texture.ReadPixels(new Rect(0f, 0f, 1080f, 1920f), 0, 0);
        _texture.Apply();
        byte[] bytes = _texture.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(_directory, name + ".png"), bytes);
        Assert.That(bytes.Length, Is.GreaterThan(10000));
        Debug.Log("[LookShaderTests] Capture: " + name + " on " + SystemInfo.graphicsDeviceType);
        return bytes;
    }

    // Measures the ink added around the sphere's middle row at one and two pixel outline widths
    void AssertOutlineWidths(float distance, bool isOrthographic)
    {
        _camera.orthographic = isOrthographic;
        _camera.orthographicSize = 16f;
        string label = distance.ToString(CultureInfo.InvariantCulture) + (isOrthographic ? "-ortho" : "");
        _camera.transform.position = -_camera.transform.forward * distance;
        _material.SetFloat("_HLOutlineWidthMultiplier", 0f);
        byte[] noOutline = Capture("beauty-look-outline-" + label + "-off");
        Color32[] uninked = _texture.GetPixels32();
        _material.SetFloat("_HLOutlineWidthMultiplier", 1f);
        byte[] one = Capture("beauty-look-outline-" + label + "-1px");
        Color32[] onePixels = _texture.GetPixels32();
        _material.SetFloat("_HLOutlineWidthMultiplier", 2f);
        byte[] two = Capture("beauty-look-outline-" + label + "-2px");
        Assert.That(one.SequenceEqual(noOutline), Is.False);
        Assert.That(two.SequenceEqual(one), Is.False);

        Color32[] twoPixels = _texture.GetPixels32();
        Vector3 center = _camera.WorldToViewportPoint(_sphere.transform.position);
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
        Debug.Log("[LookShaderTests] Beauty outline " + label + ": " + string.Join(",", widths));
    }

    // Draws the quad through the forward pass and reads back the centre pixel
    static Color ReadCentre(Material material, Mesh mesh, RenderTexture target, Texture2D texture,
        MaterialPropertyBlock block)
    {
        using (CommandBuffer command = new CommandBuffer())
        {
            command.SetRenderTarget(target);
            command.ClearRenderTarget(false, true, Color.magenta);
            command.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            command.DrawMesh(mesh, Matrix4x4.identity, material, 0, material.FindPass("HLForward"), block);
            Graphics.ExecuteCommandBuffer(command);
        }

        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0f, 0f, 16f, 16f), 0, 0);
        texture.Apply();
        return texture.GetPixel(8, 8);
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
