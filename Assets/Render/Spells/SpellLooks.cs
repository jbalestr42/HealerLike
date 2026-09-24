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

        // The authored row, else the look the handler's channels derive for this caster on this target
        public SpellLook GetLook(ABuffHandlerFactory factory, GameObject source, GameObject target)
        {
            if (factory != null && buffs != null && buffs.ContainsKey(factory))
            {
                return buffs[factory];
            }

            EffectChannels channels = EffectDerivation.Channels(factory, IsSameSide(source, target));
            SpellLook look = new SpellLook();
            look.element = EffectComposer.Element(channels);
            look.family = channels.family;
            look.tempo = channels.tempo;
            return look;
        }

        public ProjectileLook GetProjectileLook(GameObject prefab)
        {
            if (prefab != null && projectiles != null && projectiles.ContainsKey(prefab))
            {
                return projectiles[prefab];
            }
            return new ProjectileLook { style = EffectDerivation.Delivery(prefab) };
        }

        // A spawned projectile carries no link to its prefab, so its look is read from the behaviours baked in
        // it: a chain keeps its contact path
        public ProjectileLook GetSpawnedLook(Projectile projectile)
        {
            if (projectile == null)
            {
                return GetProjectileLook(null);
            }

            ProjectileLook look = new ProjectileLook { style = EffectDerivation.Delivery(projectile.gameObject) };
            ChainLightningProjectile chain = projectile as ChainLightningProjectile;
            if (chain != null)
            {
                look.preserveContactPath = true;
            }
            return look;
        }

        // The caster's side against the target's: the healer's Character, which is not an Entity, plays for the
        // player, and a status without a caster is taken as its target's own
        static bool IsSameSide(GameObject source, GameObject target)
        {
            if (source == null || target == null)
            {
                return true;
            }

            Entity caster = source.GetComponent<Entity>();
            Entity recipient = target.GetComponent<Entity>();
            Entity.EntityType casterSide = Entity.EntityType.Player;
            if (caster != null)
            {
                casterSide = caster.entityType;
            }

            Entity.EntityType recipientSide = Entity.EntityType.Player;
            if (recipient != null)
            {
                recipientSide = recipient.entityType;
            }

            return casterSide == recipientSide;
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
        public bool preserveContactPath;
    }
}
