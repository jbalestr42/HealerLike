using System.IO;
using HealerLike.Render.Creatures;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HealerLike.Render.Zones;

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
            StoneModelAuthoring.Build(root + "Prefabs/StoneSoldierModel.prefab", "StoneSoldierModel", StonePreset.Boulder);
            BuildDerivedStone();
            BuildBlock();
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
                bodySO.FindProperty("_groundShadow").objectReferenceValue = AddDisc(stoneGo.transform, true);
                bodySO.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(stoneGo, path);
            PrefabUtility.UnloadPrefabContents(stoneGo);
        }

        static void BuildBlock()
        {
            GameObject sourceBlock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/Block.prefab");
            GameObject blockGo = (GameObject)PrefabUtility.InstantiatePrefab(sourceBlock);
            blockGo.name = "StoneBlock";
            foreach (MeshRenderer meshRenderer in blockGo.GetComponentsInChildren<MeshRenderer>())
            {
                Object.DestroyImmediate(meshRenderer);
            }
            foreach (MeshFilter meshFilter in blockGo.GetComponentsInChildren<MeshFilter>())
            {
                Object.DestroyImmediate(meshFilter);
            }

            GameObject child = new GameObject("StoneClump");
            child.layer = blockGo.layer;
            child.transform.SetParent(blockGo.transform, false);
            StoneTerrainClump clump = child.AddComponent<StoneTerrainClump>();
            SerializedObject clumpSO = new SerializedObject(clump);
            clumpSO.FindProperty("_stoneMaterial").objectReferenceValue = Load<Material>(StoneMaterialPath);
            clumpSO.ApplyModifiedPropertiesWithoutUndo();
            AddClumpChildren(clump);
            // The grass flattens around the bare disc, the clump places it at Init
            GameObject trampleGo = new GameObject("Trample");
            trampleGo.transform.SetParent(child.transform, false);
            clumpSO.Update();
            clumpSO.FindProperty("_trample").objectReferenceValue = trampleGo.AddComponent<TrampleZone>();
            clumpSO.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(blockGo, root + "Prefabs/StoneBlock.prefab");
            Object.DestroyImmediate(blockGo);
        }

        static void AddClumpChildren(StoneTerrainClump clump)
        {
            GameObject face = new GameObject("OchreFace", typeof(MeshFilter), typeof(MeshRenderer));
            face.layer = clump.gameObject.layer;
            face.transform.SetParent(clump.transform, false);
            face.GetComponent<MeshRenderer>().sharedMaterial = Load<Material>(StoneMaterialPath);
            face.SetActive(false);

            SerializedObject clumpSO = new SerializedObject(clump);
            clumpSO.FindProperty("_groundDisc").objectReferenceValue = AddDisc(clump.transform, false);
            clumpSO.FindProperty("_groundShadow").objectReferenceValue = AddDisc(clump.transform, true);
            clumpSO.FindProperty("_ochreFace").objectReferenceValue = face.GetComponent<MeshFilter>();
            clumpSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // Bare earth or cast shadow, flat on the baked unit disc, hidden until its owner's Init
        public static StoneGroundDisc AddDisc(Transform parent, bool isShadow)
        {
            GameObject discGo = new GameObject(isShadow ? "GroundShadow" : "GroundDisc", typeof(MeshFilter),
                typeof(MeshRenderer));
            discGo.layer = parent.gameObject.layer;
            discGo.transform.SetParent(parent, false);
            discGo.GetComponent<MeshFilter>().sharedMesh = Load<PrimitiveMeshes>(
                "Assets/Render/Creatures/Data/PrimitiveMeshes.asset").disc;
            MeshRenderer renderer = discGo.GetComponent<MeshRenderer>();
            string material = isShadow ? "GroundShadow" : "GroundDisc";
            renderer.sharedMaterial = Load<Material>(root + "Materials/" + material + ".mat");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            StoneGroundDisc disc = discGo.AddComponent<StoneGroundDisc>();
            SerializedObject discSO = new SerializedObject(disc);
            discSO.FindProperty("_renderer").objectReferenceValue = renderer;
            discSO.FindProperty("_isShadow").boolValue = isShadow;
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
