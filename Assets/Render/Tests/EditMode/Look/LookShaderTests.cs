using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Look
{

// Look.shader and its companions are assets, not classes: these tests import, compile and draw them on
// primitives. The shipped materials are in LookMaterialTests, the grass scenes in GrassDrawTests and the
// outline pipeline scene in OutlinesPassTests.
public class LookShaderTests
{
    static readonly string lookShaderPath = "Assets/Render/Shaders/Look.shader";

    LookTestScene _scene;

    [SetUp]
    public void SetUp()
    {
        _scene = new LookTestScene();
        _scene.Init();
    }

    [TearDown]
    public void TearDown()
    {
        _scene.Release();
    }

    ItemType Track<ItemType>(ItemType item) where ItemType : Object
    {
        return _scene.Track(item);
    }

    [Test]
    public void CompilePass_PrimitiveShader_ImportsAndCompilesSupportedVariants()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(lookShaderPath);
        Assert.That(shader, Is.Not.Null);
        Material material = Track(new Material(shader));

        string[] passes = { "HLForward", "HLShadowCaster", "HLOutline", "HLDepthOnly", "HLDepthNormals" };
        foreach (string pass in passes)
        {
            Assert.That(material.FindPass(pass), Is.GreaterThanOrEqualTo(0), pass);
        }

        Assert.That(shader.GetPropertyCount(), Is.EqualTo(11));
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
    public void Render_WorkingSpaceColourVector_ReachesPrimitiveShaderUnchanged()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Requires graphics readback");
        }

        // A real URP render initializes the light and shadow constants. A bare command-buffer draw can
        // inherit zero attenuation, which now correctly covers the fill with the explicit cast-shadow layer.
        MeshRenderer surface = BuildLitQuad();
        Color artist = new Color(0.4f, 0.6f, 0.8f, 1f);
        Color linear = artist.linear;

        MaterialPropertyBlock artistColor = new MaterialPropertyBlock();
        artistColor.SetColor("_BaseColor", artist);
        MaterialPropertyBlock linearColor = new MaterialPropertyBlock();
        linearColor.SetColor("_BaseColor", linear);
        MaterialPropertyBlock linearVector = new MaterialPropertyBlock();
        linearVector.SetVector("_BaseColor", linear);
        Color a = ReadCentre(surface, artistColor);
        Color b = ReadCentre(surface, linearColor);
        Color c = ReadCentre(surface, linearVector);

        Assert.That(((Vector4)a - (Vector4)c).magnitude, Is.LessThan(0.004f),
                    "Artist SetColor and working SetVector must agree");
        Assert.That(((Vector4)b - (Vector4)c).magnitude, Is.GreaterThan(0.2f),
                    "Double conversion must be detected by GPU readback");
        Assert.That(((Vector4)c - (Vector4)linear).magnitude, Is.LessThan(0.004f),
                    "Explicit working vector should reach GPU unchanged");
        Debug.Log("[LookShaderTests] Colour: active=" + QualitySettings.activeColorSpace
                  + " artist=" + artist.ToString("F5") + " linear=" + linear.ToString("F5")
                  + " A=" + a.ToString("F5") + " B=" + b.ToString("F5") + " C=" + c.ToString("F5"));
    }

    [Test]
    public void Render_SubPixelHatch_FadesWhileResolvedStrokesRemain()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Requires graphics readback");
        }

        // A lit white face with known unshadowed illumination, viewed away from the sun just enough to
        // exercise the ink mask. The fill remains white; the authored navy ink is its only darkening source.
        MeshRenderer surface = BuildLitQuad(60f);
        MaterialPropertyBlock white = new MaterialPropertyBlock();
        white.SetVector("_BaseColor", Vector4.one);
        LookSettings settings = _scene.look.settings;
        settings.inkScale = 0.0001f;
        settings.inkWidth = 0.04f;
        settings.inkStart = 0f;
        settings.inkRange = 1f;
        settings.densityMul = 1f;
        settings.inkWarp = 0f;
        settings.dashAmount = 0f;
        settings.dashScale = 1f;
        settings.inkDistStart = 10000f;
        settings.inkFarSpacing = 0f;
        _scene.look.settings = settings;
        ReadCentre(surface, white);
        float paper = MeanBrightness(_scene.texture);
        settings.inkStrength = 1f;
        _scene.look.settings = settings;
        ReadCentre(surface, white);
        float inked = MeanBrightness(_scene.texture);

        Assert.That(paper, Is.GreaterThan(0.98f), "The uninked control must be lit white, not cast-shadow blue");
        Assert.That(inked, Is.EqualTo(paper).Within(0.005f), "Unresolved strokes must not become solid ink");
        settings.inkScale = 0.25f;
        _scene.look.settings = settings;
        ReadCentre(surface, white);
        float resolved = MeanBrightness(_scene.texture);
        Assert.That(paper - resolved, Is.GreaterThan(0.1f), "Readable foreground strokes must remain");
        Debug.Log("[LookShaderTests] Sub-pixel hatch: paper=" + paper.ToString("F4")
                  + " unresolved=" + inked.ToString("F4") + " resolved=" + resolved.ToString("F4"));
    }

    [TestCase(1f, 0f)]
    [TestCase(1.15f, 0f)]
    [TestCase(1.15f, -0.1f)]
    public void Render_CelGradient_HasTwoPlateausAndABoundedTransition(float contrast, float materialOffset)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Requires graphics readback");
        }

        _scene.BuildKeyLight(20f, 4f);
        _scene.camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -4f), Quaternion.identity);
        LookSettings settings = _scene.look.settings;
        settings.inkStrength = 0f;
        settings.contrast = contrast;
        _scene.look.settings = settings;
        Material material = Track(new Material(AssetDatabase.LoadAssetAtPath<Shader>(lookShaderPath)));
        material.SetFloat("_HLToonThresholdOffset", materialOffset);
        Mesh mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        GameObject surface = Track(new GameObject("Cel gradient probe"));
        surface.layer = LookTestScene.Layer;
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        // A working-space plant green with a deliberate low blue channel. Fill contrast must keep it.
        Color source = new Color(0.21f, 0.58f, 0.05f, 1f);
        block.SetVector("_BaseColor", source);
        renderer.SetPropertyBlock(block);
        float[] dotProducts = { 0.05f, 0.2f, 0.35f, 0.45f, 0.55f, 0.7f, 0.9f };
        Color[] pixels = new Color[dotProducts.Length];
        for (int i = 0; i < dotProducts.Length; i++)
        {
            // Fixed N.L samples: two on each plateau and three in the transition. An authored offset moves
            // the whole interval; it must not stretch the gradient across the rest of the lit hemisphere.
            float angle = Mathf.Acos(dotProducts[i] + 2f * materialOffset) * Mathf.Rad2Deg;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * mesh.normals[0];
            RenderSettings.sun.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
            _scene.Render();
            pixels[i] = _scene.texture.GetPixel(128, 128).linear;
        }
        Debug.Log("[LookShaderTests] Cel band, contrast=" + contrast + " offset=" + materialOffset
            + " samples=" + string.Join(",", pixels.Select(pixel => pixel.ToString("F3"))));
        Assert.That(((Vector4)pixels[0] - (Vector4)pixels[1]).magnitude, Is.LessThan(0.008f),
            "The shade plateau must stop changing with the light angle");
        Assert.That(((Vector4)pixels[5] - (Vector4)pixels[6]).magnitude, Is.LessThan(0.008f),
            "The lit plateau must stop changing with the light angle");
        for (int i = 1; i < 5; i++)
        {
            Assert.That(pixels[i + 1].g - pixels[i].g, Is.GreaterThan(0.025f),
                "Only the bounded transition should grade between shade and light");
        }
        Assert.That(pixels[6].g - pixels[0].g, Is.GreaterThan(0.15f));
        foreach (int i in new[] { 5, 6 })
        {
            Assert.That(pixels[i].b, Is.GreaterThan(0.01f), "The lit plant's blue channel must survive contrast");
            Assert.That(pixels[i].r / pixels[i].g, Is.EqualTo(source.r / source.g).Within(0.01f));
            Assert.That(pixels[i].b / pixels[i].g, Is.EqualTo(source.b / source.g).Within(0.01f));
        }
    }

    // Both colour plumbing and hatch resolution need an actual URP light/shadow state, not globals left by
    // whichever camera rendered previously. All objects remain on the fixture's private layer.
    MeshRenderer BuildLitQuad(float lightAngle = 0f)
    {
        _scene.BuildKeyLight(20f, 4f);
        _scene.camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -4f), Quaternion.identity);
        LookSettings settings = _scene.look.settings;
        settings.toonThreshold = 0.1f;
        settings.toonSoftness = 0.01f;
        settings.inkStrength = 0f;
        settings.contrast = 1f;
        _scene.look.settings = settings;
        Mesh mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        GameObject surface = Track(new GameObject("Lit shader probe"));
        surface.layer = LookTestScene.Layer;
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = Track(new Material(AssetDatabase.LoadAssetAtPath<Shader>(lookShaderPath)));
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        Vector3 direction = Quaternion.Euler(0f, lightAngle, 0f) * mesh.normals[0];
        RenderSettings.sun.transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
        return renderer;
    }

    // The central square lies entirely inside the quad, excluding the clear colour and antialiased silhouette.
    static float MeanBrightness(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels(64, 64, 128, 128);
        float sum = 0f;
        foreach (Color pixel in pixels)
        {
            sum += (pixel.r + pixel.g + pixel.b) / 3f;
        }
        return sum / pixels.Length;
    }

    Color ReadCentre(Renderer surface, MaterialPropertyBlock block)
    {
        surface.SetPropertyBlock(block);
        _scene.Render();
        return _scene.texture.GetPixel(128, 128).linear;
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
