using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.Render.Look
{
    public class HLOutlinesPassTests
    {
        [Test]
        public void PassRequestsDepthAndNormalsOnlyForEdgeMaterial()
        {
            var type = typeof(HLLookSettings).Assembly.GetType("HealerLike.Render.Look.HLOutlinesPass", true);
            var shader = Shader.Find("Hidden/HL/Look/DepthNormalOutline");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                var pass = Activator.CreateInstance(type, new object[] { 1 << 7, material });
                Assert.That(type.GetProperty("renderPassEvent").GetValue(pass).ToString(), Is.EqualTo("AfterRenderingOpaques"));
                Assert.That(type.GetProperty("input").GetValue(pass).ToString(), Does.Contain("Depth").And.Contain("Normal"));
                Assert.That(type.GetField("layerMask", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pass), Is.EqualTo(1 << 7));
                var hullOnly = Activator.CreateInstance(type, new object[] { -1, null });
                Assert.That(Convert.ToInt32(type.GetProperty("input").GetValue(hullOnly)), Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }
    }
}
