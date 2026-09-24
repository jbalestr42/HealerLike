using System.IO;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Environment;
using HealerLike.Render.Grass;
using HealerLike.Render.Look;
using HealerLike.Render.Spells;
using HealerLike.Render.Stones;
using HealerLike.Render.Zones;

namespace HealerLike.Render.Stage
{
    // RenderManager.prefab: the manager, its key light, the board grass and the nested sink, stone effects and controls
    public static class RenderManagerAuthoring
    {
        public static readonly string PrefabPath = "Assets/Render/Stage/Prefabs/RenderManager.prefab";
        public static readonly string CreatureLooksPath = "Assets/Render/Creatures/Data/CreatureLooks.asset";
        public static readonly string SpellLooksPath = "Assets/Render/Spells/Data/SpellLooks.asset";
        public static readonly string BoardMaterialPath = "Assets/Render/Stage/Materials/StageGround.mat";
        public static readonly string SinkPath = "Assets/Render/Spells/Prefabs/SpellVisualSink.prefab";
        public static readonly string StoneEffectsPath = "Assets/Render/Stones/Prefabs/StoneEffects.prefab";
        public static readonly string DeliveryVocabularyPath = "Assets/Render/Deliveries/Data/Resources/DeliveryVocabulary.asset";
        // Decoration in Main that the render preview hides, and the far ground under the environment plane
        public static readonly string[] HiddenObjects = { "MiddleLine", "Sphere", "Ground" };
        // The Main scene's directional light colour
        static readonly Color keyColor = new Color(1f, 0.95686275f, 0.8392157f);

        public static GameObject Create(Object pipeline, GameObject environment, GameObject controls)
        {
            GameObject root = new GameObject("RenderManager");
            RenderManager manager = root.AddComponent<RenderManager>();
            root.AddComponent<StageLauncher>();
            LookController look = root.AddComponent<LookController>();
            ZoneRegistry zones = root.AddComponent<ZoneRegistry>();
            StageRangeDriver rangeDriver = root.AddComponent<StageRangeDriver>();

            StageKeyLight keyLight = CreateKeyLight(root);
            GameObject grassGo = new GameObject("Grass");
            grassGo.transform.SetParent(root.transform, false);
            GrassField grass = grassGo.AddComponent<GrassField>();
            EnvironmentAuthoring.SetGrass(grass);
            // The tufts are laid out at the reference size, the ring strips keep the same scale
            grass.bladeHeightScale = 1f;

            SpellVisualSink sink = Nest<SpellVisualSink>(SinkPath, root);
            StoneEffects stoneEffects = Nest<StoneEffects>(StoneEffectsPath, root);
            BattleFocus battleFocus = Nest<BattleFocus>(AssetDatabase.GetAssetPath(controls), root);

            SerializedObject data = new SerializedObject(manager);
            data.FindProperty("_creatureLooks").objectReferenceValue = EnvironmentAuthoring.Load<Object>(CreatureLooksPath);
            data.FindProperty("_spellLooks").objectReferenceValue = EnvironmentAuthoring.Load<Object>(SpellLooksPath);
            data.FindProperty("_meshes").objectReferenceValue = EnvironmentAuthoring.Load<Object>(EnvironmentAuthoring.MeshesPath);
            data.FindProperty("_pipeline").objectReferenceValue = pipeline;
            data.FindProperty("_groundMaterial").objectReferenceValue = EnvironmentAuthoring.Load<Material>(BoardMaterialPath);
            data.FindProperty("_environmentPrefab").objectReferenceValue = environment.GetComponent<EnvironmentRoot>();
            data.FindProperty("_look").objectReferenceValue = look;
            data.FindProperty("_zones").objectReferenceValue = zones;
            data.FindProperty("_grass").objectReferenceValue = grass;
            data.FindProperty("_spellSink").objectReferenceValue = sink;
            data.FindProperty("_stoneEffects").objectReferenceValue = stoneEffects;
            data.FindProperty("_battleFocus").objectReferenceValue = battleFocus;
            data.FindProperty("_rangeDriver").objectReferenceValue = rangeDriver;
            data.FindProperty("_keyLight").objectReferenceValue = keyLight;
            data.FindProperty("_deliveryVocabulary").objectReferenceValue = EnvironmentAuthoring.Load<Object>(DeliveryVocabularyPath);
            SerializedProperty hidden = data.FindProperty("_hiddenObjectNames");
            hidden.arraySize = HiddenObjects.Length;
            for (int i = 0; i < HiddenObjects.Length; i++)
            {
                hidden.GetArrayElementAtIndex(i).stringValue = HiddenObjects[i];
            }

            data.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // The prefab carries the key light's aim, the manager only makes it the sun
        static StageKeyLight CreateKeyLight(GameObject root)
        {
            GameObject lightGo = new GameObject("KeyLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.rotation = StageKeyLight.Aim(StageKeyLight.KeyDirection);
            Light keyLight = lightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = keyColor;
            keyLight.intensity = 1f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.lightmapBakeType = LightmapBakeType.Realtime;
            StageKeyLight stageKeyLight = lightGo.AddComponent<StageKeyLight>();
            stageKeyLight.keyLight = keyLight;
            return stageKeyLight;
        }

        static ComponentType Nest<ComponentType>(string path, GameObject root) where ComponentType : Component
        {
            GameObject prefab = EnvironmentAuthoring.Load<GameObject>(path);
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            return instance.GetComponentInChildren<ComponentType>(true);
        }
    }
}
