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

    // Built only by the opt-in capture tests
    readonly OutlinesCaptureScene _capture = new OutlinesCaptureScene();

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
        _capture.Build(_scene, StagePlay.CaptureFolder);
        Camera camera = _scene.camera;
        camera.transform.rotation = Quaternion.Euler(73.7f, 0f, 0f);
        camera.transform.position = -camera.transform.forward * 43.837f;
        Material groundMaterial = _scene.Track(new Material(_capture.material));
        _capture.ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
        groundMaterial.SetFloat("_HLGroundGrid", 1f);
        Shader.SetGlobalVector("_HLGridOrigin", new Vector4(-8f, 0f, -8f, 0f));
        Shader.SetGlobalVector("_HLGridExtent", new Vector4(16f, 0f, 16f, 0f));
        Shader.SetGlobalFloat("_HLGridCell", 1f);
        Shader.SetGlobalFloat("_HLGridStrength", 0f);

        byte[] gridOff = _capture.Capture("beauty-look-grid-off");
        Color32[] gridOffPixels = _scene.texture.GetPixels32();
        Shader.SetGlobalFloat("_HLGridStrength", 0.16f);
        byte[] gridOn = _capture.Capture("beauty-look-grid");
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

        _capture.settings.fogStart = 43.837f;
        _capture.settings.fogEnd = 50.356f;
        _scene.look.settings = _capture.settings;
        _capture.Capture("beauty-look-grid-fade");
        Shader.SetGlobalFloat("_HLGridStrength", 0f);
        _capture.Capture("beauty-look-fog");

        _capture.settings.fogStart = 70f;
        _capture.settings.fogEnd = 100f;
        _capture.settings.inkStrength = 1f;
        _capture.settings.inkWarp = 0f;
        _capture.settings.dashAmount = 0f;
        _scene.look.settings = _capture.settings;
        foreach (float distance in new[] { 42.8f, 43.837f, 47.8f })
        {
            camera.transform.position = -camera.transform.forward * distance;
            _capture.Capture("beauty-look-hatch-" + distance.ToString(CultureInfo.InvariantCulture));
        }

        _capture.settings.inkStrength = 0f;
        _scene.look.settings = _capture.settings;
        _capture.outlines.layerMask = 1 << LookTestScene.Layer;
        _capture.outlines.Create();
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
        _capture.Build(_scene, StagePlay.CaptureFolder);

        _capture.Capture("render-look-shadow");
        Color32[] pixels = _scene.texture.GetPixels32();
        Assert.That(pixels.Count(c => c.b > c.g * 1.3f && c.b > c.r * 1.5f), Is.GreaterThan(1000));

        for (int z = 0; z < 12; z++)
        {
            for (int x = 0; x < 18; x++)
            {
                Vector3 bladePosition = new Vector3((x - 9) * 0.38f, 0.3f, -2.8f - z * 0.38f);
                GameObject blade = _capture.CreatePrimitive(PrimitiveType.Cube, "Masked blade", bladePosition,
                    new Vector3(0.045f, 0.6f, 0.09f), 0f);
                blade.transform.rotation = Quaternion.Euler(0f, (x * 37 + z * 23) % 180, (x % 3 - 1) * 15);
                blade.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        byte[] off = _capture.Capture("render-look-edges-off");
        _capture.outlines.depthNormalEdges = true;
        _capture.outlines.Create();
        byte[] on = _capture.Capture("render-look-edges-on");
        Assert.That(on.SequenceEqual(off), Is.False, "Screen pass must change the image.");

        Texture2D left = _scene.Track(new Texture2D(2, 2));
        left.LoadImage(off);
        Texture2D pair = _scene.Track(new Texture2D(2160, 1920, TextureFormat.RGB24, false));
        pair.SetPixels(0, 0, 1080, 1920, left.GetPixels());
        pair.SetPixels(1080, 0, 1080, 1920, _scene.texture.GetPixels());
        pair.Apply();
        string sideBySide = Path.Combine(_capture.directory, "render-look-edges-side-by-side.png");
        File.WriteAllBytes(sideBySide, pair.EncodeToPNG());

        _capture.outlines.useNormalEdgeMask = false;
        _capture.outlines.ApplyEdgeSettings();
        byte[] unmasked = _capture.Capture("render-look-edges-unmasked");
        Assert.That(unmasked.SequenceEqual(on), Is.False,
                    "Mask must suppress normal edges independently of the depth threshold.");

        _capture.outlines.useNormalEdgeMask = true;
        _capture.outlines.ApplyEdgeSettings();
        _capture.settings.inkStrength = 1f;
        _capture.settings.inkWarp = 0f;
        _capture.settings.dashAmount = 0f;
        _scene.look.settings = _capture.settings;
        _capture.Capture("render-look-hatch-31");
        foreach (float distance in new[] { 26f, 40f })
        {
            _scene.camera.transform.position = -_scene.camera.transform.forward * distance;
            _capture.Capture("render-look-hatch-" + distance);
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
        _capture.material.SetFloat("_HLOutlineWidthMultiplier", 0f);
        byte[] noOutline = _capture.Capture("beauty-look-outline-" + label + "-off");
        Color32[] uninked = _scene.texture.GetPixels32();
        _capture.material.SetFloat("_HLOutlineWidthMultiplier", 1f);
        byte[] one = _capture.Capture("beauty-look-outline-" + label + "-1px");
        Color32[] onePixels = _scene.texture.GetPixels32();
        _capture.material.SetFloat("_HLOutlineWidthMultiplier", 2f);
        byte[] two = _capture.Capture("beauty-look-outline-" + label + "-2px");
        Assert.That(one.SequenceEqual(noOutline), Is.False);
        Assert.That(two.SequenceEqual(one), Is.False);

        Color32[] twoPixels = _scene.texture.GetPixels32();
        Vector3 center = camera.WorldToViewportPoint(_capture.sphere.transform.position);
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
