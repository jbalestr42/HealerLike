using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Items
{

// An item matches every needed tag and none of the excluded ones, a tag also matching its descendants
public class ItemTagFilterTests
{
    readonly List<Object> _created = new List<Object>();
    GameplayTag _player;
    GameplayTag _entity;
    GameplayTag _cursed;

    [SetUp]
    public void SetUp()
    {
        _player = CreateTag("Player");
        _entity = CreateTag("Entity");
        _cursed = CreateTag("Cursed");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
    }

    GameplayTag CreateTag(string name, GameplayTag parent = null)
    {
        GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
        tag.name = name;
        TestHelpers.SetPrivateField(tag, "_parent", parent);
        _created.Add(tag);
        return tag;
    }

    ItemFactory CreateItem(params GameplayTag[] tags)
    {
        ItemFactory item = ScriptableObject.CreateInstance<ItemFactory>();
        item.data = new ItemData { tags = new List<GameplayTag>(tags) };
        _created.Add(item);
        return item;
    }

    static List<GameplayTag> Tags(params GameplayTag[] tags)
    {
        return new List<GameplayTag>(tags);
    }

    [Test]
    public void Matches_NoFilter_EveryItem()
    {
        Assert.IsTrue(ItemTagFilter.Matches(CreateItem(_player), null, null));
        Assert.IsTrue(ItemTagFilter.Matches(CreateItem(), Tags(), Tags()));
    }

    [Test]
    public void Matches_EveryIncludedTagIsNeeded()
    {
        ItemFactory cursedPlayerItem = CreateItem(_player, _cursed);
        ItemFactory playerItem = CreateItem(_player);

        Assert.IsTrue(ItemTagFilter.Matches(cursedPlayerItem, Tags(_player, _cursed), null));
        Assert.IsFalse(ItemTagFilter.Matches(playerItem, Tags(_player, _cursed), null));
        Assert.IsFalse(ItemTagFilter.Matches(CreateItem(_entity), Tags(_player), null));
    }

    [Test]
    public void Matches_NoExcludedTag()
    {
        Assert.IsTrue(ItemTagFilter.Matches(CreateItem(_player), Tags(_player), Tags(_cursed)));
        Assert.IsFalse(ItemTagFilter.Matches(CreateItem(_player, _cursed), Tags(_player), Tags(_cursed)));
        Assert.IsFalse(ItemTagFilter.Matches(CreateItem(_cursed), null, Tags(_cursed)));
    }

    [Test]
    public void Matches_TagsMatchTheirDescendants()
    {
        GameplayTag cursedWeapon = CreateTag("CursedWeapon", _cursed);
        ItemFactory item = CreateItem(_player, cursedWeapon);

        Assert.IsTrue(ItemTagFilter.Matches(item, Tags(_cursed), null));
        Assert.IsFalse(ItemTagFilter.Matches(item, Tags(_player), Tags(_cursed)));
        // Not the other way around: the parent tag isn't one of its children
        Assert.IsFalse(ItemTagFilter.Matches(CreateItem(_cursed), Tags(cursedWeapon), null));
    }

    [Test]
    public void Matches_MissingItemOrTags_IsSafe()
    {
        Assert.IsFalse(ItemTagFilter.Matches(null, null, null));
        Assert.IsTrue(ItemTagFilter.Matches(CreateItem(_player, null), Tags(_player), Tags(_cursed)));
        Assert.IsFalse(ItemTagFilter.Matches(CreateItem(_player), Tags((GameplayTag)null), null));
        Assert.IsTrue(ItemTagFilter.Matches(CreateItem(_player), null, Tags((GameplayTag)null)));
    }
}

}
