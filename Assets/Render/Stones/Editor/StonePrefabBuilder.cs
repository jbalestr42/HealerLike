using System.IO;
using HealerLike.Render.Creatures;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stones
{
    public static class StonePrefabBuilder
    {
        static readonly string root = "Assets/Render/Stones/";

        public static readonly string StoneMaterialPath = "Assets/Render/Look/Look_Stone.mat";

        [MenuItem("Tools/Render/Author Stone Prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(root + "Prefabs");
            BuildEffects();
            BuildDerivedStone();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static AssetType Load<AssetType>(string path) where AssetType : Object
        {
            AssetType asset = AssetDatabase.LoadAssetAtPath<AssetType>(path);
            if (asset == null)
            {
                Debug.LogError($"[StonePrefabBuilder] Missing {path}");
            }
            return asset;
        }

        // The derived stone keeps the CreatureBuilder that draws it and gains the body that sheds, collapses and throws
        public static void BuildDerivedStone()
        {
            string path = root + "Prefabs/DerivedStone.prefab";
            GameObject stoneGo = PrefabUtility.LoadPrefabContents(path);
            if (stoneGo.GetComponent<StoneBody>() == null)
            {
                StoneBody body = stoneGo.AddComponent<StoneBody>();
                SerializedObject bodySO = new SerializedObject(body);
                bodySO.FindProperty("_groundShadow").objectReferenceValue = AddShadow(stoneGo.transform);
                bodySO.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(stoneGo, path);
            PrefabUtility.UnloadPrefabContents(stoneGo);
        }

        // A cast shadow, flat on the baked unit disc, hidden until its owner's Init
        static StoneGroundDisc AddShadow(Transform parent)
        {
            GameObject discGo = new GameObject("GroundShadow", typeof(MeshFilter), typeof(MeshRenderer));
            discGo.layer = parent.gameObject.layer;
            discGo.transform.SetParent(parent, false);
            discGo.GetComponent<MeshFilter>().sharedMesh = Load<PrimitiveMeshes>(
                "Assets/Render/Creatures/Data/PrimitiveMeshes.asset").disc;
            MeshRenderer renderer = discGo.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Load<Material>(root + "Materials/GroundShadow.mat");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            StoneGroundDisc disc = discGo.AddComponent<StoneGroundDisc>();
            SerializedObject discSO = new SerializedObject(disc);
            discSO.FindProperty("_renderer").objectReferenceValue = renderer;
            discSO.FindProperty("_isShadow").boolValue = true;
            discSO.ApplyModifiedPropertiesWithoutUndo();
            discGo.SetActive(false);
            return disc;
        }

        // The one effects owner and the fragment it pools for debris, dust and thrown shards
        static void BuildEffects()
        {
            GameObject fragmentGo = new GameObject("StoneFragment", typeof(MeshFilter), typeof(MeshRenderer));
            GameObject fragment = PrefabUtility.SaveAsPrefabAsset(fragmentGo, root + "Prefabs/StoneFragment.prefab");
            Object.DestroyImmediate(fragmentGo);

            GameObject effectsGo = new GameObject("StoneEffects");
            StoneEffects effects = effectsGo.AddComponent<StoneEffects>();
            SerializedObject effectsSO = new SerializedObject(effects);
            effectsSO.FindProperty("_stoneMaterial").objectReferenceValue = Load<Material>(StoneMaterialPath);
            effectsSO.FindProperty("_coralMaterial").objectReferenceValue = Load<Material>(root + "Materials/CoralSpark.mat");
            effectsSO.FindProperty("_dustMaterial").objectReferenceValue = Load<Material>(root + "Materials/Dust.mat");
            effectsSO.FindProperty("_meshes").objectReferenceValue = Load<PrimitiveMeshes>(
                "Assets/Render/Creatures/Data/PrimitiveMeshes.asset");
            effectsSO.FindProperty("_fragmentPrefab").objectReferenceValue = fragment;
            effectsSO.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(effectsGo, root + "Prefabs/StoneEffects.prefab");
            Object.DestroyImmediate(effectsGo);
        }
    }
}
