using System;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    public static class EffectCompositionValidator
    {
        public const int MaxParts = 256;

        public static bool TryValidate(EffectChannels channels, EffectVocabulary vocabulary, out string error)
        {
            if (!Enum.IsDefined(typeof(EffectOperation), channels.operation)
                || !Enum.IsDefined(typeof(EffectAspect), channels.aspect)
                || !Enum.IsDefined(typeof(EffectTempo), channels.tempo))
            {
                error = "Choose valid effect grammar channel values.";
                return false;
            }

            EffectElement element;
            if (vocabulary == null || !vocabulary.TryGetElement(channels.operation, channels.aspect, out element)
                || vocabulary.GetEntry(element) == null)
            {
                error = "The effect vocabulary is missing the selected cell entry.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
