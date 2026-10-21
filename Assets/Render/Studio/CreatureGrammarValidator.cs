using System.Collections.Generic;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;

namespace HealerLike.Render.Studio
{
    // What stops a grammar preset from composing: its channels, then the vocabulary tables they select, then the
    // fragments and sizes those entries hold, then the part budget. Each stage runs only once the one before is clean,
    // so the composer is never reached with a table it would read through.
    public static class CreatureGrammarValidator
    {
        public static string[] Validate(CreatureGrammarPreset preset)
        {
            List<string> errors = new List<string>();
            if (preset == null)
            {
                errors.Add("Choose a creature grammar preset.");
                return errors.ToArray();
            }

            UnitChannels channels;
            string channelError;
            if (!preset.TryChannels(out channels, out channelError))
            {
                errors.Add(channelError);
            }

            CreatureCompositionValidator.Check(channels, preset.vocabulary, errors);
            if (errors.Count != 0)
            {
                return errors.ToArray();
            }

            CreatureGrammarBudget.Check(preset.vocabulary, channels, errors);
            return errors.ToArray();
        }

        public static LookPart[] Pick(LookPart[] plant, LookPart[] stone, bool isPlant)
        {
            if (isPlant)
            {
                return plant;
            }
            return stone;
        }
    }
}
