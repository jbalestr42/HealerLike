using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // The look drawn for each buff and projectile: an authored row wins, else the look is derived from the data
    [CreateAssetMenu(menuName = "Custom/Data/Render/SpellLooks")]
    public class SpellLooks : SerializedScriptableObject
    {
        [DictionaryDrawerSettings(KeyLabel = "Buff", ValueLabel = "Look")]
        public Dictionary<ABuffHandlerFactory, SpellLook> buffs = new Dictionary<ABuffHandlerFactory, SpellLook>();

        [DictionaryDrawerSettings(KeyLabel = "Projectile", ValueLabel = "Look")]
        public Dictionary<GameObject, ProjectileLook> projectiles = new Dictionary<GameObject, ProjectileLook>();

        public SpellLook boon;
        public SpellLook bane;
        public SpellLook rot;
        public SpellLook renew;

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
            return GetLook(EffectDerivation.Family(factory, isSameSide), EffectDerivation.Group(factory));
        }

        // Defence and prevention boons close plates around the body, offence boons orbit it
        public SpellLook GetLook(EffectFamily family, AttributeGroup group)
        {
            switch (family)
            {
                case EffectFamily.Damage:
                    return impact;
                case EffectFamily.Heal:
                    return heal;
                case EffectFamily.Rot:
                    return rot;
                case EffectFamily.Renew:
                    return renew;
                case EffectFamily.Boon:
                    return group == AttributeGroup.Offence ? boon : shield;
                default:
                    return bane;
            }
        }

        public ProjectileLook GetProjectileLook(GameObject prefab)
        {
            if (prefab != null && projectiles.ContainsKey(prefab))
            {
                return projectiles[prefab];
            }
            return new ProjectileLook { style = EffectDerivation.Delivery(prefab) };
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
