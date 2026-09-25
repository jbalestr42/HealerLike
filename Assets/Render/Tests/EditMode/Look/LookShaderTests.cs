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
    public void DrawMesh_WorkingSpaceColourVector_ReachesPrimitiveShaderUnchanged()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Requires graphics readback");
        }

        Material material = Track(new Material(AssetDatabase.LoadAssetAtPath<Shader>(lookShaderPath)));
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
        Debug.Log("[LookShaderTests] Colour: active=" + QualitySettings.activeColorSpace
                  + " artist=" + artist.ToString("F5") + " linear=" + linear.ToString("F5")
                  + " A=" + a.ToString("F5") + " B=" + b.ToString("F5") + " C=" + c.ToString("F5"));
    }

    [Test]
    public void DrawMesh_SubPixelHatch_FadesWhileResolvedStrokesRemain()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Requires graphics readback");
        }

        Material material = Track(new Material(AssetDatabase.LoadAssetAtPath<Shader>(lookShaderPath)));
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

        Assert.That(inked, Is.EqualTo(paper).Within(0.005f), "Unresolved strokes must not become solid ink");
        Shader.SetGlobalFloat("_HLInkScale", 0.5f);
        ReadCentre(material, mesh, target, texture, white);
        float resolved = MeanBrightness(texture);
        Assert.That(paper - resolved, Is.GreaterThan(0.1f), "Readable foreground strokes must remain");
        Debug.Log("[LookShaderTests] Sub-pixel hatch: paper=" + paper.ToString("F4")
                  + " inked=" + inked.ToString("F4"));
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
