using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GrassPaletteTests
{
    [Test]
    public void GrassBladeMaterial_Asset_IsTheLookShaderWithGrassKeywordAndNormalEdges()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat");

        Assert.AreEqual("HL/Look/Primitive", material.shader.name);
        Assert.IsTrue(material.IsKeywordEnabled(GrassPalette.InstancedKeyword));
        Assert.IsTrue(material.enableInstancing);
        Assert.AreEqual(1f, material.GetFloat("_HLNormalEdges"));
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

        foreach (string name in new[] { "_HL_DarkGreen", "_HL_MidGreen", "_HL_LightGreen", "_HL_HealColor", "_HL_SlateColor" })
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
    public void LightGreen_Default_IsThePlantBaseColour()
    {
        Material plant = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Default.mat");

        Color lightGreen = new GrassPalette().lightGreen;

        Color baseColor = plant.GetColor("_BaseColor");
        Assert.AreEqual(baseColor.r, lightGreen.r, 0.005f);
        Assert.AreEqual(baseColor.g, lightGreen.g, 0.005f);
        Assert.AreEqual(baseColor.b, lightGreen.b, 0.005f);
    }
}

}
