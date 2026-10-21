using System.Collections.Generic;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{
    // Grass.compute is the kernel GrassField dispatches; these tests run it directly on Metal
    public abstract class AGrassComputeFixture
    {
        protected ComputeShader _compute;
        protected int _kernel;
        protected readonly List<GraphicsBuffer> _buffers = new List<GraphicsBuffer>();
        protected GraphicsBuffer _seedBuffer;
        protected GraphicsBuffer _stateBuffer;
        protected GraphicsBuffer _zoneBuffer;
        protected GraphicsBuffer _visible;
        protected GraphicsBuffer _counter;
        protected TuftSeed[] _layout;
        protected Zone[] _zones;
        protected TuftState[] _states;
        protected Vector4[] _planes;
        protected Texture2D _groundMotion;
        protected Texture2D _groundCrush;
        protected Texture2D _groundState;

        // 65 upright tufts of the mean size, so a zone's push is the whole lean
        protected static TuftSeed[] CreateSeeds()
        {
            TuftSeed[] seeds = new TuftSeed[65];
            for (int i = 0; i < seeds.Length; i++)
            {
                seeds[i].heightWidthLean = new Vector4(GrassLayout.TuftHeight, GrassLayout.TuftWidth, 0f, 0f);
            }

            return seeds;
        }

        [SetUp]
        public void SetUp()
        {
            if (!SystemInfo.supportsComputeShaders || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Assert.Ignore("Requires a graphics device; run the grass suite with -force-metal.");
            }

            ComputeShader asset = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/Grass.compute");
            Assert.NotNull(asset);
            _compute = Object.Instantiate(asset);
            _kernel = _compute.FindKernel("HLUpdateGrass");

            // Tuft 0 sits under the heal zone, tuft 1 under the hostile one, the rest far away
            _layout = CreateSeeds();
            for (int i = 0; i < _layout.Length; i++)
            {
                float x = 20f;
                if (i == 0)
                {
                    x = -2f;
                }
                else if (i == 1)
                {
                    x = 2f;
                }
                _layout[i].positionYaw = new Vector4(x, 0.505f, 0f, 0f);
            }
            _seedBuffer = CreateBuffer(GraphicsBuffer.Target.Structured, 65, TuftSeed.Stride);
            _seedBuffer.SetData(_layout);

            // One state past the tuft count guards against writes beyond it
            _stateBuffer = CreateBuffer(GraphicsBuffer.Target.Structured, 66, TuftState.Stride);
            _states = new TuftState[66];
            _states[65].leanHeightSpike = Vector4.one * 123f;
            _stateBuffer.SetData(_states);

            _zoneBuffer = CreateBuffer(GraphicsBuffer.Target.Structured, 64, Zone.Stride);
            _zones = new Zone[64];
            _zones[0] = new Zone
            {
                position = new Vector3(-2f, 90f, 0f), radius = 1f, kind = 1, strength = 0.8f, age = 0.06f
            };
            _zones[1] = new Zone
            {
                position = new Vector3(2f, -90f, 0f), radius = 1f, kind = 2, strength = 0.9f, age = 0.09f
            };
            _zoneBuffer.SetData(_zones);

            _visible = CreateBuffer(GraphicsBuffer.Target.Append, 65, 4);
            _counter = CreateBuffer(GraphicsBuffer.Target.Raw, 1, 4);
            _planes = new Vector4[6];
            for (int i = 0; i < 6; i++)
            {
                _planes[i] = new Vector4(0f, 0f, 0f, 100f);
            }

            _compute.SetBuffer(_kernel, "_HLBladeSeeds", _seedBuffer);
            _compute.SetBuffer(_kernel, "_HLBladeStates", _stateBuffer);
            _compute.SetBuffer(_kernel, "_HLZones", _zoneBuffer);
            _compute.SetBuffer(_kernel, "_HLVisibleBlades", _visible);
            _compute.SetInt("_HLBladeCount", 65);
            _compute.SetInt("_HLZoneCount", 2);
            _compute.SetVectorArray("_HLFrustumPlanes", _planes);
            _compute.SetFloat("_HLCullMargin", GrassBounds.Envelope(1f));
            _compute.SetVector("_HLGroundWind", GroundWind.Shader(0f, 0f));
            SetGround(null, 0f);
        }

        // A ground of one lean, flatness and state over the whole test area, or none
        protected void SetGround(Vector4? motion, float crush, Vector4 state = default)
        {
            DestroyGround();
            _groundMotion = new Texture2D(4, 4, TextureFormat.RGBAFloat, false, true);
            _groundCrush = new Texture2D(4, 4, TextureFormat.RFloat, false, true);
            _groundState = new Texture2D(4, 4, TextureFormat.RGBAFloat, false, true);
            Color motionColour = motion.HasValue ? (Color)motion.Value : Color.clear;
            Color[] motionPixels = new Color[16];
            Color[] crushPixels = new Color[16];
            Color[] statePixels = new Color[16];
            for (int i = 0; i < 16; i++)
            {
                motionPixels[i] = motionColour;
                crushPixels[i] = new Color(crush, 0f, 0f, 0f);
                statePixels[i] = state;
            }

            _groundMotion.SetPixels(motionPixels);
            _groundMotion.Apply();
            _groundCrush.SetPixels(crushPixels);
            _groundCrush.Apply();
            _groundState.SetPixels(statePixels);
            _groundState.Apply();
            _compute.SetTexture(_kernel, "_HLGroundMotion", _groundMotion);
            _compute.SetTexture(_kernel, "_HLGroundCrush", _groundCrush);
            _compute.SetTexture(_kernel, "_HLGroundState", _groundState);
            // Twenty units either side of the origin, well past every test tuft
            _compute.SetVector("_HLGroundRect", new Vector4(-40f, -40f, 1f / 80f, 1f / 80f));
            _compute.SetFloat("_HLGroundActive", motion.HasValue ? 1f : 0f);
        }

        protected void DestroyGround()
        {
            if (_groundMotion != null)
            {
                Object.DestroyImmediate(_groundMotion);
                Object.DestroyImmediate(_groundCrush);
                Object.DestroyImmediate(_groundState);
                _groundMotion = null;
                _groundCrush = null;
                _groundState = null;
            }
        }

        [TearDown]
        public void TearDown()
        {
            DestroyGround();
            foreach (GraphicsBuffer buffer in _buffers)
            {
                buffer.Dispose();
            }
            _buffers.Clear();
            if (_compute != null)
            {
                Object.DestroyImmediate(_compute);
            }
        }

        protected GraphicsBuffer CreateBuffer(GraphicsBuffer.Target target, int count, int stride)
        {
            GraphicsBuffer buffer = new GraphicsBuffer(target, count, stride);
            _buffers.Add(buffer);
            return buffer;
        }

        // Runs the kernel and returns how many tuft ids it appended to the visible list
        protected uint Dispatch()
        {
            _visible.SetCounterValue(0);
            _compute.Dispatch(_kernel, 2, 1, 1);
            GraphicsBuffer.CopyCount(_visible, _counter, 0);
            uint[] counts = new uint[1];
            _counter.GetData(counts);
            return counts[0];
        }

        protected void ReadStates()
        {
            _stateBuffer.GetData(_states);
        }

        // Moves tuft 0 to the point under a single zone and returns its state after one dispatch
        protected TuftState Sample(int kind, Vector3 point, float radius, float age, float strength = 1f, uint heading = 0)
        {
            _layout[0].positionYaw = new Vector4(point.x, 0.505f, point.z, 0f);
            _seedBuffer.SetData(_layout);
            _zones[0] = new Zone { radius = radius, kind = kind, strength = strength, age = age, reserved = heading };
            _zoneBuffer.SetData(_zones);
            _compute.SetInt("_HLZoneCount", 1);
            Dispatch();
            ReadStates();
            return _states[0];
        }
    }
}
