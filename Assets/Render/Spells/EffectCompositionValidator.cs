using System;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    public static class EffectCompositionValidator
    {
        public const int MaxParts = 256;

        public static bool TryValidate(EffectChannels channels, EffectVocabulary vocabulary, out string error)
        {
            if (!TryValidateChannels(channels, out error)) return false;
            if (vocabulary == null || vocabulary.elements == null
                || !vocabulary.TryGetElement(channels.operation, channels.aspect, channels.tempo, out EffectElement element)
                || !Enum.IsDefined(typeof(EffectElement), element)
                || !vocabulary.elements.TryGetValue(element, out ElementEntry entry) || entry == null)
            {
                error = "The effect vocabulary is missing the selected cell entry.";
                return false;
            }
            return EffectChannelComposition.TryValidate(vocabulary, channels, entry, out error);
        }

        public static bool TryValidateChannels(EffectChannels channels, out string error)
        {
            if (!Defined(channels.operation) || !Defined(channels.aspect) || !Defined(channels.tempo)
                || !Defined(channels.magnitude) || !Defined(channels.reach) || !Defined(channels.delivery)
                || !Defined(channels.trigger) || !Defined(channels.side) || !Defined(channels.origin)
                || !Defined(channels.family) || !Defined(channels.group))
            {
                error = "Choose valid effect grammar channel values.";
                return false;
            }
            // periodSeconds is raw gameplay input. Composition intentionally falls back to the entry
            // cycle for zero, negative or nonfinite periods; validate the resolved recipe clock instead.
            error = null;
            return true;
        }

        static bool Defined<T>(T value) where T : Enum => Enum.IsDefined(typeof(T), value);
    }
}
