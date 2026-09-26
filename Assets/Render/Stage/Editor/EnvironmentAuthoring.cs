using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Environment;
using HealerLike.Render.Grass;

namespace HealerLike.Render.Stage
{
    // Environment.prefab: the ground plane, the grass strip template, the scatter, the foreground and the ridge
    public static class EnvironmentAuthoring
    {
        public static readonly string PrefabPath = "Assets/Render/Environment/Prefabs/Environment.prefab";
        public static readonly string PlantMaterialPath = "Assets/Render/Look/Look_Default.mat";
        public static readonly string StoneMaterialPath = "Assets/Render/Look/Look_Stone.mat";
        public static readonly string GroundMaterialPath = "Assets/Render/Environment/Look_Ground.mat";
        public static readonly string MeshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
        public static readonly string PalettePath = "Assets/Render/Grammar/Data/LookPalette.asset";
        public static readonly string GrassComputePath = "Assets/Render/Shaders/Grass.compute";
        public static readonly string BladeMaterialPath = "Assets/Render/Grass/Materials/GrassBlade.mat";
        public static readonly string RingMaterialPath = "Assets/Render/Grass/Materials/HealRing.mat";
        public static readonly string GroundShaderPath = "Assets/Render/Shaders/GroundMotion.shader";

        public static GameObject Create()
        {
            GameObject root = new GameObject("Environment");
            GameObject strip = Child(root, "GrassStrip");
            GrassField stripField = strip.AddComponent<GrassField>();
            SetGrass(stripField);
            strip.SetActive(false);

            EnvironmentGrass grass = root.AddComponent<EnvironmentGrass>();
            RenderAssets.SetReference(grass, "_stripTemplate", stripField);
            EnvironmentScatter scatter = root.AddComponent<EnvironmentScatter>();
            SetMaterials(scatter);
            RenderAssets.SetReference(scatter, "_palette", RenderAssets.Load<Object>(PalettePath));
            EnvironmentGust gust = root.AddComponent<EnvironmentGust>();
            EnvironmentForeground foreground = Child(root, "Foreground").AddComponent<EnvironmentForeground>();
            SetMaterials(foreground);
            EnvironmentRidge ridge = Child(root, "FarRidge").AddComponent<EnvironmentRidge>();
            SetMaterials(ridge);
            GameObject ground = CreateGround(root);

            EnvironmentRoot environment = root.AddComponent<EnvironmentRoot>();
            RenderAssets.SetReference(environment, "_scatter", scatter);
            RenderAssets.SetReference(environment, "_foreground", foreground);
            RenderAssets.SetReference(environment, "_ridge", ridge);
            RenderAssets.SetReference(environment, "_grass", grass);
            RenderAssets.SetReference(environment, "_gust", gust);
            RenderAssets.SetReference(environment, "_ground", ground.transform);

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // The grass field settings shared by the board and the strips
        public static void SetGrass(GrassField field)
        {
            RenderAssets.SetReference(field, "_meshes", RenderAssets.Load<Object>(MeshesPath));
            RenderAssets.SetReference(field, "_updateGrass", RenderAssets.Load<ComputeShader>(GrassComputePath));
            RenderAssets.SetReference(field, "_lookMaterial", RenderAssets.Load<Material>(BladeMaterialPath));
            RenderAssets.SetReference(field, "_ringMaterial", RenderAssets.Load<Material>(RingMaterialPath));
        }

        // The field that owns the ground motion the others sample: the board's
        public static void SetGround(GrassField field)
        {
            RenderAssets.SetReference(field, "_groundShader", RenderAssets.Load<Shader>(GroundShaderPath));
        }

        // The game-scale ground, a thousand units wide, never intercepting gameplay raycasts
        static GameObject CreateGround(GameObject root)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.SetParent(root.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            MeshRenderer groundRenderer = ground.GetComponent<MeshRenderer>();
            groundRenderer.sharedMaterial = RenderAssets.Load<Material>(GroundMaterialPath);
            groundRenderer.shadowCastingMode = ShadowCastingMode.Off;
            return ground;
        }

        static void SetMaterials(Component component)
        {
            RenderAssets.SetReference(component, "_plantMaterial", RenderAssets.Load<Material>(PlantMaterialPath));
            RenderAssets.SetReference(component, "_stoneMaterial", RenderAssets.Load<Material>(StoneMaterialPath));
        }

        static GameObject Child(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }
    }
}
