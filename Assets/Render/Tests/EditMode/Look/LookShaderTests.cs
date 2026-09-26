using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Look
{
    public class LookShaderTests
    {
        static readonly string lookShaderPath = "Assets/Render/Shaders/Look.shader";

        LookTestScene _scene;

        [SetUp]
        public void SetUp()
        {
            _scene = new LookTestScene();
            _scene.Init();
        }

        [TearDown]
        public void TearDown()
        {
            _scene.Release();
        }

        ItemType Track<ItemType>(ItemType item) where ItemType : Object
        {
            return _scene.Track(item);
        }

        [Test]
        public void CompilePass_PrimitiveShader_ImportsAndCompilesSupportedVariants()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(lookShaderPath);
            Assert.That(shader, Is.Not.Null);
            Material material = Track(new Material(shader));

            string[] passes = { "HLForward", "HLShadowCaster", "HLOutline", "HLDepthOnly", "HLDepthNormals" };
            foreach (string pass in passes)
            {
                Assert.That(material.FindPass(pass), Is.GreaterThanOrEqualTo(0), pass);
            }

            Assert.That(shader.GetPropertyCount(), Is.EqualTo(19));
            Assert.That(shader.GetPropertyName(3), Is.EqualTo("_BaseColor"));
            // The ground state's colours on grass: ash, dead grass and the heal's glow
            Assert.That(shader.FindPropertyIndex("_HLAshColor"), Is.GreaterThanOrEqualTo(0));
            Assert.That(shader.FindPropertyIndex("_HLWiltColor"), Is.GreaterThanOrEqualTo(0));
            Assert.That(shader.FindPropertyIndex("_HLGlowColor"), Is.GreaterThanOrEqualTo(0));
            Assert.That(shader.FindPropertyIndex("_HLBlightColor"), Is.GreaterThanOrEqualTo(0));
            Assert.That(shader.FindPropertyIndex("_HLFrostColor"), Is.GreaterThanOrEqualTo(0));
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
            {
                Assert.That(shader.isSupported, Is.True);
                string[] mainKeywords =
                {
                    "", "_MAIN_LIGHT_SHADOWS", "_MAIN_LIGHT_SHADOWS_CASCADE", "_MAIN_LIGHT_SHADOWS_SCREEN"
                };
                string[] softKeywords =
                {
                    "", "_SHADOWS_SOFT", "_SHADOWS_SOFT_LOW", "_SHADOWS_SOFT_MEDIUM", "_SHADOWS_SOFT_HIGH"
                };
                foreach (bool instanced in new[] { false, true })
                {
                    foreach (string main in mainKeywords)
                    {
                        foreach (string soft in softKeywords)
                        {
                            Compile(material, "HLForward", instanced, main, soft);
                        }
                    }

                    Compile(material, "HLShadowCaster", instanced);
                    Compile(material, "HLShadowCaster", instanced, "_CASTING_PUNCTUAL_LIGHT_SHADOW");
                    Compile(material, "HLOutline", instanced);
                    Compile(material, "HLDepthOnly", instanced);
                    Compile(material, "HLDepthNormals", instanced);
                    Compile(material, "HLDepthNormals", instanced, "_GBUFFER_NORMALS_OCT");
                }

                Debug.Log("[LookShaderTests] Primitive: synchronously compiled 52 pass/keyword combinations on "
                          + SystemInfo.graphicsDeviceType);
            }

            AssertNoErrors(shader);
        }

        static void Compile(Material material, string passName, bool instanced, params string[] keywords)
        {
            material.enableInstancing = instanced;
            material.shaderKeywords = keywords.Where(k => k.Length != 0)
                .Concat(instanced ? new[] { "INSTANCING_ON" } : new string[0]).ToArray();
            int pass = material.FindPass(passName);
            ShaderUtil.CompilePass(material, pass, true);
            Assert.That(ShaderUtil.IsPassCompiled(material, pass), Is.True, passName);
            AssertNoErrors(material.shader);
        }

        static void AssertNoErrors(Shader shader)
        {
            IEnumerable<ShaderMessage> errors = ShaderUtil.GetShaderMessages(shader)
                .Where(m => m.severity == ShaderCompilerMessageSeverity.Error);
            Assert.That(errors.Select(m => m.message + " at " + m.file + ":" + m.line), Is.Empty);
        }
    }
}
