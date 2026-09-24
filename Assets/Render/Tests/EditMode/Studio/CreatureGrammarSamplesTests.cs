using NUnit.Framework;
using UnityEngine;
using HealerLike.Render.Creatures;
using HealerLike.Render.Grammar;
using HealerLike.Render.Spells;

using HealerLike.Render.Studio.Editor;

namespace HealerLike.Render.Studio
{
    public class CreatureGrammarSamplesTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void PresetBuildsAValidRecipeThroughRealGrammar(int index)
        {
            var preset = CreatureGrammarSamples.Build(index);
            CreatureRecipe recipe = null;
            try
            {
                Assert.IsEmpty(preset.Validate());
                recipe = preset.Compose();
                Assert.IsNotNull(recipe);
                Assert.IsTrue(CreatureValidator.TryValidate(recipe, out string error), error);
                Assert.AreEqual(CreatureGrammarSamples.Names[index], preset.displayName);
            }
            finally { if (recipe) Object.DestroyImmediate(recipe); Object.DestroyImmediate(preset); }
        }
    }
}
