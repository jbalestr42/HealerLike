using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stage
{
    // One-off: adds the view components the old builder put on the stage model copies, frees the soldier view
    // from his model, authors the block trample child and seeds CreatureLooks. Deleted once its output is committed.
    public static class ViewSeeder
    {
        static readonly string creatures = "Assets/Render/Creatures/Prefabs/";
        static readonly string soldierPath = "Assets/Render/Stones/Prefabs/HLStoneSoldierModel.prefab";
        static readonly string blockPath = "Assets/Render/Stones/Prefabs/HLStoneBlock.prefab";
        static readonly string healerPath = creatures + "HLHealerCharacter.prefab";
        static readonly string[] keep = { "BodyPivot", "HLStonePresentation", "GroundShadow" };
        static readonly Dictionary<string, string> entities = new Dictionary<string, string>
        {
            { "NormalEntity", "HLNormal" }, { "SwarmEntity", "HLSwarm" }, { "TestEntity", "HLTest" },
            { "FastShootEntity", "HLFastShoot" }, { "TripleShootEntity", "HLTripleShoot" }, { "MultiShotEntity", "HLMultiShot" },
            { "RandomShootEntity", "HLRandomShoot" }, { "ChainLightningEntity", "HLChainLightning" },
            { "ChannelingEntity", "HLChanneling" }, { "HitArmorBufferEntity", "HLHitArmorBuffer" }
        };

        public static void Seed()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            FreeSoldier();
            CreatureLooks looks = ScriptableObject.CreateInstance<CreatureLooks>();
            foreach (KeyValuePair<string, string> pair in entities)
            {
                GameObject view = Seed(creatures + pair.Value + ".prefab", true);
                looks.entities[Find<EntityData>(pair.Key)] = view;
            }

            GameObject soldier = Seed(soldierPath, false);
            looks.entities[Find<EntityData>("SoldierEntity")] = soldier;
            looks.ally = looks.entities[Find<EntityData>("NormalEntity")];
            looks.enemy = soldier;
            GameObject healer = SeedHealer();
            looks.character = healer;
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/Data" }))
            {
                looks.characters[AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid))] = healer;
            }

            AssetDatabase.CreateAsset(looks, RenderManagerAuthoring.CreatureLooksPath);
            AddBlockTrample();
            AssetDatabase.SaveAssets();
            Debug.Log($"[ViewSeeder] {looks.entities.Count} entity rows, {looks.characters.Count} character rows");
        }

        static GameObject Seed(string path, bool isAlly)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            // The view sits under his model, which already carries the model scale
            root.transform.localScale = Vector3.one;
            Add<HLStatusObserver>(root);
            Add<HLHealPulse>(root);
            Add<HLTrampleZone>(root);
            if (isAlly)
            {
                Add<HLRangePreview>(root).observePointer = false;
            }
            else
            {
                Add<HLBruiseZone>(root);
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            return saved;
        }

        static GameObject SeedHealer()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(healerPath);
            Add<HLHealPulse>(root);
            Add<HLTrampleZone>(root);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, healerPath);
            PrefabUtility.UnloadPrefabContents(root);
            return saved;
        }

        // The soldier was a variant of his Viking slime: unpack it and keep only the stone parts
        static void FreeSoldier()
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(soldierPath));
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Strip(instance.transform, true);
            PrefabUtility.SaveAsPrefabAsset(instance, soldierPath);
            Object.DestroyImmediate(instance);
        }

        static bool Strip(Transform node, bool isRoot)
        {
            bool isKept = System.Array.IndexOf(keep, node.name) >= 0;
            if (isKept)
            {
                return true;
            }

            bool hasKeptChild = false;
            for (int i = node.childCount - 1; i >= 0; i--)
            {
                hasKeptChild |= Strip(node.GetChild(i), false);
            }

            if (!isRoot && !hasKeptChild)
            {
                Object.DestroyImmediate(node.gameObject);
                return false;
            }

            // Behaviours first, the renderers and animators they require go after
            foreach (MonoBehaviour behaviour in node.GetComponents<MonoBehaviour>())
            {
                if (!behaviour.GetType().Namespace?.StartsWith("HealerLike.Render") ?? true)
                {
                    Debug.Log($"[ViewSeeder] Removed {behaviour.GetType().Name} from {node.name}");
                    Object.DestroyImmediate(behaviour);
                }
            }

            foreach (Component component in node.GetComponents<Component>())
            {
                if (!(component is Transform) && !(component is MonoBehaviour))
                {
                    Debug.Log($"[ViewSeeder] Removed {component.GetType().Name} from {node.name}");
                    Object.DestroyImmediate(component);
                }
            }

            return true;
        }

        static void AddBlockTrample()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(blockPath);
            HLStoneTerrainClump clump = root.GetComponentInChildren<HLStoneTerrainClump>(true);
            GameObject trampleGo = new GameObject("Trample");
            trampleGo.transform.SetParent(clump.transform, false);
            HLTrampleZone trample = trampleGo.AddComponent<HLTrampleZone>();
            SerializedObject data = new SerializedObject(clump);
            data.FindProperty("_trample").objectReferenceValue = trample;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, blockPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static ComponentType Add<ComponentType>(GameObject root) where ComponentType : Component
        {
            ComponentType component = root.GetComponent<ComponentType>();
            return component != null ? component : root.AddComponent<ComponentType>();
        }

        static AssetType Find<AssetType>(string name) where AssetType : Object
        {
            foreach (string guid in AssetDatabase.FindAssets(name + " t:" + typeof(AssetType).Name, new[] { "Assets/Data" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                {
                    return AssetDatabase.LoadAssetAtPath<AssetType>(path);
                }
            }

            Debug.LogError($"[ViewSeeder] No {typeof(AssetType).Name} named {name} in Assets/Data");
            return null;
        }
    }
}
