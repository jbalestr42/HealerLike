using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Creatures;

namespace HealerLike.Render.Spells
{
    // The look drawn for each buff and projectile, an unmapped buff gets boon or bane from the caster's side
    [CreateAssetMenu(menuName = "Custom/Render/SpellLooks")]
    public class SpellLooks : SerializedScriptableObject
    {
        [DictionaryDrawerSettings(KeyLabel = "Buff", ValueLabel = "Look")]
        public Dictionary<ABuffHandlerFactory, SpellLook> buffs = new Dictionary<ABuffHandlerFactory, SpellLook>();

        [DictionaryDrawerSettings(KeyLabel = "Projectile", ValueLabel = "Look")]
        public Dictionary<GameObject, ProjectileLook> projectiles = new Dictionary<GameObject, ProjectileLook>();

        public SpellLook boon;
        public SpellLook bane;

        public SpellLook heal;
        public SpellLook impact;
        public SpellLook manaGain;
        public SpellLook manaLoss;
        public SpellLook chain;
        public SpellLook shield;

        // Area pulses, the hostile one is the slate litter
        public SpellLook area;
        public SpellLook hostileArea;

        public SpellLook GetLook(ABuffHandlerFactory factory, bool isSameSide)
        {
            if (factory != null && buffs.ContainsKey(factory))
            {
                return buffs[factory];
            }
            return isSameSide ? boon : bane;
        }

        public ProjectileLook GetProjectileLook(GameObject prefab)
        {
            if (prefab != null && projectiles.ContainsKey(prefab))
            {
                return projectiles[prefab];
            }
            return new ProjectileLook();
        }
    }

    [Serializable]
    public class SpellLook
    {
        [AssetsOnly]
        public SpellEffect effectPrefab;
        // Colour of the effect parts, not the body tint
        public Color tint = Color.white;
        public Vector3 offset = Vector3.zero;
    }

    [Serializable]
    public class ProjectileLook
    {
        public DeliveryStyle style = DeliveryStyle.Direct;
        public GestureKind presentation = GestureKind.Attack;
        public bool preserveContactPath;
    }
}
