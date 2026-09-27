using System.Collections.Generic;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Spells
{
    // Each optional channel selects one authored piece. The stable order is additive, never a replacement.
    public static class EffectChannelComposition
    {
        static IEnumerable<ElementEntry> Entries(EffectVocabulary vocabulary, EffectChannels channels)
        {
            if (vocabulary.reach != null && vocabulary.reach.TryGetValue(channels.reach, out ElementEntry reach))
                yield return reach;
            if (vocabulary.delivery != null && vocabulary.delivery.TryGetValue(channels.delivery, out ElementEntry delivery))
                yield return delivery;
            if (vocabulary.trigger != null && vocabulary.trigger.TryGetValue(channels.trigger, out ElementEntry trigger))
                yield return trigger;
            if (vocabulary.side != null && vocabulary.side.TryGetValue(channels.side, out ElementEntry side))
                yield return side;
            if (vocabulary.origin != null && vocabulary.origin.TryGetValue(channels.origin, out ElementEntry origin))
                yield return origin;
        }

        public static bool TryValidate(EffectVocabulary vocabulary, EffectChannels channels, ElementEntry core,
            out string error)
        {
            if (!EffectValidator.TryValidateEntry(core, out error)) return false;
            long count = EffectValidator.PartCount(core);
            foreach (ElementEntry entry in Entries(vocabulary, channels))
            {
                if (!EffectValidator.TryValidateEntry(entry, out error)) return false;
                count += EffectValidator.PartCount(entry);
                if (count > EffectValidator.MaxParts)
                {
                    error = "Require 1..256 effect parts across the core and channel pieces.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        // The caller validated every selected entry. Use the entry composer so pieces never recurse into channels.
        public static EffectRecipe[] Compose(EffectVocabulary vocabulary, EffectChannels channels,
            EffectElement label, int stacks, float charges)
        {
            var additions = new List<EffectRecipe>();
            foreach (ElementEntry entry in Entries(vocabulary, channels))
            {
                EffectRecipe piece = EffectComposer.Compose(vocabulary, label, channels.family, channels.tempo,
                    channels.periodSeconds, stacks, charges, 0f, entry);
                if (piece == null) return new EffectRecipe[] { null }; // Reject the entire composition upstream.
                piece.channels = channels;
                if (piece.presentation != null && piece.presentation.enabled)
                    piece.scale *= vocabulary.MagnitudeScale(channels.magnitude);
                additions.Add(piece);
            }
            return additions.ToArray();
        }
    }
}
