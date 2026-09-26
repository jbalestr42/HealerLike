using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEditor;

namespace HealerLike.Render.Creatures
{

public class CreatureRecipeAuthoringTests
{
    string _name;
    string _path;
    CreatureRecipe _existing;
    LookVocabulary _vocabulary;

    [SetUp]
    public void SetUp()
    {
        _name = "RecipeAuthoringTest" + Guid.NewGuid().ToString("N");
        _path = "Assets/Render/Creatures/Data/" + _name + ".asset";
        _existing = ScriptableObject.CreateInstance<CreatureRecipe>();
        _existing.name = _name;
        _existing.idle.seed = 731;
        AssetDatabase.CreateAsset(_existing, _path);
        _vocabulary = RenderTestAssets.LoadLookVocabulary();
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(_path);
    }

    [TestCase(-1)]
    [TestCase(int.MaxValue)]
    public void SaveRecipe_UnsupportedArmCount_RejectsWithoutAllocationOrAssetChanges(int arms)
    {
        string before = EditorJsonUtility.ToJson(_existing);
        int recipes = Resources.FindObjectsOfTypeAll<CreatureRecipe>().Length;
        LogAssert.Expect(LogType.Error, "[CreatureRecipeAuthoring] Invalid parts, roots, arm count or vocabulary.");
        Assert.IsNull(CreatureRecipeAuthoring.SaveRecipe(_name, Parts(), 4, arms, 83, _vocabulary));
        Assert.AreEqual(before, EditorJsonUtility.ToJson(_existing));
        Assert.AreEqual(recipes, Resources.FindObjectsOfTypeAll<CreatureRecipe>().Length);
    }

    [Test]
    public void SaveRecipe_MissingPartsOrVocabulary_RejectsBeforeCreatingARecipe()
    {
        int recipes = Resources.FindObjectsOfTypeAll<CreatureRecipe>().Length;
        LogAssert.Expect(LogType.Error, "[CreatureRecipeAuthoring] Invalid parts, roots, arm count or vocabulary.");
        Assert.IsNull(CreatureRecipeAuthoring.SaveRecipe(_name, null, 4, 0, 83, _vocabulary));
        LogAssert.Expect(LogType.Error, "[CreatureRecipeAuthoring] Invalid parts, roots, arm count or vocabulary.");
        Assert.IsNull(CreatureRecipeAuthoring.SaveRecipe(_name, Parts(), 4, 0, 83, null));
        Assert.AreEqual(recipes, Resources.FindObjectsOfTypeAll<CreatureRecipe>().Length);
    }

    [Test]
    public void SaveRecipe_InvalidParts_PreservesTheExistingAssetAndCallerParts()
    {
        List<CreaturePart> parts = Parts();
        CreaturePart invalid = parts[0];
        invalid.dimensions = Vector3.zero;
        parts[0] = invalid;
        string before = EditorJsonUtility.ToJson(_existing);
        LogAssert.Expect(LogType.Error, new Regex("^\\[CreatureRecipeAuthoring\\] " + _name + ":"));
        CreatureRecipe saved = CreatureRecipeAuthoring.SaveRecipe(_name, parts, 4, 0, 83, _vocabulary);
        Assert.IsNull(saved);
        Assert.AreEqual(before, EditorJsonUtility.ToJson(_existing));
        Assert.AreEqual(Vector3.zero, parts[0].dimensions);
    }

    [Test]
    public void SaveRecipe_ValidUpdate_PreservesAssetIdentityAndDoesNotScaleCallerParts()
    {
        List<CreaturePart> parts = Parts();
        string guid = AssetDatabase.AssetPathToGUID(_path);
        CreatureRecipe saved = CreatureRecipeAuthoring.SaveRecipe(_name, parts, 4, 0, 83, _vocabulary);
        Assert.AreSame(_existing, saved);
        Assert.AreEqual(_name, saved.name);
        Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(_path));
        Assert.AreEqual(83, saved.idle.seed);
        Assert.AreEqual(Vector3.one, parts[0].dimensions);
        Assert.AreEqual(new Vector3(1.45f, 2.05f, 1.45f), saved.parts[0].dimensions);
    }

    static List<CreaturePart> Parts()
    {
        return new List<CreaturePart>
        {
            CreatureRecipeAuthoring.Part(
                "Body",
                Primitive.Sphere,
                Vector3.zero,
                Vector3.one,
                Color.white,
                parent: -1
            ),
        };
    }
}
}
