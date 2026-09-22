using NUnit.Framework;
using UnityEngine;
namespace HealerLike.Render.Creatures
{
    public class HLCreatureRecipeTests
    {
        [Test] public void DefaultsHaveBoundedRootsAndIndependentArrays()
        {
            var a = ScriptableObject.CreateInstance<HLCreatureRecipe>();
            try { Assert.AreEqual(4, a.roots.count); Assert.Less(a.roots.footRadius + a.roots.thickness, .46f); Assert.NotNull(a.parts); Assert.NotNull(a.arms); Assert.NotNull(a.sourceLocal); }
            finally { Object.DestroyImmediate(a); }
        }
    }
}
