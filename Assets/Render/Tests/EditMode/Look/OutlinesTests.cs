using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Look
{

public class OutlinesTests
{
    static readonly FieldInfo edgeMaterialField = typeof(Outlines).GetField("_edgeMaterial",
                                                                            BindingFlags.Instance | BindingFlags.NonPublic);

    Outlines _feature;

    [SetUp]
    public void SetUp()
    {
        _feature = ScriptableObject.CreateInstance<Outlines>();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(_feature);
    }

    static Shader LoadEdgeShader()
    {
        return AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Look/OutlinesEdges.shader");
    }

    [Test]
    public void Create_Recreated_KeepsDefaultsAndReleasesPreviousMaterial()
    {
        Assert.That((int)_feature.layerMask, Is.EqualTo(-1));
        Assert.That(_feature.depthNormalEdges, Is.EqualTo(true));

        _feature.edgeShader = LoadEdgeShader();
        _feature.Create();
        Material first = (Material)edgeMaterialField.GetValue(_feature);
        Assert.That(first != null, Is.True);
        Assert.That(first.GetVector("_HLEdgeDepth"), Is.EqualTo(new Vector4(1f, 31f, 1f, 0f)));
        Assert.That(first.GetVector("_HLEdgeNormals"), Is.EqualTo(new Vector4(55f, 35f, 1f, 0f)));

        _feature.depthThresholdWorld = -0.5f;
        _feature.useNormalEdgeMask = false;
        _feature.ApplyEdgeSettings();
        Assert.That(first.GetVector("_HLEdgeDepth").x, Is.EqualTo(0.001f));
        Assert.That(first.GetVector("_HLEdgeNormals").z, Is.Zero);
        Assert.That(first.shader.name, Is.EqualTo("Hidden/HL/Look/DepthNormalOutline"));

        _feature.Create();
        Assert.That(first == null, Is.True, "Recreation must destroy the previous material.");
        Material second = (Material)edgeMaterialField.GetValue(_feature);
        Assert.That(second != null, Is.True);

        _feature.Dispose();
        Assert.That(second == null, Is.True);
        Assert.That(edgeMaterialField.GetValue(_feature), Is.Null);
    }

    [Test]
    public void Create_HullOnly_RequestsNoDepthNormalInputs()
    {
        _feature.depthNormalEdges = false;

        _feature.Create();

        OutlinesPass pass = (OutlinesPass)typeof(Outlines).GetField("_pass", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(_feature);
        Assert.That(Convert.ToInt32(pass.input), Is.Zero);
    }

    [Test]
    public void Create_WithoutEdgeShader_DrawsHullsOnly()
    {
        _feature.edgeShader = null;

        _feature.Create();

        Assert.IsNull(edgeMaterialField.GetValue(_feature));
    }
}

}
