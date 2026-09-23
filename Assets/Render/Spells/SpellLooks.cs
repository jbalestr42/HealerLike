using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Deliveries;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Rows for the buffs and projectiles the grammar gets wrong; a row replaces the whole derived look for its key
    [CreateAssetMenu(menuName = "Custom/Data/Render/SpellLooks")]
    public class SpellLooks : SerializedScriptableObject
    {
        [DictionaryDrawerSettings(KeyLabel = "Buff", ValueLabel = "Look")]
        public Dictionary<ABuffHandlerFactory, SpellLook> buffs = new Dictionary<ABuffHandlerFactory, SpellLook>();

        [DictionaryDrawerSettings(KeyLabel = "Projectile", ValueLabel = "Look")]
        public Dictionary<GameObject, ProjectileLook> projectiles = new Dictionary<GameObject, ProjectileLook>();

        // The authored row, or null when the look is derived
        public SpellLook GetLook(ABuffHandlerFactory factory)
        {
            if (factory != null && buffs != null && buffs.ContainsKey(factory))
            {
                return buffs[factory];
            }
            return null;
        }

        public ProjectileLook GetProjectileLook(GameObject prefab)
        {
            if (prefab != null && projectiles != null && projectiles.ContainsKey(prefab))
            {
                return projectiles[prefab];
            }
            return new ProjectileLook { style = EffectDerivation.Delivery(prefab) };
        }
    }

    [Serializable]
    public class SpellLook
    {
        public EffectElement element;
        // Picks the colour, mana elements keep their own
        public EffectFamily family;
        public EffectTempo tempo;
    }

    [Serializable]
    public class ProjectileLook
    {
        public DeliveryStyle style = DeliveryStyle.Direct;
        public GestureKind presentation = GestureKind.Attack;
        public bool preserveContactPath;
    }
}
