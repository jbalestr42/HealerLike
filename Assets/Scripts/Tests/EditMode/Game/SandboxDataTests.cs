using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game
{

public class SandboxDataTests
{
    readonly List<Object> _objects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _objects.Add(instance);
        return instance;
    }

    [Test]
    public void CreateCharacterData_UsesTheCharacterAttributesAndEverySkill()
    {
        CharacterData character = CreateTracked<CharacterData>();
        character.attributes = new Dictionary<AttributeType, float> { { AttributeType.ManaMax, 100f } };
        character.entities = new List<EntityData> { CreateTracked<EntityData>() };
        character.items = new List<AItemFactory> { CreateTracked<ItemFactory>() };
        SandboxData sandboxData = CreateTracked<SandboxData>();
        sandboxData.character = character;
        sandboxData.characterSkills = new List<ACharacterSkillFactory> { CreateTracked<BuffCharacterSkillFactory>(), CreateTracked<ApplyConsumerCharacterSkillFactory>() };

        CharacterData characterData = sandboxData.CreateCharacterData();
        _objects.Add(characterData);

        CollectionAssert.AreEqual(character.attributes, characterData.attributes);
        Assert.AreNotSame(character.attributes, characterData.attributes);
        CollectionAssert.AreEqual(sandboxData.characterSkills, characterData.skills);
        Assert.IsEmpty(characterData.entities);
        Assert.IsEmpty(characterData.items);
    }

    [Test]
    public void CreateCharacterData_PlayedCharacter_UsesItsAttributesSkillsAndItems()
    {
        SandboxData sandboxData = CreateTracked<SandboxData>();
        sandboxData.character = CreateTracked<CharacterData>();
        sandboxData.character.attributes = new Dictionary<AttributeType, float> { { AttributeType.ManaMax, 100f } };
        sandboxData.characterSkills = new List<ACharacterSkillFactory> { CreateTracked<BuffCharacterSkillFactory>() };
        CharacterData played = CreateTracked<CharacterData>();
        played.title = "Druid";
        played.attributes = new Dictionary<AttributeType, float> { { AttributeType.ManaMax, 80f }, { AttributeType.HealPower, 15f } };
        played.entities = new List<EntityData> { CreateTracked<EntityData>() };
        played.items = new List<AItemFactory> { CreateTracked<ItemFactory>() };
        played.skills = new List<ACharacterSkillFactory> { CreateTracked<ApplyConsumerCharacterSkillFactory>() };

        CharacterData characterData = sandboxData.CreateCharacterData(played);
        _objects.Add(characterData);

        Assert.AreEqual("Druid", characterData.title);
        CollectionAssert.AreEqual(played.attributes, characterData.attributes);
        CollectionAssert.AreEqual(played.skills, characterData.skills);
        CollectionAssert.AreEqual(played.items, characterData.items);
        // The sandbox units are placed from the sandbox panel
        Assert.IsEmpty(characterData.entities);
    }

    [Test]
    public void GetNextCharacter_CyclesFromEverySkillThroughEachCharacter()
    {
        SandboxData sandboxData = CreateTracked<SandboxData>();
        CharacterData first = CreateTracked<CharacterData>();
        CharacterData second = CreateTracked<CharacterData>();
        sandboxData.characters = new List<CharacterData> { first, second };

        Assert.AreSame(first, sandboxData.GetNextCharacter(null));
        Assert.AreSame(second, sandboxData.GetNextCharacter(first));
        Assert.IsNull(sandboxData.GetNextCharacter(second));
    }

    [Test]
    public void GetNextCharacter_WithoutCharacters_StaysOnEverySkill()
    {
        SandboxData sandboxData = CreateTracked<SandboxData>();

        Assert.IsNull(sandboxData.GetNextCharacter(null));
    }

    [Test]
    public void Waves_AreEmptyByDefault()
    {
        SandboxData sandboxData = CreateTracked<SandboxData>();

        Assert.IsNotNull(sandboxData.waves);
        Assert.IsEmpty(sandboxData.waves);
    }
}

}
