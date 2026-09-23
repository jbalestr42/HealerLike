using System.Collections.Generic;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Creatures
{
    // An authored view wins for an entity or character that has a row, every other entity is derived from its data
    [CreateAssetMenu(menuName = "Custom/Data/Render/CreatureLooks")]
    public class CreatureLooks : SerializedScriptableObject
    {
        [DictionaryDrawerSettings(KeyLabel = "Entity", ValueLabel = "View")]
        public Dictionary<EntityData, GameObject> entities = new Dictionary<EntityData, GameObject>();

        [DictionaryDrawerSettings(KeyLabel = "Character", ValueLabel = "View")]
        public Dictionary<CharacterData, GameObject> characters = new Dictionary<CharacterData, GameObject>();

        // The views that host a derived recipe, one per side since the side picks the material
        [AssetsOnly]
        public GameObject plant;

        [AssetsOnly]
        public GameObject stone;

        [AssetsOnly]
        public GameObject character;

        // The parts and proportions every derived unit is composed from
        [AssetsOnly]
        public LookVocabulary vocabulary;

        static readonly string dataFolder = "Assets/Render/Creatures/Data/";
        static readonly string prefabFolder = "Assets/Render/Creatures/Prefabs/";

        // Derived once per entity and side, the same data always draws the same creature
        readonly Dictionary<EntityData, CreatureRecipe> _plants = new Dictionary<EntityData, CreatureRecipe>();
        readonly Dictionary<EntityData, CreatureRecipe> _stones = new Dictionary<EntityData, CreatureRecipe>();

        public GameObject GetView(EntityData data, Entity.EntityType entityType)
        {
            if (data != null && entities.ContainsKey(data))
            {
                return entities[data];
            }
            return LookDerivation.Side(entityType) == LookSide.Plant ? plant : stone;
        }

        public GameObject GetView(CharacterData data)
        {
            if (data != null && characters.ContainsKey(data))
            {
                return characters[data];
            }
            return character;
        }

        // Saves the derived recipe of an entity and gives it its own row, a host prefab carrying that recipe, to edit by hand
        [Button("Bake to override")]
        public GameObject BakeToOverride(EntityData data, Entity.EntityType entityType)
        {
#if UNITY_EDITOR
            if (data == null || entities.ContainsKey(data))
            {
                Debug.LogError("[CreatureLooks] Bake to override needs an entity without a row.");
                return null;
            }

            GameObject host = LookDerivation.Side(entityType) == LookSide.Plant ? plant : stone;
            CreatureRecipe derived = LookComposer.Compose(LookDerivation.Channels(data, entityType), vocabulary);
            if (derived == null || host == null)
            {
                Debug.LogError($"[CreatureLooks] {data.name} has no derived recipe or no host to bake.");
                return null;
            }

            CreatureRecipe recipe = Instantiate(derived);
            DestroyImmediate(derived);
            recipe.hideFlags = HideFlags.None;
            recipe.name = data.name + "Look";
            AssetDatabase.CreateAsset(recipe, AssetDatabase.GenerateUniqueAssetPath(dataFolder + recipe.name + ".asset"));

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(host);
            SerializedObject builder = new SerializedObject(instance.GetComponent<CreatureBuilder>());
            builder.FindProperty("_recipe").objectReferenceValue = recipe;
            builder.ApplyModifiedPropertiesWithoutUndo();
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabFolder + recipe.name + ".prefab");
            GameObject view = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            DestroyImmediate(instance);

            entities[data] = view;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CreatureLooks] {data.name} now draws {prefabPath}.");
            return view;
#else
            return null;
#endif
        }

        // The recipe for a derived view, null when an authored view carries its own
        public CreatureRecipe GetRecipe(EntityData data, Entity.EntityType entityType)
        {
            if (data == null || entities.ContainsKey(data))
            {
                return null;
            }

            Dictionary<EntityData, CreatureRecipe> cache = LookDerivation.Side(entityType) == LookSide.Plant ? _plants : _stones;
            if (!cache.ContainsKey(data) || cache[data] == null)
            {
                cache[data] = LookComposer.Compose(LookDerivation.Channels(data, entityType), vocabulary);
            }
            return cache[data];
        }
    }
}
