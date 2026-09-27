using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Rows for the buffs the grammar gets wrong; a row replaces the whole derived look for its buff
    [CreateAssetMenu(menuName = "Custom/Data/Render/SpellLooks")]
    public partial class SpellLooks : SerializedScriptableObject
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

            EffectContext context = EffectDerivation.Context(source, target);
            EffectChannels channels = EffectDerivation.Channels(factory, EffectDerivation.IsSameSide(source, target), context);
            SpellLook look = new SpellLook();
            look.element = EffectComposer.Element(channels);
            look.family = channels.family;
            look.tempo = channels.tempo;
            return look;
        }

    }
}
