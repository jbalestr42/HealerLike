using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Grammar;
#if UNITY_EDITOR
using UnityEditor;
#endif
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

        public GameObject GetView(EntityData data, Entity.EntityType entityType)
        {
            if (data != null && entities.ContainsKey(data))
            {
                return entities[data];
            }

            if (LookDerivation.Side(entityType) == LookSide.Plant)
            {
                return plant;
            }

            return stone;
        }

        public GameObject GetView(CharacterData data)
        {
            if (data != null && characters.ContainsKey(data))
            {
                return characters[data];
            }

            return character;
        }

        // Saves the derived recipe of an entity and gives it its own row, a host prefab carrying that recipe, to edit
        // by hand
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
            if (host == null || host.GetComponent<CreatureBuilder>() == null)
            {
                Debug.LogError("[CreatureLooks] The selected host needs a CreatureBuilder before it can be baked.");
                return null;
            }
            CreatureRecipe derived = LookComposer.Compose(LookDerivation.Channels(data, entityType), vocabulary);
            if (derived == null)
            {
                Debug.LogError($"[CreatureLooks] {data.name} has no derived recipe or no host to bake.");
                return null;
            }

            CreatureRecipe recipe = Instantiate(derived);
            DestroyImmediate(derived);
            recipe.hideFlags = HideFlags.None;
            recipe.name = data.name + "Look";
            string recipePath = AssetDatabase.GenerateUniqueAssetPath(dataFolder + recipe.name + ".asset");
            AssetDatabase.CreateAsset(recipe, recipePath);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(host);
            SerializedObject builder = new SerializedObject(instance.GetComponent<CreatureBuilder>());
            builder.FindProperty("_recipe").objectReferenceValue = recipe;
            builder.ApplyModifiedPropertiesWithoutUndo();
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabFolder + recipe.name + ".prefab");
            GameObject view = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            DestroyImmediate(instance);

            entities[data] = view;
            EditorUtility.SetDirty(this);
            if (AssetDatabase.Contains(this))
            {
                AssetDatabase.SaveAssetIfDirty(this);
            }
            Debug.Log($"[CreatureLooks] {data.name} now draws {prefabPath}.");
            return view;
#else
            return null;
#endif
        }

        // A new recipe for a derived view, owned by the caller; null when an authored view carries its own. The same
        // data always composes the same creature; nothing is kept on this shared asset.
        public CreatureRecipe GetRecipe(EntityData data, Entity.EntityType entityType)
        {
            if (data == null || entities.ContainsKey(data))
            {
                return null;
            }

            return LookComposer.Compose(LookDerivation.Channels(data, entityType), vocabulary);
        }
    }
}
