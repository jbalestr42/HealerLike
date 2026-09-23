using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Grass
{

public class HLGrassPaletteTests
{
    Material _lookMaterial;
    Material _bladeMaterial;

    [SetUp]
    public void SetUp()
    {
        _lookMaterial = new Material(Shader.Find("HL/Look/Primitive"));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_lookMaterial);
        if (_bladeMaterial != null)
        {
            Object.DestroyImmediate(_bladeMaterial);
        }
    }

    [Test]
    public void CreateBladeMaterial_LookMaterial_UsesLookShaderWithGrassKeyword()
    {
        _bladeMaterial = HLGrassPalette.CreateBladeMaterial(_lookMaterial);

        Assert.AreEqual("HL/Look/Primitive", _bladeMaterial.shader.name);
        Assert.IsTrue(_bladeMaterial.IsKeywordEnabled(HLGrassPalette.InstancedKeyword));
        Assert.IsTrue(_bladeMaterial.enableInstancing);
    }

    [Test]
    public void CreateBladeMaterial_LookMaterial_LeavesSourceUntouched()
    {
        _bladeMaterial = HLGrassPalette.CreateBladeMaterial(_lookMaterial);

        Assert.AreNotSame(_lookMaterial, _bladeMaterial);
        Assert.IsFalse(_lookMaterial.IsKeywordEnabled(HLGrassPalette.InstancedKeyword));
    }

    [Test]
    public void GrassBladeMaterial_Asset_IsTheLookShaderWithGrassKeywordAndDepthEdgesOnly()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat");

        Assert.AreEqual("HL/Look/Primitive", material.shader.name);
        Assert.IsTrue(material.IsKeywordEnabled(HLGrassPalette.InstancedKeyword));
        Assert.IsTrue(material.enableInstancing);
        Assert.AreEqual(0f, material.GetFloat("_HLNormalEdges"));
    }

    [Test]
    public void HealRingMaterial_Asset_IsTheRingShaderWithInstancing()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/HealRing.mat");

        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/HLGrassRing.shader"), material.shader);
        Assert.IsTrue(material.enableInstancing);
    }

    [Test]
    public void Apply_PropertyBlock_SetsEveryGrassColour()
    {
        MaterialPropertyBlock properties = new MaterialPropertyBlock();

        HLGrassPalette.Apply(properties);

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

        HLGrassPalette.Apply(properties);

        Assert.AreEqual((Vector4)tip, properties.GetVector("_HL_TipColor"));
    }
}
}
