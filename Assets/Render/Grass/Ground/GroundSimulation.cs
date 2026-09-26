using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{
    // The ground under the grass over one volume. Its motion: each frame the stamps add up into a held target and a
    // kicked force, then fixed steps spring the lean toward the target under the force and ease the flatness. Its
    // state: the auras ask for ash, vitality and glow, which ease toward them once a frame. The result is published as global textures that every
    // grass field samples at its tufts' roots, so the board and the strips around it move as one carpet.
    public class GroundSimulation : IDisposable
    {
        // The most stamps one frame draws; later ones are dropped
        public static readonly int StampCapacity = 1024;

        public static readonly int MotionId = Shader.PropertyToID("_HLGroundMotion");
        public static readonly int CrushId = Shader.PropertyToID("_HLGroundCrush");
        public static readonly int StateId = Shader.PropertyToID("_HLGroundState");
        public static readonly int RectId = Shader.PropertyToID("_HLGroundRect");
        public static readonly int ActiveId = Shader.PropertyToID("_HLGroundActive");

        static readonly int stampsId = Shader.PropertyToID("_HLGroundStamps");
        static readonly int sizeId = Shader.PropertyToID("_HLGroundSize");
        static readonly int previousId = Shader.PropertyToID("_HLGroundPrevious");
        static readonly int previousCrushId = Shader.PropertyToID("_HLGroundPreviousCrush");
        static readonly int targetId = Shader.PropertyToID("_HLGroundTarget");
        static readonly int forceId = Shader.PropertyToID("_HLGroundForce");
        static readonly int auraId = Shader.PropertyToID("_HLGroundAura");
        static readonly int previousStateId = Shader.PropertyToID("_HLGroundPreviousState");
        static readonly int stateRatesId = Shader.PropertyToID("_HLGroundStateRates");
        static readonly int glowRatesId = Shader.PropertyToID("_HLGroundGlowRates");
        static readonly int springId = Shader.PropertyToID("_HLGroundSpring");
        static readonly int crushRatesId = Shader.PropertyToID("_HLGroundCrushRates");
        static readonly int windId = Shader.PropertyToID("_HLGroundWind");
        static readonly int stepId = Shader.PropertyToID("_HLGroundStep");
        static readonly int stampPass = 0;
        static readonly int leanPass = 1;
        static readonly int crushPass = 2;
        static readonly int forcePass = 3;
        static readonly int auraPass = 4;
        static readonly int statePass = 5;

        readonly GroundStamp[] _stamps = new GroundStamp[StampCapacity];
        readonly RenderTexture[] _motion = new RenderTexture[2];
        readonly RenderTexture[] _crush = new RenderTexture[2];
        readonly RenderTexture[] _state = new RenderTexture[2];
        RenderTexture _target;
        RenderTexture _force;
        RenderTexture _aura;
        int _currentState;
        GraphicsBuffer _stampBuffer;
        CommandBuffer _commands;
        Material _material;
        int _current;
        int _stampCount;

        GroundVolume _volume;
        public GroundVolume volume { get { return _volume; } }

        public GroundSpringSettings settings;
        public GroundStateSettings stateSettings = GroundStateSettings.Default;

        public RenderTexture motion { get { return _motion[_current]; } }
        public RenderTexture crush { get { return _crush[_current]; } }
        // x ash, y vitality from dead at -1 to lush at 1, z light from frost at -1 to glow at 1, w blight
        public RenderTexture state { get { return _state[_currentState]; } }
        public int stampCount { get { return _stampCount; } }

        public bool isValid { get { return _material != null && _target != null; } }

        public static bool IsSupported()
        {
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null
                   && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)
                   && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf);
        }

        // Logs and stays invalid when the device, the shader or the volume cannot run the ground
        public GroundSimulation(Shader shader, GroundVolume volume, GroundSpringSettings settings)
        {
            this.settings = settings;
            if (shader == null || !shader.isSupported || !volume.isValid || !IsSupported())
            {
                Debug.LogError("[GroundSimulation] Needs a supported ground shader, half float targets and a valid volume.");
                return;
            }

            _volume = volume;
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _target = CreateTarget("GroundTarget", RenderTextureFormat.ARGBHalf);
            _force = CreateTarget("GroundForce", RenderTextureFormat.ARGBHalf);
            _aura = CreateTarget("GroundAura", RenderTextureFormat.ARGBHalf);
            for (int i = 0; i < 2; i++)
            {
                _motion[i] = CreateTarget("GroundMotion" + i, RenderTextureFormat.ARGBHalf);
                _crush[i] = CreateTarget("GroundCrush" + i, RenderTextureFormat.RHalf);
                _state[i] = CreateTarget("GroundState" + i, RenderTextureFormat.ARGBHalf);
            }

            _stampBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, StampCapacity, GroundStamp.Stride);
            _commands = new CommandBuffer { name = "GroundSimulation" };
            _material.SetVector(RectId, volume.ShaderRect());
            _material.SetVector(sizeId, new Vector4(volume.width, volume.height, 1f / volume.width, 1f / volume.height));
            Reset();
        }

        RenderTexture CreateTarget(string name, RenderTextureFormat format)
        {
            RenderTexture texture = new RenderTexture(_volume.width, _volume.height, 0, format, RenderTextureReadWrite.Linear)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.Create();
            return texture;
        }

        // Every texel upright, still and standing
        public void Reset()
        {
            if (!isValid)
            {
                return;
            }

            _commands.Clear();
            for (int i = 0; i < 2; i++)
            {
                _commands.SetRenderTarget(_motion[i]);
                _commands.ClearRenderTarget(false, true, Color.clear);
                _commands.SetRenderTarget(_crush[i]);
                _commands.ClearRenderTarget(false, true, Color.clear);
                _commands.SetRenderTarget(_state[i]);
                _commands.ClearRenderTarget(false, true, Color.clear);
            }

            Graphics.ExecuteCommandBuffer(_commands);
            _stampCount = 0;
        }

        // The stamps of this frame; later ones past the capacity are dropped
        public void SetStamps(ReadOnlySpan<GroundStamp> stamps)
        {
            _stampCount = Mathf.Min(stamps.Length, StampCapacity);
            stamps.Slice(0, _stampCount).CopyTo(_stamps);
        }

        // Adds the frame's stamps, then advances the lean and flatness by deltaTime in equal steps. wind is
        // GroundWind.Shader at the frame's time.
        public void Step(float deltaTime, Vector4 wind)
        {
            if (!isValid)
            {
                return;
            }

            int steps = GroundSpring.StepCount(deltaTime, out float step);
            _commands.Clear();
            _commands.SetRenderTarget(_target);
            _commands.ClearRenderTarget(false, true, Color.clear);
            if (_stampCount > 0)
            {
                _stampBuffer.SetData(_stamps, 0, 0, _stampCount);
                _material.SetBuffer(stampsId, _stampBuffer);
                _commands.DrawProcedural(Matrix4x4.identity, _material, stampPass, MeshTopology.Triangles, 6,
                                         _stampCount);
            }

            _commands.SetRenderTarget(_force);
            _commands.ClearRenderTarget(false, true, Color.clear);
            if (_stampCount > 0)
            {
                _commands.DrawProcedural(Matrix4x4.identity, _material, forcePass, MeshTopology.Triangles, 6,
                                         _stampCount);
            }

            _commands.SetRenderTarget(_aura);
            _commands.ClearRenderTarget(false, true, Color.clear);
            if (_stampCount > 0)
            {
                _commands.DrawProcedural(Matrix4x4.identity, _material, auraPass, MeshTopology.Triangles, 6,
                                         _stampCount);
            }

            Graphics.ExecuteCommandBuffer(_commands);
            _material.SetTexture(targetId, _target);
            _material.SetTexture(forceId, _force);
            Vector2 texel = _volume.texelSize;
            _material.SetVector(springId, settings.ShaderSpring(Mathf.Min(texel.x, texel.y)));
            _material.SetVector(crushRatesId, new Vector4(settings.crushFall, settings.crushRise, 0f, 0f));
            for (int i = 0; i < steps; i++)
            {
                // The wind moves on within the frame, each step at its own time
                Vector4 stepWind = wind;
                stepWind.w -= (steps - 1 - i) * step;
                _material.SetVector(windId, stepWind);
                _material.SetFloat(stepId, step);
                _material.SetTexture(previousId, _motion[_current]);
                _material.SetTexture(previousCrushId, _crush[_current]);
                int next = 1 - _current;
                _commands.Clear();
                _commands.SetRenderTarget(_motion[next]);
                _commands.DrawProcedural(Matrix4x4.identity, _material, leanPass, MeshTopology.Triangles, 3, 1);
                _commands.SetRenderTarget(_crush[next]);
                _commands.DrawProcedural(Matrix4x4.identity, _material, crushPass, MeshTopology.Triangles, 3, 1);
                Graphics.ExecuteCommandBuffer(_commands);
                _current = next;
            }

            if (steps > 0)
            {
                StepState(step * steps);
            }
        }

        // The slow state moves once a frame; its exponential ease takes any frame length
        void StepState(float frame)
        {
            _material.SetTexture(auraId, _aura);
            _material.SetTexture(previousStateId, _state[_currentState]);
            _material.SetVector(stateRatesId, stateSettings.ShaderRates());
            _material.SetVector(glowRatesId, stateSettings.ShaderLightRates());
            _material.SetFloat(stepId, frame);
            int next = 1 - _currentState;
            _commands.Clear();
            _commands.SetRenderTarget(_state[next]);
            _commands.DrawProcedural(Matrix4x4.identity, _material, statePass, MeshTopology.Triangles, 3, 1);
            Graphics.ExecuteCommandBuffer(_commands);
            _currentState = next;
        }

        // What every grass field samples until Unpublish
        public void Publish()
        {
            if (!isValid)
            {
                return;
            }

            Shader.SetGlobalTexture(MotionId, motion);
            Shader.SetGlobalTexture(CrushId, crush);
            Shader.SetGlobalTexture(StateId, state);
            Shader.SetGlobalVector(RectId, _volume.ShaderRect());
            Shader.SetGlobalFloat(ActiveId, 1f);
        }

        public static void Unpublish()
        {
            Shader.SetGlobalTexture(MotionId, Texture2D.blackTexture);
            Shader.SetGlobalTexture(CrushId, Texture2D.blackTexture);
            Shader.SetGlobalTexture(StateId, Texture2D.blackTexture);
            Shader.SetGlobalFloat(ActiveId, 0f);
        }

        public void Dispose()
        {
            _stampBuffer?.Dispose();
            _stampBuffer = null;
            _commands?.Release();
            _commands = null;
            ReleaseTarget(ref _target);
            ReleaseTarget(ref _force);
            ReleaseTarget(ref _aura);
            for (int i = 0; i < 2; i++)
            {
                ReleaseTarget(ref _motion[i]);
                ReleaseTarget(ref _crush[i]);
                ReleaseTarget(ref _state[i]);
            }

            if (_material != null)
            {
                UnityEngine.Object.DestroyImmediate(_material);
                _material = null;
            }
        }

        static void ReleaseTarget(ref RenderTexture texture)
        {
            if (texture != null)
            {
                texture.Release();
                UnityEngine.Object.DestroyImmediate(texture);
                texture = null;
            }
        }
    }
}
