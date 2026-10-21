using System.Collections.Generic;
using System.Reflection;
using HealerLike.Render.Creatures;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{
    public abstract class AGrassFieldFixture
    {
        protected static readonly Rect oneCell = new Rect(-0.5f, -0.5f, 1f, 1f);
        // The keyword GrassBlade.mat carries so the look shader reads the tuft buffers
        protected static readonly string instancedKeyword = "HL_GRASS_INSTANCED";
        protected static readonly string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";

        protected GameObject _go;
        protected GrassField _field;
        protected GraphicsBuffer _borrowedZones;
        GroundTestGlobals _globals;

        protected static bool HasGraphicsDevice()
        {
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && SystemInfo.supportsComputeShaders
                   && SystemInfo.supportsIndirectArgumentsBuffer;
        }

        [SetUp]
        public void SetUp()
        {
            _globals = new GroundTestGlobals();
            _go = new GameObject("GrassFieldTest");
            _field = _go.AddComponent<GrassField>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_field != null)
            {
                _field.Release();
            }

            ZoneRegistry registry = _go != null ? _go.GetComponent<ZoneRegistry>() : null;
            registry?.Release();
            _ground?.Dispose();
            _ground = null;
            Object.DestroyImmediate(_go);
            if (_borrowedZones != null)
            {
                _borrowedZones.Dispose();
                _borrowedZones = null;
            }
            _globals?.Dispose();
            _globals = null;
        }

        protected void SetAssets()
        {
            ComputeShader compute = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/Grass.compute");
            Material look = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/GrassBlade.mat");
            Material ring = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/HealRing.mat");
            TestHelpers.SetPrivateField(_field, "_meshes", AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath));
            TestHelpers.SetPrivateField(_field, "_updateGrass", compute);
            TestHelpers.SetPrivateField(_field, "_lookMaterial", look);
            TestHelpers.SetPrivateField(_field, "_ringMaterial", ring);
        }

        // Built through the update a frame runs, with a camera to cull against
        protected void BuildOneCellField()
        {
            if (_borrowedZones == null)
            {
                _borrowedZones = new GraphicsBuffer(GraphicsBuffer.Target.Structured, ZonePacker.MaxZones, Zone.Stride);
            }

            Camera camera = _go.GetComponent<Camera>();
            if (camera == null)
            {
                camera = _go.AddComponent<Camera>();
            }

            _field.Init(oneCell, 1f, 0.5f, camera, _borrowedZones, ZonePacker.MaxZones);
            _field.tuftBudget = 65;
            SetAssets();
            _field.UpdateField(_borrowedZones, 0);
            Assert.IsTrue(_field.isReady);
        }

        // Every buffer the field owns, including the zone rings and compute state.
        protected List<GraphicsBuffer> OwnedBuffers()
        {
            List<GraphicsBuffer> buffers = new List<GraphicsBuffer>();
            buffers.Add(_field.tuftDraw.arguments);
            buffers.Add(_field.socleDraw.arguments);
            GrassTuftBatch batch = Batch();
            buffers.Add(batch.rings.arguments);
            buffers.Add(batch.seeds);
            buffers.Add(batch.states);
            buffers.Add(batch.visible);
            foreach (GraphicsBuffer buffer in buffers)
            {
                Assert.IsTrue(buffer.IsValid());
            }
            return buffers;
        }

        protected GrassTuftBatch Batch()
        {
            return (GrassTuftBatch)typeof(GrassField).GetField("_tufts",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_field);
        }

        // A field on the one-cell area stepped through a real zone registry and a ground holding an obstacle at its
        // centre, owning the ground simulation when asked
        protected Ground _ground;

        protected ZoneRegistry BuildWithRegistry(bool ownsGround)
        {
            Camera camera = _go.AddComponent<Camera>();
            ZoneRegistry registry = _go.AddComponent<ZoneRegistry>();
            registry.Init();
            _field.Init(oneCell, 1f, 0.5f, camera, registry.buffer, ZonePacker.MaxZones);
            _field.tuftBudget = 65;
            SetAssets();
            if (ownsGround)
            {
                TestHelpers.SetPrivateField(_field, "_groundShader",
                    AssetDatabase.LoadAssetAtPath<Shader>("Assets/Render/Shaders/GroundSimulation.shader"));
            }

            _ground = new Ground();
            _ground.Hold(_ground.vocabulary.obstacle).Show(new Vector3(0f, 0.5f, 0f), 0.6f, 1f);
            _ground.Advance(0.2f);
            registry.PublishFrame(0.2f);
            _field.UpdateField(registry, _ground);
            return registry;
        }
    }
}
