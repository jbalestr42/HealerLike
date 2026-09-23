using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.Render.Creatures
{
    public class HLCreatureRecipeTests
    {
        [Test]
        public void AuthoredHealerBowlPointsDownAndJoinsRaisedRootCrown()
        {
            string path = "Assets/Render/Creatures/Data/HLHealer.asset";
            HLCreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(path);
            Assert.NotNull(recipe);
            HLPart bulb = Array.Find(recipe.parts, part => part.id == "HLBulb");
            HLPart hip = Array.Find(recipe.parts, part => part.id == "HLHip");

            Assert.AreEqual(HLPrimitive.Cone, bulb.primitive);
            Assert.Less(Vector3.Dot(Quaternion.Euler(bulb.localEuler) * Vector3.up, Vector3.up), -0.99f,
                "The bowl must taper down toward the roots, not point up into the crown.");
            Assert.AreEqual(HLPrimitive.Sphere, hip.primitive);
            Assert.AreEqual(bulb.parent, hip.parent);
            float parentY = recipe.parts[hip.parent].localPosition.y;
            float hipY = parentY + hip.localPosition.y;
            float bowlTipY = parentY + bulb.localPosition.y - bulb.dimensions.y * 0.5f;
            Assert.LessOrEqual(Mathf.Abs(recipe.roots.hipHeight - hipY), hip.dimensions.y * 0.5f);
            Assert.LessOrEqual(Mathf.Abs(bowlTipY - hipY), hip.dimensions.y * 0.5f + 0.025f,
                "The hip joint must connect the bowl to the root crown.");
            Assert.GreaterOrEqual(recipe.roots.hipHeight, 0.55f);
            Assert.GreaterOrEqual(recipe.roots.kneeHeight, 0.30f);
            Assert.AreEqual(0.41f, recipe.roots.footRadius, 0.0001f);
            Assert.LessOrEqual(recipe.roots.footRadius + recipe.roots.thickness, 0.46f);
            Assert.IsTrue(HLCreatureValidator.TryValidate(recipe, out string error), error);
        }

        [Test]
        public void StackRootKneeClearsConicalBaseWithoutChangingFootprint()
        {
            string path = "Assets/Render/Creatures/Data/HLSphereStack.asset";
            HLCreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<HLCreatureRecipe>(path);
            Assert.NotNull(recipe);
            HLPart cone = Array.Find(recipe.parts, part => part.id == "HLConicalRoot");

            float bottom = cone.localPosition.y - cone.dimensions.y * 0.5f;
            float kneeFraction = (recipe.roots.kneeHeight - bottom) / cone.dimensions.y;
            float coneRadiusAtKnee = Mathf.Max(cone.dimensions.x, cone.dimensions.z) * 0.5f * (1f - kneeFraction);
            float kneeInnerRadius = recipe.roots.footRadius * 0.6f - recipe.roots.thickness;

            Assert.Greater(kneeInnerRadius, coneRadiusAtKnee,
                "Root knees must emerge outside the opaque conical base.");
            Assert.AreEqual(0.41f, recipe.roots.footRadius, 0.0001f);
            Assert.LessOrEqual(recipe.roots.footRadius + recipe.roots.thickness, 0.46f);
            Assert.IsTrue(HLCreatureValidator.TryValidate(recipe, out string error), error);
        }

        [Test]
        public void DefaultsHaveBoundedRootsAndIndependentArrays()
        {
            HLCreatureRecipe recipe = ScriptableObject.CreateInstance<HLCreatureRecipe>();
            try
            {
                Assert.AreEqual(4, recipe.roots.count);
                Assert.Less(recipe.roots.footRadius + recipe.roots.thickness, 0.46f);
                Assert.NotNull(recipe.parts);
                Assert.NotNull(recipe.arms);
                Assert.NotNull(recipe.sourceLocal);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(recipe);
            }
        }
    }
}
