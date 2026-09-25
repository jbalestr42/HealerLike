using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
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
        Assert.That(color.r, Is.EqualTo(127 / 255f).Within(0.000001f));
        Assert.That(color.g, Is.EqualTo(201 / 255f).Within(0.000001f));
        Assert.That(color.b, Is.EqualTo(63 / 255f).Within(0.000001f));
        Assert.That(color.a, Is.EqualTo(1f));
    }

    [Test]
    public void ShadeControls_ShippedMaterials_SculptCreaturesAndKeepGrassSelfShadeQuiet()
    {
        Material body = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat");
        Material shared = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");
        Material grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat");
        Material stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Stone.mat");

        Assert.That(body.GetFloat("_HLToonThresholdOffset"), Is.GreaterThan(0f));
        Assert.That(body.GetColor("_HLShadeTint").a, Is.GreaterThan(0f));
        foreach (Material global in new[] { shared, stone })
        {
            Assert.That(global.GetFloat("_HLToonThresholdOffset"), Is.Zero, global.name);
            Assert.That(global.GetColor("_HLShadeTint").a, Is.Zero, global.name); // zero strength keeps the global tint
        }
        foreach (Material creature in new[] { body, shared, stone })
        {
            Assert.That(creature.GetFloat("_HLLitSculpt"), Is.GreaterThan(0.5f), creature.name);
        }
        Assert.That(body.GetColor("_HLShadeTint").g, Is.GreaterThan(body.GetColor("_HLShadeTint").b));
        Assert.That(stone.GetFloat("_HLFaceHatch"), Is.GreaterThan(shared.GetFloat("_HLFaceHatch")));
        Assert.That(grass.GetFloat("_HLToonThresholdOffset"), Is.Zero);
        Assert.That(grass.GetFloat("_HLFaceHatch"), Is.Zero);
        Assert.That(grass.GetColor("_HLShadeTint").a, Is.GreaterThan(0f));
        Assert.That(grass.GetColor("_HLShadeTint").g, Is.GreaterThan(grass.GetColor("_HLShadeTint").b));
    }

    [Test]
    public void Render_ComposedPlantUnderKeyLight_OnlyTheBodyTakesTheBodyShade()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Assert.Ignore("Requires graphics readback");
        }

        // The body's teal is hard to tell from the global shade, so a copy of the body material paints it magenta
        Material shared = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");
        Material body = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat");
        Material marked = Track(new Material(body));
        marked.SetColor("_HLShadeTint", new Color(1f, 0f, 1f, 1f));
        _scene.BuildKeyLight(25f, 8f);
        _scene.camera.transform.position += Vector3.up * 0.7f;
        Renderer[] renderers = CreateKeyLightPlant(shared, marked);

        ShowOnly(renderers, marked);
        _scene.Render();
        float bodyShade = LookTestScene.ShadeShare(_scene.texture);
        ShowOnly(renderers, shared);
        _scene.Render();
        float otherShade = LookTestScene.ShadeShare(_scene.texture);

        Assert.That(bodyShade, Is.InRange(0.25f, 0.75f)); // about half the body past its later split
        Assert.That(otherShade, Is.LessThan(0.02f)); // heads, stems and roots keep the global shade
        Debug.Log("[LookShaderTests] Plant body shade share " + bodyShade.ToString("F3") + ", other parts "
                  + otherShade.ToString("F3"));
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
