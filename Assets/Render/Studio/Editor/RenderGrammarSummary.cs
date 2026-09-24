using System.Collections;
using HealerLike.Render.Creatures;
using HealerLike.Render.Spells;

namespace HealerLike.Render.Studio.Editor
{
    // The one-line counts the grammar library shows above each table; a missing table counts as empty
    public static class RenderGrammarSummary
    {
        public static string Describe(LookVocabulary vocabulary)
        {
            return Count(vocabulary.heads) + " head presets · "
                + Count(vocabulary.accessories) + " accessory presets · "
                + Count(vocabulary.bodies) + " body bands · " + Count(vocabulary.stems) + " stem bands · "
                + Count(vocabulary.roots) + " reach bands";
        }

        public static string Describe(CreatureLooks looks)
        {
            return Count(looks.entities) + " entity overrides · " + Count(looks.characters) + " character overrides. "
                + "Empty tables mean those looks are generated from gameplay data.";
        }

        public static string Describe(SpellLooks looks)
        {
            return Count(looks.buffs) + " buff overrides. Empty tables mean the grammar supplies the look.";
        }

        public static int Count(ICollection table)
        {
            if (table == null)
            {
                return 0;
            }
            return table.Count;
        }
    }
}
