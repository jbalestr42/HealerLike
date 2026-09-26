using System.Linq;
using HealerLike.Render.Look;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{
    public class GrassMaterialTests : AGrassDrawFixture
    {
        [Test]
        public void GrassBladeMaterial_ApprovedPalette_KeepsGrassGreenAndSharedPlantSurface()
        {
            Material grass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat");
            Material plant = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat");

            Assert.AreSame(plant.shader, grass.shader);
            Assert.IsTrue(grass.IsKeywordEnabled(instancedKeyword));
            CollectionAssert.AreEquivalent(plant.shaderKeywords.Append(instancedKeyword), grass.shaderKeywords);
            Assert.AreEqual(plant.enableInstancing, grass.enableInstancing);
            Assert.AreEqual(plant.renderQueue, grass.renderQueue);
            Assert.That(grass.GetFloat("_HLNormalEdges"), Is.Zero);
            Color approved = new Color32(55, 191, 104, 255);
            Assert.That(Vector4.Distance(grass.GetColor("_BaseColor"), approved), Is.LessThan(0.000001f),
                "The approved grass palette is #37BF68, independently of the creature's lime palette.");
            foreach (string property in new[] { "_HLToonThresholdOffset", "_HLHatchMultiplier", "_HLFaceHatch",
                "_HLMeadowVariation", "_HLGrassTipLight", "_HLHighlightWidth" })
            {
                Assert.That(grass.GetFloat(property), Is.EqualTo(plant.GetFloat(property)), property);
            }
            foreach (string property in new[] { "_HLShadeTint", "_HLShadeTurnTint", "_HLHighlightTint" })
            {
                Assert.That(Vector4.Distance(grass.GetColor(property), plant.GetColor(property)),
                    Is.LessThan(0.000001f), property);
            }
        }

        [Test]
        public void Show_IndirectTuft_MatchesEquivalentPlantMaterialAcrossRealLightAngles()
        {
            _scene.BuildKeyLight(20f, 4f);
            _scene.camera.orthographic = true;
            _scene.camera.orthographicSize = 0.65f;
            _scene.camera.transform.SetPositionAndRotation(new Vector3(0f, 0.5f, -4f), Quaternion.identity);
            Material body = _scene.Track(new Material(
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Look/Look_Body.mat")));
            body.SetFloat("_HLNormalEdges", 0f);
            Material grass = _scene.Track(new Material(body));
            grass.EnableKeyword(instancedKeyword);
            GroundSimulation.Unpublish();
            GameObject plantMesh = _scene.Track(new GameObject("Equivalent plant mesh"));
            plantMesh.layer = LookTestScene.Layer;
            plantMesh.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            plantMesh.AddComponent<MeshFilter>().sharedMesh = _mesh;
            MeshRenderer direct = plantMesh.AddComponent<MeshRenderer>();
            direct.sharedMaterial = body;
            direct.shadowCastingMode = ShadowCastingMode.Off;
            _draw.Release();
            _draw = new GrassDraw(_mesh, grass, 1, new Bounds(Vector3.up * 0.5f, Vector3.one * 2f), LookTestScene.Layer);
            // Select the camera-facing facet after the same nonuniform scale used by the indirect adapter.
            Vector3 normal = _mesh.normals.OrderBy(n => n.z).First();
            normal = new Vector3(normal.x / 0.8f, normal.y, normal.z / 0.8f).normalized;
            Vector3 glintLight = Vector3.Reflect(Vector3.forward, normal);
            Vector3[] directions = { glintLight, Vector3.left, Vector3.forward };
            using (GraphicsBuffer seeds = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftSeed.Stride))
            using (GraphicsBuffer states = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, TuftState.Stride))
            using (GraphicsBuffer visible = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 4))
            {
                seeds.SetData(new[] { new TuftSeed { heightWidthLean = new Vector4(1f, 0.8f, 0f, 0f) } });
                states.SetData(new[] { new TuftState { leanHeightSpike = new Vector4(0f, 0f, 1f, 0f) } });
                visible.SetData(new uint[] { 0 });
                _draw.BindTufts(seeds, states, visible, 1f);
                Color32[][] samples = new Color32[directions.Length][];
                for (int angle = 0; angle < directions.Length; angle++)
                {
                    RenderSettings.sun.transform.rotation = Quaternion.LookRotation(-directions[angle], Vector3.up);
                    direct.enabled = true;
                    _draw.Hide();
                    _scene.Render();
                    Color32[] expected = _scene.texture.GetPixels32();
                    direct.enabled = false;
                    _draw.Show(_scene.camera, () => true);
                    _scene.Render();
                    Color32[] actual = _scene.texture.GetPixels32();
                    samples[angle] = actual;
                    _draw.Hide();
                    int covered = 0;
                    int mismatched = 0;
                    float totalError = 0f;
                    for (int pixel = 0; pixel < expected.Length; pixel++)
                    {
                        Color32 a = expected[pixel];
                        Color32 b = actual[pixel];
                        if (a.r < 250 || a.g < 250 || a.b < 250)
                        {
                            covered++;
                        }
                        int error = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                        totalError += error;
                        if (error > 6)
                        {
                            mismatched++;
                        }
                    }
                    Assert.That(covered, Is.GreaterThan(1000), "The comparison must contain visible geometry");
                    Assert.That(mismatched / (float)covered, Is.LessThan(0.02f),
                        "Instanced grass must match the plant surface, allowing only raster-edge differences");
                    Assert.That(totalError / (3f * covered), Is.LessThan(0.7f), "Mean channel error in 8-bit codes");
                    Debug.Log("[GrassDrawTests] Plant equivalence angle=" + angle + " covered=" + covered
                        + " mismatched=" + mismatched + " meanError=" + (totalError / (3f * covered)).ToString("F4"));
                }
                Color32 glint = samples[0][128 * 256 + 128];
                Color32 shade = samples[2][128 * 256 + 128];
                Assert.That(glint.r - shade.r, Is.GreaterThan(100), "Real light movement must reveal the bright face");
                Assert.That(glint.g - shade.g, Is.GreaterThan(80),
                    "The control cannot pass by comparing two invisible draws");
            }
        }

        [Test]
        public void HealRingMaterial_Asset_IsTheRingShaderWithInstancing()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/HealRing.mat");

            Shader ring = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/GrassRing.shader");
            Assert.AreEqual(ring, material.shader);
            Assert.IsTrue(material.enableInstancing);
        }

        [Test]
        public void Tufts_GrassInstancingKeywords_CompileLookAndRingShaders()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Shader compilation needs a graphics device.");
            }

            foreach (string path in new[] { lookShaderPath, "Assets/Render/Shaders/GrassRing.shader" })
            {
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                Assert.NotNull(shader);
                Material material = _scene.Track(new Material(shader) { enableInstancing = true });
                string[] shadows =
                {
                    "", "_MAIN_LIGHT_SHADOWS", "_MAIN_LIGHT_SHADOWS_CASCADE", "_MAIN_LIGHT_SHADOWS_SCREEN"
                };
                foreach (string shadow in shadows)
                {
                    foreach (bool isOctahedral in new[] { false, true })
                    {
                        string normals = isOctahedral ? "_GBUFFER_NORMALS_OCT" : "";
                        material.shaderKeywords = new[]
                        {
                            "PROCEDURAL_INSTANCING_ON", "HL_GRASS_INSTANCED", shadow, normals, "_SHADOWS_SOFT"
                        };
                        for (int pass = 0; pass < material.passCount; pass++)
                        {
                            ShaderUtil.CompilePass(material, pass, true);
                            Assert.IsTrue(ShaderUtil.IsPassCompiled(material, pass), path + " pass " + pass);
                        }
                    }
                }

                foreach (ShaderMessage message in ShaderUtil.GetShaderMessages(shader))
                {
                    Assert.AreNotEqual(ShaderCompilerMessageSeverity.Error, message.severity, message.message);
                }
            }
        }
    }
}
