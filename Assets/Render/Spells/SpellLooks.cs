using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Rows for the buffs the grammar gets wrong; a row replaces the whole derived look for its buff
    [CreateAssetMenu(menuName = "Custom/Data/Render/SpellLooks")]
    public class SpellLooks : SerializedScriptableObject
    {
        [DictionaryDrawerSettings(KeyLabel = "Buff", ValueLabel = "Look")]
        public Dictionary<ABuffHandlerFactory, SpellLook> buffs = new Dictionary<ABuffHandlerFactory, SpellLook>();

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
}
