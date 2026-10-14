using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game
{

// The item rewards of a character: the items of its class, or of no class at all
public class ClassItemFilterTests
{
    readonly List<Object> _objects = new List<Object>();
    GameplayTag _clericTag;
    GameplayTag _druidTag;
    CharacterData _cleric;
    CharacterData _druid;
    List<CharacterData> _characters;

    [SetUp]
    public void SetUp()
    {
        _clericTag = CreateTag("Cleric");
        _druidTag = CreateTag("Druid");
        _cleric = CreateCharacter(_clericTag);
        _druid = CreateCharacter(_druidTag);
        _characters = new List<CharacterData> { _cleric, _druid };
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    GameplayTag CreateTag(string name)
    {
        GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
        tag.name = name;
        _objects.Add(tag);
        return tag;
    }

    CharacterData CreateCharacter(GameplayTag classTag)
    {
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
        character.classTag = classTag;
        _objects.Add(character);
        return character;
    }

    ItemFactory CreateItem(params GameplayTag[] tags)
    {
        ItemFactory item = ScriptableObject.CreateInstance<ItemFactory>();
        item.data = new ItemData { tags = new List<GameplayTag>(tags) };
        _objects.Add(item);
        return item;
    }

    [Test]
    public void ItemOfItsClass_ForTheCharacter()
    {
        Assert.IsTrue(ClassItemFilter.IsForCharacter(CreateItem(_clericTag), _cleric, _characters));
    }

    [Test]
    public void ItemOfAnotherClass_NotForTheCharacter()
    {
        Assert.IsFalse(ClassItemFilter.IsForCharacter(CreateItem(_druidTag), _cleric, _characters));
    }

    [Test]
    public void ItemWithoutClass_ForEveryCharacter()
    {
        ItemFactory item = CreateItem(CreateTag("Entity"));

        Assert.IsTrue(ClassItemFilter.IsForCharacter(item, _cleric, _characters));
        Assert.IsTrue(ClassItemFilter.IsForCharacter(item, _druid, _characters));
    }

    // Made for both classes
    [Test]
    public void ItemOfSeveralClasses_ForEachOfThem()
    {
        ItemFactory item = CreateItem(_druidTag, _clericTag);

        Assert.IsTrue(ClassItemFilter.IsForCharacter(item, _cleric, _characters));
        Assert.IsTrue(ClassItemFilter.IsForCharacter(item, _druid, _characters));
    }

    [Test]
    public void CharacterWithoutClass_OnlyTheItemsWithoutClass()
    {
        CharacterData classless = CreateCharacter(null);

        Assert.IsFalse(ClassItemFilter.IsForCharacter(CreateItem(_clericTag), classless, _characters));
        Assert.IsTrue(ClassItemFilter.IsForCharacter(CreateItem(), classless, _characters));
    }
}

}
