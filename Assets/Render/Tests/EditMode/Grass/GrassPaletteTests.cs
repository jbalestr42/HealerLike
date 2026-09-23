using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GrassPaletteTests
{
    [Test]
    public void GrassBladeMaterial_Asset_IsTheLookShaderWithGrassKeywordDepthEdgesOnlyAndNoHatch()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat");

        Assert.AreEqual("HL/Look/Primitive", material.shader.name);
        Assert.IsTrue(material.IsKeywordEnabled(GrassPalette.InstancedKeyword));
        Assert.IsTrue(material.enableInstancing);
        Assert.AreEqual(0f, material.GetFloat("_HLNormalEdges"));
        Assert.AreEqual(0f, material.GetFloat("_HLHatchMultiplier"));
    }

    [Test]
    public void HealRingMaterial_Asset_IsTheRingShaderWithInstancing()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/HealRing.mat");

        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/GrassRing.shader"), material.shader);
        Assert.IsTrue(material.enableInstancing);
    }

    [Test]
    public void Apply_PropertyBlock_SetsEveryGrassColour()
    {
        MaterialPropertyBlock properties = new MaterialPropertyBlock();

        new GrassPalette().Apply(properties);

        foreach (string name in new[] { "_HL_DarkGreen", "_HL_MidGreen", "_HL_LightGreen", "_HL_TipGreen", "_HL_HealColor", "_HL_SlateColor" })
        {
            Assert.IsTrue(properties.HasVector(name), name);
        }
    }

    [Test]
    public void Apply_PropertyBlock_ConvertsToWorkingColourSpace()
    {
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        GrassPalette palette = new GrassPalette();
        Color expected = palette.lightGreen;
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            expected = expected.linear;
        }

        palette.Apply(properties);

        Vector4 actual = properties.GetVector("_HL_LightGreen");
        Assert.AreEqual(expected.r, actual.x, 0.001f);
        Assert.AreEqual(expected.g, actual.y, 0.001f);
        Assert.AreEqual(expected.b, actual.z, 0.001f);
    }

    [Test]
    public void Greens_Default_AreTheReferenceCarpet()
    {
        GrassPalette palette = new GrassPalette();

        Assert.That((Color32)palette.darkGreen, Is.EqualTo(new Color32(76, 125, 81, 255))); // #4c7d51
        Assert.That((Color32)palette.midGreen, Is.EqualTo(new Color32(91, 144, 85, 255))); // #5b9055, the body
        Assert.That((Color32)palette.lightGreen, Is.EqualTo(new Color32(97, 154, 92, 255))); // #619a5c
        Assert.That((Color32)palette.tipGreen, Is.EqualTo(new Color32(140, 186, 108, 255))); // #8cba6c
    }

    [Test]
    public void Greens_Default_StayBetween35And50PercentSaturation()
    {
        GrassPalette palette = new GrassPalette();

        foreach (Color green in new[] { palette.darkGreen, palette.midGreen, palette.lightGreen, palette.tipGreen })
        {
            Color.RGBToHSV(green, out float hue, out float saturation, out float value);
            Assert.That(saturation, Is.InRange(0.35f, 0.5f), green.ToString());
        }
    }
}

}
