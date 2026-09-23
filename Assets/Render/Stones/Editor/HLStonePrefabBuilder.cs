using System.IO;
using System.Linq;
using HealerLike.Render.Creatures;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HealerLike.Render.Stones
{
    public static class HLStonePrefabBuilder
    {
        static readonly string root = "Assets/Render/Stones/";

        [MenuItem("HealerLike/Build stone prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(root + "Prefabs");
            BuildEffects();
            string slimes = "Assets/Models/Kawaii Slime/Prefabs/";
            BuildModel(slimes + "Slime_01_Viking.prefab", "HLStoneSoldierModel", HLStonePreset.Boulder);
            BuildModel(slimes + "Slime_03 Leaf.prefab", "HLStoneCairnModel", HLStonePreset.Cairn);
            BuildBlock();
            foreach (string path in Directory.GetFiles("Assets/Prefabs/Projectiles", "*.prefab"))
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source.GetComponent<Projectile>() == null)
                {
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                instance.name = "HLStone" + source.name;
                if (instance.GetComponent<HLStoneProjectileImpactBridge>() == null)
                {
                    instance.AddComponent<HLStoneProjectileImpactBridge>();
                }
                PrefabUtility.SaveAsPrefabAsset(instance, root + "Prefabs/" + instance.name + ".prefab");
                Object.DestroyImmediate(instance);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Adds the authored children to the existing prefabs in place, so their file ids and variants survive
        [MenuItem("HealerLike/Add stone prefab children")]
        public static void AddChildren()
        {
            BuildEffects();
            foreach (string name in new string[] { "HLStoneSoldierModel", "HLStoneCairnModel" })
            {
                string path = root + "Prefabs/" + name + ".prefab";
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                AddModelChildren(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            string blockPath = root + "Prefabs/HLStoneBlock.prefab";
            GameObject block = PrefabUtility.LoadPrefabContents(blockPath);
            AddClumpChildren(block.GetComponentInChildren<HLStoneTerrainClump>(true));
            PrefabUtility.SaveAsPrefabAsset(block, blockPath);
            PrefabUtility.UnloadPrefabContents(block);
            AssetDatabase.SaveAssets();
        }

        static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError($"[HLStonePrefabBuilder] Missing {path}");
            }
            return asset;
        }

        static void BuildModel(string sourcePath, string name, HLStonePreset preset)
        {
            GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            GameObject modelGo = (GameObject)PrefabUtility.InstantiatePrefab(sourceModel);
            modelGo.name = name;
            // Remove the inherited render hierarchy, keep the authored HUD as a sibling of BodyPivot
            foreach (Transform child in modelGo.transform.Cast<Transform>().ToArray())
            {
                if (child.GetComponentInChildren<EntityHUD>(true) == null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
            foreach (Component component in modelGo.GetComponents<Component>())
            {
                if (!(component is Transform) && !(component is EntityModel))
                {
                    Object.DestroyImmediate(component);
                }
            }
            if (modelGo.GetComponent<EntityModel>() == null)
            {
                modelGo.AddComponent<EntityModel>();
            }

            GameObject body = new GameObject("BodyPivot");
            body.transform.SetParent(modelGo.transform, false);
            body.AddComponent<LookAtTarget>();

            GameObject source = new GameObject("SkillSource");
            source.transform.SetParent(body.transform, false);
            source.transform.localPosition = new Vector3(0f, 0.55f, 0.2f);
            source.AddComponent<SkillSource>();

            GameObject target = new GameObject("SkillTargetPoint");
            target.transform.SetParent(body.transform, false);
            target.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            target.AddComponent<SkillTargetPointTag>();

            HLStoneEnemyVisual visual = modelGo.AddComponent<HLStoneEnemyVisual>();
            SerializedObject visualSO = new SerializedObject(visual);
            visualSO.FindProperty("_bodyPivot").objectReferenceValue = body.transform;
            visualSO.FindProperty("_preset").enumValueIndex = (int)preset;
            visualSO.FindProperty("_stoneMaterial").objectReferenceValue = Load<Material>("Assets/Render/Look/HLLook_Stone.mat");
            visualSO.ApplyModifiedPropertiesWithoutUndo();
            AddModelChildren(modelGo);
            PrefabUtility.SaveAsPrefabAsset(modelGo, root + "Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(modelGo);
        }

        static void AddModelChildren(GameObject modelGo)
        {
            if (modelGo.transform.Find("GroundShadow") != null)
            {
                return;
            }

            HLStoneEnemyVisual visual = modelGo.GetComponent<HLStoneEnemyVisual>();
            Transform body = modelGo.transform.Find("BodyPivot");
            GameObject presentation = new GameObject("HLStonePresentation");
            presentation.transform.SetParent(body, false);

            SerializedObject visualSO = new SerializedObject(visual);
            visualSO.FindProperty("_presentation").objectReferenceValue = presentation.transform;
            visualSO.FindProperty("_groundShadow").objectReferenceValue = AddDisc(modelGo.transform, true);
            visualSO.FindProperty("_effects").objectReferenceValue = Load<HLStoneEffects>(root + "Prefabs/StoneEffects.prefab");
            visualSO.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildBlock()
        {
            GameObject sourceBlock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/Block.prefab");
            GameObject blockGo = (GameObject)PrefabUtility.InstantiatePrefab(sourceBlock);
            blockGo.name = "HLStoneBlock";
            foreach (MeshRenderer meshRenderer in blockGo.GetComponentsInChildren<MeshRenderer>())
            {
                Object.DestroyImmediate(meshRenderer);
            }
            foreach (MeshFilter meshFilter in blockGo.GetComponentsInChildren<MeshFilter>())
            {
                Object.DestroyImmediate(meshFilter);
            }

            GameObject child = new GameObject("HLStoneClump");
            child.layer = blockGo.layer;
            child.transform.SetParent(blockGo.transform, false);
            HLStoneTerrainClump clump = child.AddComponent<HLStoneTerrainClump>();
            SerializedObject clumpSO = new SerializedObject(clump);
            clumpSO.FindProperty("_stoneMaterial").objectReferenceValue = Load<Material>("Assets/Render/Look/HLLook_Stone.mat");
            clumpSO.ApplyModifiedPropertiesWithoutUndo();
            AddClumpChildren(clump);
            PrefabUtility.SaveAsPrefabAsset(blockGo, root + "Prefabs/HLStoneBlock.prefab");
            Object.DestroyImmediate(blockGo);
        }

        static void AddClumpChildren(HLStoneTerrainClump clump)
        {
            if (clump.transform.Find("GroundDisc") != null)
            {
                return;
            }

            GameObject face = new GameObject("HLOchreFace", typeof(MeshFilter), typeof(MeshRenderer));
            face.layer = clump.gameObject.layer;
            face.transform.SetParent(clump.transform, false);
            face.GetComponent<MeshRenderer>().sharedMaterial = Load<Material>("Assets/Render/Look/HLLook_Stone.mat");
            face.SetActive(false);

            SerializedObject clumpSO = new SerializedObject(clump);
            clumpSO.FindProperty("_groundDisc").objectReferenceValue = AddDisc(clump.transform, false);
            clumpSO.FindProperty("_groundShadow").objectReferenceValue = AddDisc(clump.transform, true);
            clumpSO.FindProperty("_ochreFace").objectReferenceValue = face.GetComponent<MeshFilter>();
            clumpSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // Bare earth or cast shadow, flat on the baked unit disc, hidden until its owner's Init
        static StoneGroundDisc AddDisc(Transform parent, bool isShadow)
        {
            GameObject discGo = new GameObject(isShadow ? "GroundShadow" : "GroundDisc", typeof(MeshFilter),
                typeof(MeshRenderer));
            discGo.layer = parent.gameObject.layer;
            discGo.transform.SetParent(parent, false);
            discGo.GetComponent<MeshFilter>().sharedMesh = Load<HLPrimitiveMeshes>(
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
            HLStoneEffects effects = effectsGo.AddComponent<HLStoneEffects>();
            SerializedObject effectsSO = new SerializedObject(effects);
            effectsSO.FindProperty("_stoneMaterial").objectReferenceValue = Load<Material>("Assets/Render/Look/HLLook_Stone.mat");
            effectsSO.FindProperty("_coralMaterial").objectReferenceValue = Load<Material>(root + "Materials/CoralSpark.mat");
            effectsSO.FindProperty("_dustMaterial").objectReferenceValue = Load<Material>(root + "Materials/Dust.mat");
            effectsSO.FindProperty("_meshes").objectReferenceValue = Load<HLPrimitiveMeshes>(
                "Assets/Render/Creatures/Data/PrimitiveMeshes.asset");
            effectsSO.FindProperty("_fragmentPrefab").objectReferenceValue = fragment;
            effectsSO.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(effectsGo, root + "Prefabs/StoneEffects.prefab");
            Object.DestroyImmediate(effectsGo);
        }
    }
}
