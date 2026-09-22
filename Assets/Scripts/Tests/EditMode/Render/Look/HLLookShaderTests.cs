using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Look
{
    public class HLLookShaderTests
    {
        [Test]
        public void DefaultMaterialUsesAllyGreenAndInstancing()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/HLLook_Default.mat");
            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader.name, Is.EqualTo("HL/Look/Primitive"));
            Assert.That(material.enableInstancing, Is.True);
            var color = material.GetColor("_BaseColor");
            Assert.That(color.r, Is.EqualTo(127/255f).Within(1e-6f));
            Assert.That(color.g, Is.EqualTo(201/255f).Within(1e-6f));
            Assert.That(color.b, Is.EqualTo(63/255f).Within(1e-6f));
            Assert.That(color.a, Is.EqualTo(1f));
        }

        [Test]
        public void PrimitivePassesImportAndCompileSupportedVariantsWhenGraphicsAvailable()
        {
            var shader = Shader.Find("HL/Look/Primitive");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                foreach (string pass in new[] { "HLForward", "HLShadowCaster", "HLOutline", "HLDepthOnly", "HLDepthNormals" })
                    Assert.That(material.FindPass(pass), Is.GreaterThanOrEqualTo(0), pass);
                Assert.That(shader.GetPropertyCount(), Is.EqualTo(1));
                Assert.That(shader.GetPropertyName(0), Is.EqualTo("_BaseColor"));
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    Assert.That(shader.isSupported, Is.True);
                    foreach (bool instanced in new[] { false, true })
                    {
                        foreach (string main in new[] { "", "_MAIN_LIGHT_SHADOWS", "_MAIN_LIGHT_SHADOWS_CASCADE", "_MAIN_LIGHT_SHADOWS_SCREEN" })
                        foreach (string soft in new[] { "", "_SHADOWS_SOFT", "_SHADOWS_SOFT_LOW", "_SHADOWS_SOFT_MEDIUM", "_SHADOWS_SOFT_HIGH" })
                            Compile(material, "HLForward", instanced, main, soft);
                        Compile(material, "HLShadowCaster", instanced);
                        Compile(material, "HLShadowCaster", instanced, "_CASTING_PUNCTUAL_LIGHT_SHADOW");
                        Compile(material, "HLOutline", instanced);
                        Compile(material, "HLDepthOnly", instanced);
                        Compile(material, "HLDepthNormals", instanced);
                        Compile(material, "HLDepthNormals", instanced, "_GBUFFER_NORMALS_OCT");
                    }
                    Debug.Log("HL primitive: synchronously compiled 52 pass/keyword combinations on " + SystemInfo.graphicsDeviceType);
                }
                AssertNoErrors(shader);
            }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void ScreenEdgesImportAndCompileBothNormalEncodingsWhenGraphicsAvailable()
        {
            var shader = Shader.Find("Hidden/HL/Look/DepthNormalOutline");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                Assert.That(material.FindPass("HLDepthNormalEdges"), Is.GreaterThanOrEqualTo(0));
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    Assert.That(shader.isSupported, Is.True);
                    Compile(material, "HLDepthNormalEdges", false);
                    Compile(material, "HLDepthNormalEdges", false, "_GBUFFER_NORMALS_OCT");
                    Debug.Log("HL edges: synchronously compiled both normal encodings on " + SystemInfo.graphicsDeviceType);
                }
                AssertNoErrors(shader);
            }
            finally { Object.DestroyImmediate(material); }
        }

        private static void Compile(Material material, string passName, bool instanced, params string[] keywords)
        {
            material.enableInstancing = instanced;
            material.shaderKeywords = keywords.Where(k => k.Length != 0)
                .Concat(instanced ? new[] { "INSTANCING_ON" } : new string[0]).ToArray();
            int pass = material.FindPass(passName);
            ShaderUtil.CompilePass(material, pass, true);
            Assert.That(ShaderUtil.IsPassCompiled(material, pass), Is.True, passName);
            AssertNoErrors(material.shader);
        }

        private static void AssertNoErrors(Shader shader)
        {
            var errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity == ShaderCompilerMessageSeverity.Error);
            Assert.That(errors.Select(m => m.message + " at " + m.file + ":" + m.line), Is.Empty);
        }
    }
}
