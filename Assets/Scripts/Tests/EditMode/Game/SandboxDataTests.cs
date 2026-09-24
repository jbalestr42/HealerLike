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
        character.passives = new List<ABuffHandlerFactory> { CreateTracked<BuffHandlerFactory>() };
        SandboxData sandboxData = CreateTracked<SandboxData>();
        sandboxData.character = character;
        sandboxData.characterSkills = new List<ACharacterSkillFactory> { CreateTracked<BuffCharacterSkillFactory>(), CreateTracked<ApplyConsumerCharacterSkillFactory>() };

        CharacterData characterData = sandboxData.CreateCharacterData();
        _objects.Add(characterData);

        CollectionAssert.AreEqual(character.attributes, characterData.attributes);
        Assert.AreNotSame(character.attributes, characterData.attributes);
        CollectionAssert.AreEqual(sandboxData.characterSkills, characterData.skills);
        Assert.IsEmpty(characterData.entities);
        Assert.IsEmpty(characterData.passives);
    }
}

}
