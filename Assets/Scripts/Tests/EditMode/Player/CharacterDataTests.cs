using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Players
{

// The units a reward can offer to the character
public class CharacterDataTests
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

    T Create<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _objects.Add(instance);
        return instance;
    }

    EntityData CreateEntity(string title)
    {
        EntityData entity = Create<EntityData>();
        entity.title = title;
        return entity;
    }

    [Test]
    public void GetRewardEntities_OffersTheStartingPoolAndTheRewardOnlyUnits()
    {
        CharacterData character = Create<CharacterData>();
        EntityData zealot = CreateEntity("Zealot");
        EntityData guardian = CreateEntity("Guardian");
        character.entities = new List<EntityData> { zealot };
        character.rewardEntities = new List<EntityData> { guardian };

        CollectionAssert.AreEqual(new[] { zealot, guardian }, character.GetRewardEntities());
    }

    [Test]
    public void GetRewardEntities_SkipsTheMissingUnits()
    {
        CharacterData character = Create<CharacterData>();
        EntityData zealot = CreateEntity("Zealot");
        character.entities = new List<EntityData> { null, zealot };
        character.rewardEntities = null;

        CollectionAssert.AreEqual(new[] { zealot }, character.GetRewardEntities());
    }

    [Test]
    public void GetRewardEntities_WithoutUnit_IsEmpty()
    {
        CharacterData character = Create<CharacterData>();
        character.entities = new List<EntityData>();

        Assert.IsEmpty(character.GetRewardEntities());
    }
}

}
