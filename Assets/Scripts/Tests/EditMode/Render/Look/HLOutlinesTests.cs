using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Look
{
    public class HLOutlinesTests
    {
        private static Type FeatureType => typeof(HLLookSettings).Assembly.GetType("HealerLike.Render.Look.HLOutlines", true);

        [Test]
        public void FeatureDefaultsAndRecreationRetainShaderAndReleaseMaterial()
        {
            // Reflection keeps this existing test assembly independent of URP's transitive types.
            var feature = ScriptableObject.CreateInstance(FeatureType);
            try
            {
                Assert.That((int)(LayerMask)FeatureType.GetField("LayerMask").GetValue(feature), Is.EqualTo(-1));
                Assert.That(FeatureType.GetField("DepthNormalEdges").GetValue(feature), Is.EqualTo(true));
                var materialField = FeatureType.GetField("edgeMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
                FeatureType.GetMethod("Create").Invoke(feature, null);
                var first = (Material)materialField.GetValue(feature);
                Assert.That(first != null, Is.True);
                Assert.That(first.shader.name, Is.EqualTo("Hidden/HL/Look/DepthNormalOutline"));
                FeatureType.GetMethod("Create").Invoke(feature, null);
                Assert.That(first == null, Is.True, "Recreation must destroy the previous material.");
                var second = (Material)materialField.GetValue(feature);
                Assert.That(second != null, Is.True);
                FeatureType.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(bool) }, null).Invoke(feature, new object[] { true });
                Assert.That(second == null, Is.True);
                Assert.That(materialField.GetValue(feature), Is.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(feature); }
        }

        [Test]
        public void HullOnlyFeatureDoesNotRequestDepthNormalInputs()
        {
            var feature = ScriptableObject.CreateInstance(FeatureType);
            try
            {
                FeatureType.GetField("DepthNormalEdges").SetValue(feature, false);
                FeatureType.GetMethod("Create").Invoke(feature, null);
                object pass = FeatureType.GetField("pass", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(feature);
                Assert.That(Convert.ToInt32(pass.GetType().GetProperty("input").GetValue(pass)), Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(feature); }
        }
    }
}
