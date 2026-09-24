using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HealerLike.Render.Grammar;
using Object = UnityEngine.Object;

namespace HealerLike.Render.Creatures
{

public class CreatureRecipeTests
{
    readonly List<Object> _scriptableObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _scriptableObjects.Add(instance);
        return instance;
    }

    [Test]
    public void Parts_HealerAsset_BowlPointsDownAndRootsLieFromTheStemFoot()
    {
        string path = "Assets/Render/Creatures/Data/Healer.asset";
        CreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(path);
        Assert.NotNull(recipe);
        CreaturePart bulb = Array.Find(recipe.parts, part => part.id == "Bulb");
        CreaturePart hip = Array.Find(recipe.parts, part => part.id == "Hip");
        CreaturePart stem = recipe.parts[0];

        Assert.AreEqual(Primitive.Cone, bulb.primitive);
        Assert.Less(Vector3.Dot(Quaternion.Euler(bulb.localEuler) * Vector3.up, Vector3.up), -0.99f,
            "The bowl must taper down toward the roots, not point up into the crown.");
        Assert.AreEqual(Primitive.Sphere, hip.primitive);
        Assert.AreEqual(bulb.parent, hip.parent);
        float stemFootY = stem.localPosition.y - stem.dimensions.y * 0.5f;
        Assert.LessOrEqual(Mathf.Abs(stemFootY), 0.01f, "The stem stands on the ground.");
        Assert.LessOrEqual(recipe.roots.hipHeight, stemFootY + 0.1f, "The roots leave the body at its base.");
        Assert.LessOrEqual(recipe.roots.kneeHeight, 0.1f, "The knee is only slightly raised.");
        LookVocabulary vocabulary = RenderTestAssets.LoadLookVocabulary();
        Assert.AreEqual(vocabulary.roots[ReachBand.Long].reach * vocabulary.bodyUnit, recipe.roots.footRadius, 0.0001f);
        Assert.LessOrEqual(recipe.roots.footRadius + recipe.roots.thickness, CreatureValidator.MaxRootReach);
        Assert.IsTrue(CreatureValidator.TryValidate(recipe, out string error), error);
    }

    [Test]
    public void Parts_HealerAsset_ValidatesWithItsRoles()
    {
        string path = "Assets/Render/Creatures/Data/Healer.asset";
        CreatureRecipe recipe = AssetDatabase.LoadAssetAtPath<CreatureRecipe>(path);

        CreaturePart crown = Array.Find(recipe.parts, part => part.id == "Crown");
        CreaturePart[] tips = Array.FindAll(recipe.parts, part => part.role == PartRole.Tip);

        Assert.AreEqual(PartRole.Crown, crown.role);
        Assert.AreEqual(3, tips.Length);
        Assert.IsTrue(Array.TrueForAll(tips, part => part.id.StartsWith("Bud", StringComparison.Ordinal)));
        Assert.IsTrue(CreatureValidator.TryValidate(recipe, out string error), error);
        Assert.AreEqual(Primitive.Torus, crown.primitive);
        Assert.That(recipe.roots.count, Is.InRange(8, 14));
        Assert.AreEqual(3, recipe.roots.segments);
    }

    [Test]
    public void Roots_NewRecipe_DefaultWithinTheLongestReachBesideEmptyArrays()
    {
        CreatureRecipe recipe = CreateTracked<CreatureRecipe>();

        Assert.AreEqual(4, recipe.roots.count);
        Assert.Less(recipe.roots.footRadius + recipe.roots.thickness, CreatureValidator.MaxRootReach);
        Assert.NotNull(recipe.parts);
        Assert.NotNull(recipe.arms);
        Assert.NotNull(recipe.sourceLocal);
    }
}

}
