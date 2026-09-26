using HealerLike.Render.Creatures;
using HealerLike.Render.Look;
using HealerLike.Render.Zones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Grass
{
    // The draw alone, then the real field's draws in the look under the stage key light
    public abstract class AGrassDrawFixture
    {
        protected static readonly string meshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
        protected static readonly string lookShaderPath = "Assets/Render/Shaders/Look.shader";
        protected static readonly string grassMaterialPath = "Assets/Render/Grass/Materials/GrassBlade.mat";
        // The keyword GrassBlade.mat carries so the look shader reads the tuft buffers
        protected static readonly string instancedKeyword = "HL_GRASS_INSTANCED";

        protected Mesh _mesh;
        protected Material _material;
        protected GrassDraw _draw;
        protected LookTestScene _scene;
        protected GrassField _patch;
        ZoneRegistry _registry;
        Ground _ground;
        GroundTestGlobals _globals;

        [SetUp]
        public void SetUp()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !SystemInfo.supportsIndirectArgumentsBuffer)
            {
                Assert.Ignore("Indirect argument buffers need a graphics device; run with -force-metal.");
            }

            _globals = new GroundTestGlobals();
            GroundSimulation.Unpublish();
            _scene = new LookTestScene();
            _scene.Init();
            _mesh = AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath).tuft;
            _material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(lookShaderPath));
            _draw = new GrassDraw(_mesh, _material, 7, new Bounds(Vector3.zero, Vector3.one), 3);
        }

        [TearDown]
        public void TearDown()
        {
            if (_draw != null)
            {
                _draw.Release();
            }

            if (_material != null)
            {
                Object.DestroyImmediate(_material);
            }

            if (_patch != null)
            {
                _patch.Release();
            }

            _ground?.Dispose();
            _ground = null;
            _registry?.Release();
            _registry = null;

            if (_scene != null)
            {
                _scene.Release();
            }
            _globals?.Dispose();
            _globals = null;
        }

        protected static Texture2D PublishState(Vector4 state)
        {
            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBAFloat, false, true);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = state;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            Shader.SetGlobalTexture(GroundSimulation.StateId, texture);
            Shader.SetGlobalVector(GroundSimulation.RectId, new Vector4(-10f, -10f, 1f / 20f, 1f / 20f));
            Shader.SetGlobalFloat(GroundSimulation.ActiveId, 1f);
            return texture;
        }

        protected static int ShadowPixels(Texture2D texture)
        {
            int count = 0;
            foreach (Color32 pixel in texture.GetPixels32())
            {
                if (pixel.r < 200 && pixel.g < 200 && pixel.b < 200)
                {
                    count++;
                }
            }
            return count;
        }

        protected GameObject CreateStone(Material material)
        {
            GameObject sphere = _scene.Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            sphere.name = "Key light stone";
            sphere.layer = LookTestScene.Layer;
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            return sphere;
        }

        protected GrassField CreateGrassPatch(Material tuftMaterial)
        {
            _registry = _scene.Track(new GameObject("Key light zones")).AddComponent<ZoneRegistry>();
            _registry.Init();
            GrassField field = _scene.Track(new GameObject("Key light grass")).AddComponent<GrassField>();
            _patch = field;
            field.gameObject.layer = LookTestScene.Layer;
            field.Init(new Rect(-4f, -4f, 8f, 8f), 1f, 0f, _scene.camera, _registry.buffer, ZonePacker.MaxZones);
            field.tuftBudget = 16384;
            ComputeShader compute = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Render/Shaders/Grass.compute");
            Material ring = AssetDatabase.LoadAssetAtPath<Material>("Assets/Render/Grass/Materials/HealRing.mat");
            TestHelpers.SetPrivateField(field, "_meshes", AssetDatabase.LoadAssetAtPath<PrimitiveMeshes>(meshesPath));
            TestHelpers.SetPrivateField(field, "_updateGrass", compute);
            TestHelpers.SetPrivateField(field, "_lookMaterial", tuftMaterial);
            TestHelpers.SetPrivateField(field, "_ringMaterial", ring);
            _registry.PublishFrame(0f);
            _ground = new Ground(AssetDatabase.LoadAssetAtPath<GroundVocabulary>(
                "Assets/Render/Grass/Data/GroundVocabulary.asset"));
            field.UpdateField(_registry, _ground);
            return field;
        }
    }
}
