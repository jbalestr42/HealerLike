using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{
    // The simulation adopts this owner only after every native resource has been created successfully.
    public class GroundResources : IDisposable
    {
        readonly List<RenderTexture> _targets = new List<RenderTexture>();
        public RenderTexture[] motion { get; } = new RenderTexture[2];
        public RenderTexture[] crush { get; } = new RenderTexture[2];
        public RenderTexture[] state { get; } = new RenderTexture[2];
        public RenderTexture target { get; private set; }
        public RenderTexture force { get; private set; }
        public RenderTexture aura { get; private set; }
        public GraphicsBuffer stamps { get; private set; }
        public CommandBuffer commands { get; private set; }
        public Material material { get; private set; }
        public bool isValid { get; private set; }

        // The texture creation boundary also permits deterministic allocation-failure tests.
        public GroundResources(Shader shader, GroundVolume volume, Func<RenderTexture, bool> create = null)
        {
            try
            {
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                target = CreateTarget(volume, "GroundTarget", RenderTextureFormat.ARGBHalf, create);
                force = CreateTarget(volume, "GroundForce", RenderTextureFormat.ARGBHalf, create);
                aura = CreateTarget(volume, "GroundAura", RenderTextureFormat.ARGBHalf, create);
                for (int i = 0; i < 2; i++)
                {
                    motion[i] = CreateTarget(volume, "GroundMotion" + i, RenderTextureFormat.ARGBHalf, create);
                    crush[i] = CreateTarget(volume, "GroundCrush" + i, RenderTextureFormat.RHalf, create);
                    state[i] = CreateTarget(volume, "GroundState" + i, RenderTextureFormat.ARGBHalf, create);
                }

                stamps = new GraphicsBuffer(GraphicsBuffer.Target.Structured,
                    GroundSimulation.StampCapacity, GroundStamp.Stride);
                commands = new CommandBuffer { name = "GroundSimulation" };
                isValid = true;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        RenderTexture CreateTarget(GroundVolume volume, string name, RenderTextureFormat format,
            Func<RenderTexture, bool> create)
        {
            RenderTexture texture = new RenderTexture(volume.width, volume.height, 0, format,
                RenderTextureReadWrite.Linear)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                hideFlags = HideFlags.HideAndDontSave
            };
            _targets.Add(texture);
            bool created = create != null ? create(texture) : texture.Create();
            if (!created || !texture.IsCreated())
            {
                throw new InvalidOperationException("Could not create " + name + ".");
            }

            return texture;
        }

        public void Dispose()
        {
            isValid = false;
            stamps?.Dispose();
            stamps = null;
            commands?.Release();
            commands = null;
            foreach (RenderTexture texture in _targets)
            {
                if (texture != null)
                {
                    texture.Release();
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            _targets.Clear();
            target = null;
            force = null;
            aura = null;
            Array.Clear(motion, 0, motion.Length);
            Array.Clear(crush, 0, crush.Length);
            Array.Clear(state, 0, state.Length);
            if (material != null)
            {
                UnityEngine.Object.DestroyImmediate(material);
                material = null;
            }
        }
    }
}
