using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Look
{

// The shipped look materials are assets, not classes: what each one carries, and the body shade on a composed
// plant under the key light
public class LookMaterialTests
{
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
        Assert.That(Vector4.Distance(color, RenderTestAssets.LoadPalette().plantBody), Is.LessThan(0.000001f),
            "The material fallback must match the live palette");
        Assert.That(color.a, Is.EqualTo(1f));
    }

    [Test]
    public void ShadeControls_ShippedMaterials_KeepCreatureCelBandsAndGraphicGrassShadows()
    {
        Material body = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat");
        Material shared = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");
        Material grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat");
        Material stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Stone.mat");

        Assert.That(body.GetColor("_HLShadeTint").a, Is.EqualTo(1f));
        Assert.That(body.GetColor("_HLShadeTint").b, Is.GreaterThan(body.GetColor("_HLShadeTint").g));
        Assert.That(body.GetColor("_HLShadeTurnTint").g, Is.GreaterThan(body.GetColor("_HLShadeTurnTint").b));
        Assert.That(body.GetColor("_HLHighlightTint").a, Is.GreaterThan(0f));
        Assert.That(LookSettings.Default.toonSoftness, Is.LessThan(0.025f), "The primary split must stay short");
        foreach (Material creature in new[] { body, shared, stone })
        {
            float threshold = LookSettings.Default.toonThreshold + creature.GetFloat("_HLToonThresholdOffset");
            Assert.That(threshold, Is.InRange(0.65f, 0.8f), creature.name);
            Assert.That(creature.GetFloat("_HLHatchMultiplier"), Is.EqualTo(1f), creature.name);
            Assert.That(creature.GetFloat("_HLFaceHatch"), Is.EqualTo(1f), creature.name);
        }
        foreach (Material global in new[] { shared, stone, grass })
        {
            Assert.That(global.GetColor("_HLShadeTint").a, Is.Zero, global.name);
            Assert.That(global.GetColor("_HLShadeTurnTint").a, Is.Zero, global.name);
            Assert.That(global.GetColor("_HLHighlightTint").a, Is.Zero, global.name);
        }
        Assert.That(LookSettings.Default.toonThreshold + grass.GetFloat("_HLToonThresholdOffset"),
            Is.EqualTo(0.45f).Within(0.0001f));
        Assert.That(grass.GetFloat("_HLHatchMultiplier"), Is.GreaterThan(0.5f));
        Assert.That(grass.GetFloat("_HLFaceHatch"), Is.GreaterThan(0.5f));
    }

    [Test]
    public void Render_ComposedPlantUnderKeyLight_ContinuousPlantSurfaceKeepsShadeAwayFromAccentsAndRoots()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Requires graphics readback");
        }

        // The body's blue is hard to tell from the global shade, so a copy of the body material paints it magenta
        Material shared = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");
        Material body = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat");
        Material marked = Track(new Material(body));
        marked.SetColor("_HLShadeTint", new Color(1f, 0f, 1f, 1f));
        marked.SetColor("_HLShadeTurnTint", Color.clear);
        marked.SetColor("_HLHighlightTint", Color.clear);
        _scene.BuildKeyLight(25f, 8f);
        _scene.camera.transform.position += Vector3.up * 0.7f;
        Renderer[] renderers = CreateKeyLightPlant(shared, marked);

        ShowOnly(renderers, marked);
        _scene.Render();
        float bodyShade = LookTestScene.ShadeShare(_scene.texture);
        Vector2 shadeCentre = RegionCentre(_scene.texture, true);
        Vector2 litCentre = RegionCentre(_scene.texture, false);
        Vector3 viewLight = _scene.camera.transform.InverseTransformDirection(StageKeyLight.KeyDirection);
        Vector2 towardsLight = new Vector2(viewLight.x, viewLight.y).normalized;
        ShowOnly(renderers, shared);
        _scene.Render();
        float otherShade = LookTestScene.ShadeShare(_scene.texture);

        Assert.That(bodyShade, Is.InRange(0.15f, 0.65f)); // intentional cel coverage, not a half-sphere quota
        Assert.That(Vector2.Dot(litCentre - shadeCentre, towardsLight), Is.GreaterThan(3f),
            "The lit band must sit toward the sun relative to the marked shade band");
        Assert.That(otherShade, Is.LessThan(0.02f)); // tips and roots keep the global shade
        Debug.Log("[LookShaderTests] Plant body shade share " + bodyShade.ToString("F3") + ", other parts "
                  + otherShade.ToString("F3"));
    }

    // Locates the marked shade and the remaining green fill, ignoring the white clear and dark hatch ink.
    static Vector2 RegionCentre(Texture2D texture, bool shade)
    {
        Vector2 sum = Vector2.zero;
        int count = 0;
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            bool belongs = shade ? pixel.r > pixel.g : pixel.g > pixel.r && pixel.g > pixel.b;
            if (!belongs) continue;
            sum += new Vector2(i % texture.width, i / texture.width);
            count++;
        }
        Assert.That(count, Is.GreaterThan(0), shade ? "Shade region" : "Lit region");
        return sum / count;
    }

    // A composed plant on the key light layer, a heal accent so no lit part reads redder than green
    Renderer[] CreateKeyLightPlant(Material shared, Material body)
    {
        UnitChannels channels = RenderTestAssets.CreateChannels(LookSide.Plant, HeadKind.Bud);
        channels.accent = EffectFamily.Heal;
        CreatureRecipe recipe = Track(LookComposer.Compose(channels, RenderTestAssets.LoadLookVocabulary()));
        GameObject plant = Track(new GameObject("Key light plant"));
        CreatureRig rig = new CreatureRig();
        rig.Init(recipe, plant.transform, shared, body,
            AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(RenderTestAssets.MeshesPath), 1f);
        rig.Tick(0f, 0f, new FootFrame(Vector3.zero, Vector3.up, 1f));
        foreach (Transform part in plant.GetComponentsInChildren<Transform>(true))
        {
            part.gameObject.layer = LookTestScene.Layer;
        }
        return plant.GetComponentsInChildren<Renderer>(true);
    }

    static void ShowOnly(Renderer[] renderers, Material material)
    {
        foreach (Renderer partRenderer in renderers)
        {
            partRenderer.enabled = partRenderer.sharedMaterial == material;
        }
    }
}

}
