using System;
using System.Globalization;
using System.IO;
using System.Linq;
using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Look
{

// The pass alone, then the opt-in captures of the look through a pipeline running only the outline feature
public class OutlinesPassTests
{
    Material _edgeMaterial;
    LookTestScene _scene;

    // The capture scene, built only by the opt-in capture tests
    Outlines _outlines;
    LookSettings _settings;
    Material _material;
    GameObject _ground;
    GameObject _sphere;
    string _directory;

    [SetUp]
    public void SetUp()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/OutlinesEdges.shader");
        Assert.That(shader, Is.Not.Null);
        _edgeMaterial = new Material(shader);
        _scene = new LookTestScene();
        _scene.Init();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(_edgeMaterial);
        _scene.Release();
    }

    [Test]
    public void Constructor_EdgeMaterial_RequestsDepthAndNormalsAfterOpaques()
    {
        OutlinesPass pass = new OutlinesPass(1 << 7, _edgeMaterial);

        System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance
                                               | System.Reflection.BindingFlags.NonPublic;
        object layerMask = typeof(OutlinesPass).GetField("_layerMask", flags).GetValue(pass);
        Assert.That(pass.renderPassEvent, Is.EqualTo(RenderPassEvent.AfterRenderingOpaques));
        Assert.That(pass.input.ToString(), Does.Contain("Depth").And.Contain("Normal"));
        Assert.That(layerMask, Is.EqualTo(1 << 7));
    }

    [Test]
    public void Constructor_NoEdgeMaterial_RequestsNoInputs()
    {
        OutlinesPass hullOnly = new OutlinesPass(-1, null);

        Assert.That(Convert.ToInt32(hullOnly.input), Is.Zero);
    }

    [Test]
    public void RecordRenderGraph_BeautyScene_KeepsGridInsideBoardAndOutlinesAtPixelWidth()
    {
        IgnoreUnlessCapturing("RENDER_CAPTURE_BEAUTY");
        BuildCaptureScene(StagePlay.CaptureFolder);
        Camera camera = _scene.camera;
        camera.transform.rotation = Quaternion.Euler(73.7f, 0f, 0f);
        camera.transform.position = -camera.transform.forward * 43.837f;
        Material groundMaterial = _scene.Track(new Material(_material));
        _ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
        groundMaterial.SetFloat("_HLGroundGrid", 1f);
        Shader.SetGlobalVector("_HLGridOrigin", new Vector4(-8f, 0f, -8f, 0f));
        Shader.SetGlobalVector("_HLGridExtent", new Vector4(16f, 0f, 16f, 0f));
        Shader.SetGlobalFloat("_HLGridCell", 1f);
        Shader.SetGlobalFloat("_HLGridStrength", 0f);

        byte[] gridOff = Capture("beauty-look-grid-off");
        Color32[] gridOffPixels = _scene.texture.GetPixels32();
        Shader.SetGlobalFloat("_HLGridStrength", 0.16f);
        byte[] gridOn = Capture("beauty-look-grid");
        Assert.That(gridOn.SequenceEqual(gridOff), Is.False);

        Color32[] gridOnPixels = _scene.texture.GetPixels32();
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
        Debug.Log("[OutlinesPassTests] Beauty grid: " + changedGridPixels + " changed pixels, " + escapedGridPixels
                  + " outside bounds");

        _settings.fogStart = 43.837f;
        _settings.fogEnd = 50.356f;
        _scene.look.settings = _settings;
        Capture("beauty-look-grid-fade");
        Shader.SetGlobalFloat("_HLGridStrength", 0f);
        Capture("beauty-look-fog");

        _settings.fogStart = 70f;
        _settings.fogEnd = 100f;
        _settings.inkStrength = 1f;
        _settings.inkWarp = 0f;
        _settings.dashAmount = 0f;
        _scene.look.settings = _settings;
        foreach (float distance in new[] { 42.8f, 43.837f, 47.8f })
        {
            camera.transform.position = -camera.transform.forward * distance;
            Capture("beauty-look-hatch-" + distance.ToString(CultureInfo.InvariantCulture));
        }

        _settings.inkStrength = 0f;
        _scene.look.settings = _settings;
        _outlines.layerMask = 1 << LookTestScene.Layer;
        _outlines.Create();
        float[] distances = { 26f, 42.8f, 47.8f, 43.837f };
        bool[] orthographic = { false, false, false, true };
        for (int s = 0; s < distances.Length; s++)
        {
            AssertOutlineWidths(distances[s], orthographic[s]);
        }
    }

    [Test]
    public void RecordRenderGraph_PortraitScene_ShowsShadowsAndMasksNormalEdges()
    {
        IgnoreUnlessCapturing("RENDER_CAPTURE_PORTRAIT");
        BuildCaptureScene(StagePlay.CaptureFolder);

        Capture("render-look-shadow");
        Color32[] pixels = _scene.texture.GetPixels32();
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

        Texture2D left = _scene.Track(new Texture2D(2, 2));
        left.LoadImage(off);
        Texture2D pair = _scene.Track(new Texture2D(2160, 1920, TextureFormat.RGB24, false));
        pair.SetPixels(0, 0, 1080, 1920, left.GetPixels());
        pair.SetPixels(1080, 0, 1080, 1920, _scene.texture.GetPixels());
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
        _scene.look.settings = _settings;
        Capture("render-look-hatch-31");
        foreach (float distance in new[] { 26f, 40f })
        {
            _scene.camera.transform.position = -_scene.camera.transform.forward * distance;
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

    // A private pipeline copy with only the outline feature, a key light, a look, a ground and a casting sphere
    void BuildCaptureScene(string directory)
    {
        _outlines = _scene.Track(ScriptableObject.CreateInstance<Outlines>());
        _outlines.layerMask = 0;
        _outlines.depthNormalEdges = false;
        _outlines.Create();
        _scene.UsePipeline(_outlines);
        _scene.CreateCamera("Portrait camera", 40f, 1080f / 1920f, 50f, 31f, new Color(0.75f, 0.82f, 0.88f));
        Light light = _scene.CreateKeyLight("Upper left key", Quaternion.Euler(50f, 40f, 0f));
        light.shadowBias = 0.03f;
        light.shadowNormalBias = 0.15f;
        _settings = LookSettings.Default;
        _settings.fogStart = 70f;
        _settings.fogEnd = 100f;
        _settings.inkStrength = 0f;
        _settings.outlineWidthPixels = 1f;
        _scene.CreateLook("Capture look", _settings);
        Shader look = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/Look.shader");
        _material = _scene.Track(new Material(look));
        _material.SetColor("_BaseColor", new Color(0.6f, 0.78f, 0.3f));

        _ground = CreatePrimitive(PrimitiveType.Plane, "Receiving ground", Vector3.zero, Vector3.one * 3f, 1f);
        _sphere = CreatePrimitive(PrimitiveType.Sphere, "Casting sphere", new Vector3(-1f, 2.1f, 0f),
                                  Vector3.one * 4f, 1f);
        MaterialPropertyBlock sphereProperties = new MaterialPropertyBlock();
        sphereProperties.SetColor("_BaseColor", new Color(0.55f, 0.58f, 0.64f));
        sphereProperties.SetFloat("_HLNormalEdges", 1f);
        _sphere.GetComponent<Renderer>().SetPropertyBlock(sphereProperties);
        _scene.CreateTarget(1080, 1920);
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    GameObject CreatePrimitive(PrimitiveType shape, string name, Vector3 position, Vector3 scale, float normalMask)
    {
        GameObject go = _scene.Track(GameObject.CreatePrimitive(shape));
        go.name = name;
        go.layer = LookTestScene.Layer;
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
        _scene.Render();
        byte[] bytes = _scene.texture.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(_directory, name + ".png"), bytes);
        Assert.That(bytes.Length, Is.GreaterThan(10000));
        Debug.Log("[OutlinesPassTests] Capture: " + name + " on " + SystemInfo.graphicsDeviceType);
        return bytes;
    }

    // Measures the ink added around the sphere's middle row at one and two pixel outline widths
    void AssertOutlineWidths(float distance, bool isOrthographic)
    {
        Camera camera = _scene.camera;
        camera.orthographic = isOrthographic;
        camera.orthographicSize = 16f;
        string label = distance.ToString(CultureInfo.InvariantCulture);
        if (isOrthographic)
        {
            label += "-ortho";
        }

        camera.transform.position = -camera.transform.forward * distance;
        _material.SetFloat("_HLOutlineWidthMultiplier", 0f);
        byte[] noOutline = Capture("beauty-look-outline-" + label + "-off");
        Color32[] uninked = _scene.texture.GetPixels32();
        _material.SetFloat("_HLOutlineWidthMultiplier", 1f);
        byte[] one = Capture("beauty-look-outline-" + label + "-1px");
        Color32[] onePixels = _scene.texture.GetPixels32();
        _material.SetFloat("_HLOutlineWidthMultiplier", 2f);
        byte[] two = Capture("beauty-look-outline-" + label + "-2px");
        Assert.That(one.SequenceEqual(noOutline), Is.False);
        Assert.That(two.SequenceEqual(one), Is.False);

        Color32[] twoPixels = _scene.texture.GetPixels32();
        Vector3 center = camera.WorldToViewportPoint(_sphere.transform.position);
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
        Debug.Log("[OutlinesPassTests] Beauty outline " + label + ": " + string.Join(",", widths));
    }
}

}
