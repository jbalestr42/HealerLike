using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System;
namespace HealerLike.Render.Creatures
{
    public class HLCreatureRecipeTests
    {
        [Test] public void AuthoredHealerBowlPointsDownAndJoinsRaisedRootCrown()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>("Assets/Render/Creatures/Data/HLHealer.asset");
            Assert.NotNull(recipe);
            var bulb = Array.Find(recipe.parts, p => p.id == "HLBulb");
            var hip = Array.Find(recipe.parts, p => p.id == "HLHip");
            Assert.AreEqual(HLPrimitive.Cone, bulb.primitive);
            Assert.Less(Vector3.Dot(Quaternion.Euler(bulb.localEuler) * Vector3.up, Vector3.up), -.99f,
                "The bowl must taper down toward the roots, not point up into the crown.");
            Assert.AreEqual(HLPrimitive.Sphere, hip.primitive);
            Assert.AreEqual(bulb.parent, hip.parent);
            float parentY = recipe.parts[hip.parent].localPosition.y;
            float hipY = parentY + hip.localPosition.y;
            float bowlTipY = parentY + bulb.localPosition.y - bulb.dimensions.y * .5f;
            Assert.LessOrEqual(Mathf.Abs(recipe.roots.hipHeight - hipY), hip.dimensions.y * .5f);
            Assert.LessOrEqual(Mathf.Abs(bowlTipY - hipY), hip.dimensions.y * .5f + .025f,
                "The hip joint must connect the bowl to the root crown.");
            Assert.GreaterOrEqual(recipe.roots.hipHeight, .55f);
            Assert.GreaterOrEqual(recipe.roots.kneeHeight, .30f);
            Assert.AreEqual(.41f, recipe.roots.footRadius, .0001f);
            Assert.LessOrEqual(recipe.roots.footRadius + recipe.roots.thickness, .46f);
            Assert.IsTrue(HLCreatureValidator.TryValidate(recipe, out string error), error);
        }
        [Test] public void DefaultsHaveBoundedRootsAndIndependentArrays()
        {
            var a = ScriptableObject.CreateInstance<HLCreatureRecipe>();
            try { Assert.AreEqual(4, a.roots.count); Assert.Less(a.roots.footRadius + a.roots.thickness, .46f); Assert.NotNull(a.parts); Assert.NotNull(a.arms); Assert.NotNull(a.sourceLocal); }
            finally { UnityEngine.Object.DestroyImmediate(a); }
        }
    }
}
