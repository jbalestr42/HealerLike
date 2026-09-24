using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HealerLike.Render.Look
{

public class OutlinesPassTests
{
    Material _edgeMaterial;

    [SetUp]
    public void SetUp()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/OutlinesEdges.shader");
        Assert.That(shader, Is.Not.Null);
        _edgeMaterial = new Material(shader);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(_edgeMaterial);
    }

    [Test]
    public void Constructor_EdgeMaterial_RequestsDepthAndNormalsAfterOpaques()
    {
        OutlinesPass pass = new OutlinesPass(1 << 7, _edgeMaterial);

        object layerMask = typeof(OutlinesPass).GetField("_layerMask", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .GetValue(pass);
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
}

}
