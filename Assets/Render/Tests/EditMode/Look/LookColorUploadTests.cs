using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Look
{
    public class LookColorUploadTests
    {
        [Test]
        public void WorkingSpacePropertyVectorsReachPrimitiveShaderUnchanged()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Requires graphics readback");
            }

            Material material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/Look.shader"));
            Mesh mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-0.8f, -0.8f, 0f),
                    new Vector3(-0.8f, 0.8f, 0f),
                    new Vector3(0.8f, 0.8f, 0f),
                    new Vector3(0.8f, -0.8f, 0f)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back }
            };
            RenderTexture target = new RenderTexture(16, 16, 0, RenderTextureFormat.ARGBFloat,
                                                     RenderTextureReadWrite.Linear);
            target.Create();
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBAFloat, false, true);
            string[] names =
            {
                "_HLLookApplied", "_HLToonThreshold", "_HLFogStart", "_HLFogEnd", "_HLFogBands", "_HLInkStrength"
            };
            float[] saved = new float[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                saved[i] = Shader.GetGlobalFloat(names[i]);
            }

            RenderTexture previous = RenderTexture.active;
            try
            {
                Shader.SetGlobalFloat(names[0], 1f);
                Shader.SetGlobalFloat(names[1], 0f);
                Shader.SetGlobalFloat(names[2], 10000f);
                Shader.SetGlobalFloat(names[3], 20000f);
                Shader.SetGlobalFloat(names[4], 6f);
                Shader.SetGlobalFloat(names[5], 0f);
                Color artist = new Color(0.4f, 0.6f, 0.8f, 1f);
                Color linear = artist.linear;

                Color Read(int mode)
                {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    if (mode == 0)
                    {
                        block.SetColor("_BaseColor", artist);
                    }
                    else if (mode == 1)
                    {
                        block.SetColor("_BaseColor", linear);
                    }
                    else
                    {
                        block.SetVector("_BaseColor", linear);
                    }

                    Debug.Log("COLOR mode=" + mode + " GetColor=" + block.GetColor("_BaseColor").ToString("F5")
                              + " GetVector=" + block.GetVector("_BaseColor").ToString("F5"));
                    using (CommandBuffer command = new CommandBuffer())
                    {
                        command.SetRenderTarget(target);
                        command.ClearRenderTarget(false, true, Color.magenta);
                        command.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                        command.DrawMesh(mesh, Matrix4x4.identity, material, 0, material.FindPass("HLForward"), block);
                        Graphics.ExecuteCommandBuffer(command);
                    }

                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0f, 0f, 16f, 16f), 0, 0);
                    texture.Apply();
                    Color color = texture.GetPixel(8, 8);
                    Debug.Log("COLOR pixel=" + color.ToString("F5"));
                    return color;
                }

                Color a = Read(0);
                Color b = Read(1);
                Color c = Read(2);

                Assert.That(((Vector4)a - (Vector4)c).magnitude, Is.LessThan(0.004f),
                            "Artist SetColor and working SetVector must agree");
                Assert.That(((Vector4)b - (Vector4)c).magnitude, Is.GreaterThan(0.2f),
                            "Double conversion must be detected by GPU readback");
                Assert.That(((Vector4)c - (Vector4)linear).magnitude, Is.LessThan(0.004f),
                            "Explicit working vector should reach GPU unchanged");
                Debug.Log("COLOR active=" + QualitySettings.activeColorSpace + " artist=" + artist.ToString("F5")
                          + " linear=" + linear.ToString("F5") + " A=" + a.ToString("F5") + " B=" + b.ToString("F5")
                          + " C=" + c.ToString("F5"));
            }
            finally
            {
                for (int i = 0; i < names.Length; i++)
                {
                    Shader.SetGlobalFloat(names[i], saved[i]);
                }

                RenderTexture.active = previous;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(material);
            }
        }
    }
}
