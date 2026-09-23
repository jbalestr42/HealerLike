using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    // The view prefab drawn for each entity and character, an unmapped one gets the default of its side
    [CreateAssetMenu(menuName = "Custom/Render/CreatureLooks")]
    public class CreatureLooks : SerializedScriptableObject
    {
        [DictionaryDrawerSettings(KeyLabel = "Entity", ValueLabel = "View")]
        public Dictionary<EntityData, GameObject> entities = new Dictionary<EntityData, GameObject>();

        [DictionaryDrawerSettings(KeyLabel = "Character", ValueLabel = "View")]
        public Dictionary<CharacterData, GameObject> characters = new Dictionary<CharacterData, GameObject>();

        [AssetsOnly]
        public GameObject ally;

        [AssetsOnly]
        public GameObject enemy;

        [AssetsOnly]
        public GameObject character;

        public GameObject GetView(EntityData data, Entity.EntityType entityType)
        {
            if (data != null && entities.ContainsKey(data))
            {
                return entities[data];
            }
            return entityType == Entity.EntityType.Player ? ally : enemy;
        }

        public GameObject GetView(CharacterData data)
        {
            if (data != null && characters.ContainsKey(data))
            {
                return characters[data];
            }
            return character;
        }
    }
}
