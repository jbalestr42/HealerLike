using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

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
                cache[data] = LookComposer.Compose(LookDerivation.Channels(data, entityType));
            }
            return cache[data];
        }
    }
}
