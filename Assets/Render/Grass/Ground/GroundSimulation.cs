using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{
    // The ground under the grass over one volume. Its motion: each frame the stamps add up into a held target and a
    // kicked force, then fixed steps spring the lean toward the target under the force and ease the flatness. Its
    // state: the auras ask for ash, vitality and glow, which ease toward them once a frame. Global textures let
    // every field sample the same ground, so the board and the strips around it move as one carpet.
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
        GroundResources _resources;
        int _currentState;
        int _current;
        int _stampCount;

        GroundVolume _volume;
        public GroundVolume volume { get { return _volume; } }

        public GroundSpringSettings settings;
        public GroundStateSettings stateSettings = GroundStateSettings.Default;

        public RenderTexture motion { get { return isValid ? _resources.motion[_current] : null; } }
        public RenderTexture crush { get { return isValid ? _resources.crush[_current] : null; } }
        // x ash, y vitality from dead at -1 to lush at 1, z light from frost at -1 to glow at 1, w blight
        public RenderTexture state { get { return isValid ? _resources.state[_currentState] : null; } }
        public int stampCount { get { return _stampCount; } }

        public bool isValid { get { return _resources != null && _resources.isValid; } }

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
            try
            {
                _resources = new GroundResources(shader, volume);
                _resources.material.SetVector(RectId, volume.ShaderRect());
                _resources.material.SetVector(sizeId,
                    new Vector4(volume.width, volume.height, 1f / volume.width, 1f / volume.height));
                Reset();
            }
            catch (Exception exception)
            {
                Dispose();
                Debug.LogError("[GroundSimulation] Could not allocate ground: " + exception.Message);
            }
        }

        // Every texel upright, still and standing
        public void Reset()
        {
            if (!isValid)
            {
                return;
            }

            _resources.commands.Clear();
            for (int i = 0; i < 2; i++)
            {
                _resources.commands.SetRenderTarget(_resources.motion[i]);
                _resources.commands.ClearRenderTarget(false, true, Color.clear);
                _resources.commands.SetRenderTarget(_resources.crush[i]);
                _resources.commands.ClearRenderTarget(false, true, Color.clear);
                _resources.commands.SetRenderTarget(_resources.state[i]);
                _resources.commands.ClearRenderTarget(false, true, Color.clear);
            }

            Graphics.ExecuteCommandBuffer(_resources.commands);
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
            _resources.commands.Clear();
            _resources.commands.SetRenderTarget(_resources.target);
            _resources.commands.ClearRenderTarget(false, true, Color.clear);
            if (_stampCount > 0)
            {
                _resources.stamps.SetData(_stamps, 0, 0, _stampCount);
                _resources.material.SetBuffer(stampsId, _resources.stamps);
                _resources.commands.DrawProcedural(Matrix4x4.identity, _resources.material, stampPass,
                    MeshTopology.Triangles, 6, _stampCount);
            }

            _resources.commands.SetRenderTarget(_resources.force);
            _resources.commands.ClearRenderTarget(false, true, Color.clear);
            if (_stampCount > 0)
            {
                _resources.commands.DrawProcedural(Matrix4x4.identity, _resources.material, forcePass,
                    MeshTopology.Triangles, 6, _stampCount);
            }

            _resources.commands.SetRenderTarget(_resources.aura);
            _resources.commands.ClearRenderTarget(false, true, Color.clear);
            if (_stampCount > 0)
            {
                _resources.commands.DrawProcedural(Matrix4x4.identity, _resources.material, auraPass,
                    MeshTopology.Triangles, 6, _stampCount);
            }

            Graphics.ExecuteCommandBuffer(_resources.commands);
            _resources.material.SetTexture(targetId, _resources.target);
            _resources.material.SetTexture(forceId, _resources.force);
            Vector2 texel = _volume.texelSize;
            _resources.material.SetVector(springId, settings.ShaderSpring(Mathf.Min(texel.x, texel.y)));
            _resources.material.SetVector(crushRatesId, new Vector4(settings.crushFall, settings.crushRise, 0f, 0f));
            for (int i = 0; i < steps; i++)
            {
                // The wind moves on within the frame, each step at its own time
                Vector4 stepWind = wind;
                stepWind.w -= (steps - 1 - i) * step;
                _resources.material.SetVector(windId, stepWind);
                _resources.material.SetFloat(stepId, step);
                _resources.material.SetTexture(previousId, _resources.motion[_current]);
                _resources.material.SetTexture(previousCrushId, _resources.crush[_current]);
                int next = 1 - _current;
                _resources.commands.Clear();
                _resources.commands.SetRenderTarget(_resources.motion[next]);
                _resources.commands.DrawProcedural(Matrix4x4.identity, _resources.material, leanPass,
                    MeshTopology.Triangles, 3, 1);
                _resources.commands.SetRenderTarget(_resources.crush[next]);
                _resources.commands.DrawProcedural(Matrix4x4.identity, _resources.material, crushPass,
                    MeshTopology.Triangles, 3, 1);
                Graphics.ExecuteCommandBuffer(_resources.commands);
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
            _resources.material.SetTexture(auraId, _resources.aura);
            _resources.material.SetTexture(previousStateId, _resources.state[_currentState]);
            _resources.material.SetVector(stateRatesId, stateSettings.ShaderRates());
            _resources.material.SetVector(glowRatesId, stateSettings.ShaderLightRates());
            _resources.material.SetFloat(stepId, frame);
            int next = 1 - _currentState;
            _resources.commands.Clear();
            _resources.commands.SetRenderTarget(_resources.state[next]);
            _resources.commands.DrawProcedural(Matrix4x4.identity, _resources.material, statePass,
                MeshTopology.Triangles, 3, 1);
            Graphics.ExecuteCommandBuffer(_resources.commands);
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
            // A preview or another field may have published after us; release only our publication.
            Texture published = Shader.GetGlobalTexture(MotionId);
            if (isValid && (published == _resources.motion[0] || published == _resources.motion[1]))
            {
                Unpublish();
            }

            _resources?.Dispose();
            _resources = null;
            _stampCount = 0;
        }
    }
}
