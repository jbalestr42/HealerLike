using NUnit.Framework;

namespace HealerLike.Render.Spells
{
    public class HLVisualRecipeTests
    {
        [Test]
        public void RecipeCopiesChildrenAndValidityIncludesDescendants()
        {
            HLVisualRecipe[] children = new[] { new HLVisualRecipe(default, diagnostic: "missing") };
            HLVisualRecipe r = new HLVisualRecipe(default, children: children);
            children[0] = new HLVisualRecipe(default);
            Assert.IsFalse(r.IsValid);
        }

        [Test]
        public void SignatureIncludesAllStyleAxes()
        {
            HLSpellSignature a = new HLSpellSignature();
            HLSpellSignature b = a;
            b.tempo = HLTempo.Death;
            Assert.AreNotEqual(a, b);
            b = a;
            b.variant = 1;
            Assert.AreNotEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), new HLSpellSignature().GetHashCode());
        }
    }
}
