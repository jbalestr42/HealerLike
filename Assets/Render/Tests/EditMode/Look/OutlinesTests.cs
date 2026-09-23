using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Look
{

public class OutlinesTests
{
    static Type featureType
    {
        get { return typeof(LookSettings).Assembly.GetType("HealerLike.Render.Look.Outlines", true); }
    }

    static Shader LoadEdgeShader()
    {
        return AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Look/OutlinesEdges.shader");
    }

    [Test]
    public void FeatureDefaultsAndRecreationRetainShaderAndReleaseMaterial()
    {
        ScriptableObject feature = ScriptableObject.CreateInstance(featureType);
        try
        {
            Assert.That((int)(LayerMask)featureType.GetField("layerMask").GetValue(feature), Is.EqualTo(-1));
            Assert.That(featureType.GetField("depthNormalEdges").GetValue(feature), Is.EqualTo(true));

            FieldInfo materialField = featureType.GetField("_edgeMaterial",
                                                           BindingFlags.Instance | BindingFlags.NonPublic);
            featureType.GetProperty("edgeShader").SetValue(feature, LoadEdgeShader());
            featureType.GetMethod("Create").Invoke(feature, null);
            Material first = (Material)materialField.GetValue(feature);
            Assert.That(first != null, Is.True);
            Assert.That(first.GetVector("_HLEdgeDepth"), Is.EqualTo(new Vector4(1f, 31f, 1f, 0f)));
            Assert.That(first.GetVector("_HLEdgeNormals"), Is.EqualTo(new Vector4(55f, 35f, 1f, 0f)));

            featureType.GetField("depthThresholdWorld").SetValue(feature, -0.5f);
            featureType.GetField("useNormalEdgeMask").SetValue(feature, false);
            TestHelpers.SetPrivateField(feature, "_edgeMaterial", first);
            featureType.GetMethod("ApplyEdgeSettings").Invoke(feature, null);
            Assert.That(first.GetVector("_HLEdgeDepth").x, Is.EqualTo(0.001f));
            Assert.That(first.GetVector("_HLEdgeNormals").z, Is.Zero);
            Assert.That(first.shader.name, Is.EqualTo("Hidden/HL/Look/DepthNormalOutline"));

            featureType.GetMethod("Create").Invoke(feature, null);
            Assert.That(first == null, Is.True, "Recreation must destroy the previous material.");
            Material second = (Material)materialField.GetValue(feature);
            Assert.That(second != null, Is.True);

            MethodInfo dispose = featureType.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic,
                                                       null, new[] { typeof(bool) }, null);
            dispose.Invoke(feature, new object[] { true });
            Assert.That(second == null, Is.True);
            Assert.That(materialField.GetValue(feature), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(feature);
        }
    }

    [Test]
    public void HullOnlyFeatureDoesNotRequestDepthNormalInputs()
    {
        ScriptableObject feature = ScriptableObject.CreateInstance(featureType);
        try
        {
            featureType.GetField("depthNormalEdges").SetValue(feature, false);

            featureType.GetMethod("Create").Invoke(feature, null);

            object pass = featureType.GetField("_pass", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(feature);
            Assert.That(Convert.ToInt32(pass.GetType().GetProperty("input").GetValue(pass)), Is.Zero);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(feature);
        }
    }

    [Test]
    public void Create_WithoutEdgeShader_DrawsHullsOnly()
    {
        Outlines feature = ScriptableObject.CreateInstance<Outlines>();
        feature.edgeShader = null;

        feature.Create();

        FieldInfo materialField = typeof(Outlines).GetField("_edgeMaterial",
                                                              BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNull(materialField.GetValue(feature));

        UnityEngine.Object.DestroyImmediate(feature);
    }
}

}
