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
        public static readonly string PlantMaterialPath = "Assets/Render/Look/HLLook_Default.mat";
        public static readonly string StoneMaterialPath = "Assets/Render/Look/HLLook_Stone.mat";
        public static readonly string GroundMaterialPath = "Assets/Render/Environment/HLLook_Ground.mat";
        public static readonly string MeshesPath = "Assets/Render/Creatures/Data/PrimitiveMeshes.asset";
        public static readonly string GrassComputePath = "Assets/Render/Shaders/HLGrass.compute";
        public static readonly string BladeMaterialPath = "Assets/Render/Grass/Materials/GrassBlade.mat";
        public static readonly string RingMaterialPath = "Assets/Render/Grass/Materials/HealRing.mat";

        public static GameObject Create()
        {
            GameObject root = new GameObject("Environment");
            GameObject strip = Child(root, "GrassStrip");
            GrassField stripField = strip.AddComponent<GrassField>();
            SetGrass(stripField);
            strip.SetActive(false);

            EnvironmentGrass grass = root.AddComponent<EnvironmentGrass>();
            SetReference(grass, "_stripTemplate", stripField);
            EnvironmentScatter scatter = root.AddComponent<EnvironmentScatter>();
            SetMaterials(scatter);
            root.AddComponent<EnvironmentGust>();
            SetMaterials(Child(root, "Foreground").AddComponent<EnvironmentForeground>());
            SetMaterials(Child(root, "FarRidge").AddComponent<EnvironmentRidge>());
            CreateGround(root);

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // The grass field settings shared by the board and the strips
        public static void SetGrass(GrassField field)
        {
            SetReference(field, "_meshes", Load<Object>(MeshesPath));
            SetReference(field, "_updateGrass", Load<ComputeShader>(GrassComputePath));
            SetReference(field, "_lookMaterial", Load<Material>(BladeMaterialPath));
            SetReference(field, "_ringMaterial", Load<Material>(RingMaterialPath));
        }

        // The Julien scale ground, a thousand units wide, never intercepting gameplay raycasts
        static void CreateGround(GameObject root)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.SetParent(root.transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            MeshRenderer groundRenderer = ground.GetComponent<MeshRenderer>();
            groundRenderer.sharedMaterial = Load<Material>(GroundMaterialPath);
            groundRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        static void SetMaterials(Component component)
        {
            SetReference(component, "_plantMaterial", Load<Material>(PlantMaterialPath));
            SetReference(component, "_stoneMaterial", Load<Material>(StoneMaterialPath));
        }

        static GameObject Child(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        public static AssetType Load<AssetType>(string path) where AssetType : Object
        {
            AssetType asset = AssetDatabase.LoadAssetAtPath<AssetType>(path);
            if (asset == null)
            {
                Debug.LogError($"[EnvironmentAuthoring] Missing {path}.");
            }
            return asset;
        }

        public static void SetReference(Object target, string field, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[EnvironmentAuthoring] {target.GetType().Name} has no field {field}.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
