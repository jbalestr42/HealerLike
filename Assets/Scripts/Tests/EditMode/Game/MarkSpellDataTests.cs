using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game
{

// The Mark spell, given to every character (user decision 2026-10-17): 4 mana, 7s of cooldown, on one enemy
public class MarkSpellDataTests
{
    const string MarkPath = "Assets/Data/CharacterSkills/Mark/Mark.asset";

    static MarkCharacterSkillFactory LoadMark()
    {
        MarkCharacterSkillFactory mark = AssetDatabase.LoadAssetAtPath<MarkCharacterSkillFactory>(MarkPath);
        Assert.IsNotNull(mark, MarkPath);
        return mark;
    }

    [Test]
    public void Mark_OnOneEnemy_4Mana_7sCooldown()
    {
        MarkCharacterSkillData data = LoadMark().data;

        Assert.AreEqual("Mark", data.name);
        Assert.IsFalse(string.IsNullOrEmpty(data.description));
        Assert.IsNotNull(data.icon);
        Assert.IsTrue(data.isSingle);
        Assert.AreEqual(Entity.EntityType.Computer, data.entityType);
        ResourceValidatorFactory cost = (ResourceValidatorFactory)data.validators.Find(v => v is ResourceValidatorFactory r && r.data.resourceType == ResourceType.Mana);
        Assert.IsNotNull(cost, "a mana cost");
        Assert.AreEqual(4f, cost.data.consumer.data.value.GetValue(null), 0.0001f);
        DurationValidatorFactory cooldown = (DurationValidatorFactory)data.validators.Find(v => v is DurationValidatorFactory);
        Assert.IsNotNull(cooldown, "a cooldown");
        Assert.AreEqual(7f, cooldown.data.duration, 0.0001f);
    }

    [TestCase("Cleric")]
    [TestCase("Druid")]
    [TestCase("Warlock")]
    public void EveryCharacter_HasTheMark(string character)
    {
        CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/Data/Characters/{character}Character/{character}Character.asset");

        CollectionAssert.Contains(data.skills, LoadMark());
    }

    // Every spell of a character has a key (the Cleric has 6 with the Mark)
    [TestCase("Assets/Scenes/Main.unity")]
    [TestCase("Assets/Scenes/Sandbox.unity")]
    public void EverySpellOfACharacter_HasAKey(string scenePath)
    {
        int mostSpells = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/Data/Characters" }))
        {
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));
            mostSpells = Mathf.Max(mostSpells, character.skills.Count);
        }

        string scene = System.IO.File.ReadAllText(scenePath);
        int start = scene.IndexOf("_skillInputs:");
        Assert.GreaterOrEqual(start, 0, scenePath);
        int end = scene.IndexOf("OnCharacterInit:", start);
        string inputs = scene.Substring(start, end - start);
        int keys = inputs.Split(new[] { "- {fileID" }, System.StringSplitOptions.None).Length - 1;

        Assert.GreaterOrEqual(keys, mostSpells);
        // Every key an action of the input actions, none left empty (it fails when the game starts)
        StringAssert.DoesNotContain("{fileID: 0}", inputs);
        string inputGuid = AssetDatabase.AssetPathToGUID("Assets/Settings/Custom Settings/InputActions.inputactions");
        Assert.AreEqual(keys, inputs.Split(new[] { "guid: " + inputGuid }, System.StringSplitOptions.None).Length - 1);
    }

    // By default, a unit follows the mark: one not following it is a choice of its data
    [Test]
    public void EntityData_FollowsTheMarkByDefault()
    {
        EntityData data = ScriptableObject.CreateInstance<EntityData>();
        try
        {
            Assert.IsTrue(data.followsMark);
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }
}

}
