using NUnit.Framework;
namespace HealerLike.Render.Spells
{
    public class HLVisualRecipeTests
    {
        [Test] public void RecipeCopiesChildrenAndValidityIncludesDescendants()
        {var children=new[]{new HLVisualRecipe(default,diagnostic:"missing")};var r=new HLVisualRecipe(default,children:children);children[0]=new HLVisualRecipe(default);Assert.IsFalse(r.IsValid);}
        [Test] public void SignatureIncludesAllStyleAxes()
        {var a=new HLSpellSignature();var b=a;b.tempo=HLTempo.Death;Assert.AreNotEqual(a,b);b=a;b.variant=1;Assert.AreNotEqual(a,b);Assert.AreEqual(a.GetHashCode(),new HLSpellSignature().GetHashCode());}
    }
}
