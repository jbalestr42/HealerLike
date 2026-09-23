using System.IO;
using UnityEditor;
using UnityEngine;
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
        public static readonly string BoardMaterialPath = "Assets/Render/Stage/Materials/HLStageGround.mat";
        public static readonly string SinkPath = "Assets/Render/Spells/Prefabs/HLSpellVisualSink.prefab";
        public static readonly string StoneEffectsPath = "Assets/Render/Stones/Prefabs/StoneEffects.prefab";
        // Decoration in his Main that the render preview hides, and his far ground under the environment plane
        public static readonly string[] HiddenObjects = { "MiddleLine", "Sphere", "Ground" };
        // His Main directional light colour
        static readonly Color keyColor = new Color(1f, 0.95686275f, 0.8392157f);

        public static GameObject Create(Object pipeline, GameObject environment, GameObject controls)
        {
            GameObject root = new GameObject("RenderManager");
            RenderManager manager = root.AddComponent<RenderManager>();
            root.AddComponent<StageLauncher>();
            HLLookController look = root.AddComponent<HLLookController>();
            look.settings = LookSettings();
            HLZoneRegistry zones = root.AddComponent<HLZoneRegistry>();
            HLStageRangeDriver rangeDriver = root.AddComponent<HLStageRangeDriver>();
            HLStoneDeathBridge stoneDeath = root.AddComponent<HLStoneDeathBridge>();

            HLStageKeyLight keyLight = CreateKeyLight(root);
            GameObject grassGo = new GameObject("Grass");
            grassGo.transform.SetParent(root.transform, false);
            HLGrassField grass = grassGo.AddComponent<HLGrassField>();
            EnvironmentAuthoring.SetGrass(grass);
            // A closed carpet of short spikes that leaves actor roots readable
            grass.bladeHeightScale = 0.6f;

            HLSpellVisualSink sink = Nest<HLSpellVisualSink>(SinkPath, root);
            HLStoneEffects stoneEffects = Nest<HLStoneEffects>(StoneEffectsPath, root);
            HLBattleFocus battleFocus = Nest<HLBattleFocus>(AssetDatabase.GetAssetPath(controls), root);

            SerializedObject data = new SerializedObject(manager);
            data.FindProperty("_creatureLooks").objectReferenceValue = EnvironmentAuthoring.Load<Object>(CreatureLooksPath);
            data.FindProperty("_spellLooks").objectReferenceValue = EnvironmentAuthoring.Load<Object>(SpellLooksPath);
            data.FindProperty("_meshes").objectReferenceValue = EnvironmentAuthoring.Load<Object>(EnvironmentAuthoring.MeshesPath);
            data.FindProperty("_pipeline").objectReferenceValue = pipeline;
            data.FindProperty("_groundMaterial").objectReferenceValue = EnvironmentAuthoring.Load<Material>(BoardMaterialPath);
            data.FindProperty("_environmentPrefab").objectReferenceValue = environment;
            data.FindProperty("_look").objectReferenceValue = look;
            data.FindProperty("_zones").objectReferenceValue = zones;
            data.FindProperty("_grass").objectReferenceValue = grass;
            data.FindProperty("_spellSink").objectReferenceValue = sink;
            data.FindProperty("_stoneEffects").objectReferenceValue = stoneEffects;
            data.FindProperty("_stoneDeath").objectReferenceValue = stoneDeath;
            data.FindProperty("_battleFocus").objectReferenceValue = battleFocus;
            data.FindProperty("_rangeDriver").objectReferenceValue = rangeDriver;
            data.FindProperty("_keyLight").objectReferenceValue = keyLight;
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

        // The camera independent half of the look, the manager computes fog and hatch spacing at attach
        public static HLLookSettings LookSettings()
        {
            HLLookSettings settings = HLLookSettings.Default;
            settings.shadowTint = new Color32(63, 91, 148, 255);
            settings.inkStrength = 0.75f;
            settings.fogColor = new Color32(191, 210, 224, 255);
            settings.fogBands = 6;
            settings.outlineWidthPixels = 1f;
            settings.inkSpacingPixels = 3.5f;
            return settings;
        }

        // Aimed along the stones' cheap shadow direction, so real and cheap shadows agree
        static HLStageKeyLight CreateKeyLight(GameObject root)
        {
            GameObject lightGo = new GameObject("KeyLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.rotation = HLStageKeyLight.Aim(HLStageKeyLight.StoneKeyDirection);
            Light keyLight = lightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = keyColor;
            keyLight.intensity = 1f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.lightmapBakeType = LightmapBakeType.Realtime;
            HLStageKeyLight stageKeyLight = lightGo.AddComponent<HLStageKeyLight>();
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
