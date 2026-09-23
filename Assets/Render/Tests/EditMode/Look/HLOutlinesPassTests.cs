using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Look
{
    public class HLOutlinesPassTests
    {
        [Test]
        public void PassRequestsDepthAndNormalsOnlyForEdgeMaterial()
        {
            Type type = typeof(HLLookSettings).Assembly.GetType("HealerLike.Render.Look.HLOutlinesPass", true);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Look/HLOutlinesEdges.shader");
            Assert.That(shader, Is.Not.Null);
            Material material = new Material(shader);
            try
            {
                object pass = Activator.CreateInstance(type, new object[] { 1 << 7, material });
                object layerMask = type.GetField("_layerMask", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(pass);

                Assert.That(type.GetProperty("renderPassEvent").GetValue(pass).ToString(),
                            Is.EqualTo("AfterRenderingOpaques"));
                Assert.That(type.GetProperty("input").GetValue(pass).ToString(),
                            Does.Contain("Depth").And.Contain("Normal"));
                Assert.That(layerMask, Is.EqualTo(1 << 7));

                object hullOnly = Activator.CreateInstance(type, new object[] { -1, null });
                Assert.That(Convert.ToInt32(type.GetProperty("input").GetValue(hullOnly)), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
