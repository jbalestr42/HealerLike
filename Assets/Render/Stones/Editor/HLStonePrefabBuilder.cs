#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Stones
{
    public static class HLStonePrefabBuilder
    {
        static readonly string root = "Assets/Render/Stones/";
        public static readonly int BlockCountDivisor = 3;

        [MenuItem("HealerLike/Build stone prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(root + "Prefabs");
            Material material = FindStoneMaterial();
            string slimes = "Assets/Models/Kawaii Slime/Prefabs/";
            BuildModel(slimes + "Slime_01_Viking.prefab", "HLStoneSoldierModel", HLStonePreset.Boulder, material);
            BuildModel(slimes + "Slime_03 Leaf.prefab", "HLStoneCairnModel", HLStonePreset.Cairn, material);
            BuildBlock(material);
            BuildGrid();
            foreach (string path in Directory.GetFiles("Assets/Prefabs/Projectiles", "*.prefab"))
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source.GetComponent<Projectile>() == null)
                {
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                try
                {
                    instance.name = "HLStone" + source.name;
                    if (instance.GetComponent<HLStoneProjectileImpactBridge>() == null)
                    {
                        instance.AddComponent<HLStoneProjectileImpactBridge>();
                    }
                    PrefabUtility.SaveAsPrefabAsset(instance, root + "Prefabs/" + instance.name + ".prefab");
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static Material FindStoneMaterial()
        {
            if (AssetDatabase.IsValidFolder("Assets/Render/Look"))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new string[] { "Assets/Render/Look" }))
                {
                    Material candidate = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    if (candidate.name.Contains("Stone"))
                    {
                        return candidate;
                    }
                }
            }

            string path = root + "HLPlaceholderStone.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", new Color32(74, 84, 104, 255));
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Metallic", 0f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        static void BuildModel(string sourcePath, string name, HLStonePreset preset, Material material)
        {
            GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            GameObject modelGo = (GameObject)PrefabUtility.InstantiatePrefab(sourceModel);
            try
            {
                modelGo.name = name;
                // Remove inherited render hierarchy, keep the authored HUD as a sibling of BodyPivot.
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
                visualSO.FindProperty("_stoneMaterial").objectReferenceValue = material;
                visualSO.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(modelGo, root + "Prefabs/" + name + ".prefab");
            }
            finally
            {
                Object.DestroyImmediate(modelGo);
            }
        }

        static void BuildBlock(Material material)
        {
            GameObject sourceBlock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid/Block.prefab");
            GameObject blockGo = (GameObject)PrefabUtility.InstantiatePrefab(sourceBlock);
            try
            {
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
                clumpSO.FindProperty("_stoneMaterial").objectReferenceValue = material;
                clumpSO.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(blockGo, root + "Prefabs/HLStoneBlock.prefab");
            }
            finally
            {
                Object.DestroyImmediate(blockGo);
            }
        }

        static void BuildGrid()
        {
            GameObject sourceGrid = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Grid.prefab");
            GameObject gridGo = (GameObject)PrefabUtility.InstantiatePrefab(sourceGrid);
            try
            {
                gridGo.name = "HLStoneGrid";
                GridGenerator generator = gridGo.GetComponent<GridGenerator>();
                SerializedObject generatorSO = new SerializedObject(generator);
                SerializedProperty list = generatorSO.FindProperty("_gridGenerators");
                // The source's first slot has an unresolved script GUID. Keep its serialized base
                // spawn settings in this variant, with an explicit basic-system fallback only here.
                if (list.arraySize > 0 && list.GetArrayElementAtIndex(0).objectReferenceValue == null)
                {
                    RestoreMissingBaseSystem(gridGo, list);
                }

                List<HLStoneBlockGridSystem> systems = new List<HLStoneBlockGridSystem>();
                for (int i = 0; i < list.arraySize; i++)
                {
                    BlockGridSystem old = list.GetArrayElementAtIndex(i).objectReferenceValue as BlockGridSystem;
                    if (old == null)
                    {
                        continue;
                    }

                    SerializedObject oldSO = new SerializedObject(old);
                    HLStoneBlockGridSystem replacement = gridGo.AddComponent<HLStoneBlockGridSystem>();
                    SerializedObject replacementSO = new SerializedObject(replacement);
                    foreach (string property in new string[] { "_minSize", "_maxSize" })
                    {
                        replacementSO.FindProperty(property).intValue = oldSO.FindProperty(property).intValue;
                    }
                    // A third of the source block counts, so the stones stop hiding the allies.
                    // Sizes and order are unchanged.
                    foreach (string property in new string[] { "_min", "_max" })
                    {
                        int count = oldSO.FindProperty(property).intValue;
                        int thinned = Mathf.RoundToInt(count / (float)BlockCountDivisor);
                        replacementSO.FindProperty(property).intValue = thinned;
                    }
                    replacementSO.FindProperty("_isWalkable").boolValue = old.isWalkable;
                    string blockPath = root + "Prefabs/HLStoneBlock.prefab";
                    GameObject stoneBlock = AssetDatabase.LoadAssetAtPath<GameObject>(blockPath);
                    replacementSO.FindProperty("_prefab").objectReferenceValue = stoneBlock;
                    replacementSO.ApplyModifiedPropertiesWithoutUndo();
                    list.GetArrayElementAtIndex(i).objectReferenceValue = replacement;
                    systems.Add(replacement);
                    Object.DestroyImmediate(old);
                }

                HLStoneGenerationFence fence = gridGo.AddComponent<HLStoneGenerationFence>();
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = fence;
                generatorSO.ApplyModifiedPropertiesWithoutUndo();

                HLStoneGridEntry entry = gridGo.AddComponent<HLStoneGridEntry>();
                SerializedObject entrySO = new SerializedObject(entry);
                entrySO.FindProperty("_generator").objectReferenceValue = generator;
                entrySO.FindProperty("_completionFence").objectReferenceValue = fence;
                SerializedProperty array = entrySO.FindProperty("_systems");
                array.arraySize = systems.Count;
                for (int i = 0; i < systems.Count; i++)
                {
                    array.GetArrayElementAtIndex(i).objectReferenceValue = systems[i];
                }
                entrySO.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(gridGo, root + "Prefabs/HLStoneGrid.prefab");
                if (saved == null)
                {
                    throw new System.InvalidOperationException("Stone grid prefab was not saved");
                }
            }
            finally
            {
                Object.DestroyImmediate(gridGo);
            }
        }

        static void RestoreMissingBaseSystem(GameObject gridGo, SerializedProperty list)
        {
            string yaml = File.ReadAllText("Assets/Prefabs/Grid.prefab");
            string block = Regex.Match(yaml, @"--- !u!114 &4796522870559552952[\s\S]*?(?=\n---|$)").Value;
            if (string.IsNullOrEmpty(block))
            {
                throw new System.InvalidOperationException(
                    "Source grid missing-slot recipe changed; review it before rebuilding");
            }

            GridGeneratorSystem basic = gridGo.AddComponent<GridGeneratorSystem>();
            SerializedObject basicSO = new SerializedObject(basic);
            foreach (string field in new string[] { "_min", "_max" })
            {
                string value = Regex.Match(block, field + @": (\d+)").Groups[1].Value;
                basicSO.FindProperty(field).intValue = int.Parse(value);
            }
            string isWalkable = Regex.Match(block, @"_isWalkable: (\d+)").Groups[1].Value;
            basicSO.FindProperty("_isWalkable").boolValue = isWalkable == "1";
            string guid = Regex.Match(block, @"_prefab: \{fileID: \d+, guid: ([0-9a-f]+)").Groups[1].Value;
            GameObject prop = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prop == null)
            {
                // This source prefab GUID is unresolved too. An empty prop keeps the
                // base spawn selection without inventing an obstacle on a walkable cell.
                GameObject placeholder = new GameObject("HLStoneGridPlaceholder");
                try
                {
                    prop = PrefabUtility.SaveAsPrefabAsset(placeholder, root + "Prefabs/HLStoneGridPlaceholder.prefab");
                }
                finally
                {
                    Object.DestroyImmediate(placeholder);
                }
            }
            basicSO.FindProperty("_prefab").objectReferenceValue = prop;
            basicSO.ApplyModifiedPropertiesWithoutUndo();
            list.GetArrayElementAtIndex(0).objectReferenceValue = basic;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gridGo);
        }
    }
}
#endif
