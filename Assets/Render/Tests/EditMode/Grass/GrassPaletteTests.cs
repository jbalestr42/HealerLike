using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class GrassPaletteTests
{
    [Test]
    public void GrassBladeMaterial_Asset_IsTheLookShaderWithGrassKeywordAndDepthEdgesOnly()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat");

        Assert.AreEqual("HL/Look/Primitive", material.shader.name);
        Assert.IsTrue(material.IsKeywordEnabled(GrassPalette.InstancedKeyword));
        Assert.IsTrue(material.enableInstancing);
        Assert.AreEqual(0f, material.GetFloat("_HLNormalEdges"));
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

        GrassPalette.Apply(properties);

        foreach (string name in new[] { "_HL_RootColor", "_HL_MidColor", "_HL_TipColor", "_HL_HealColor", "_HL_SlateRoot", "_HL_SlateTip" })
        {
            Assert.IsTrue(properties.HasVector(name), name);
        }
    }

    [Test]
    public void Apply_PropertyBlock_ConvertsToWorkingColourSpace()
    {
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        Color tip = new Color32(169, 204, 96, 255);
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            tip = tip.linear;
        }

        GrassPalette.Apply(properties);

        Assert.AreEqual((Vector4)tip, properties.GetVector("_HL_TipColor"));
    }
}
}
