using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Items
{

// Items and units match every needed tag and none of the excluded ones, a tag also matching its descendants
public class TagFilterTests
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
        Assert.IsTrue(TagFilter.Matches(CreateItem(_player), null, null));
        Assert.IsTrue(TagFilter.Matches(CreateItem(), Tags(), Tags()));
    }

    [Test]
    public void Matches_EveryIncludedTagIsNeeded()
    {
        ItemFactory cursedPlayerItem = CreateItem(_player, _cursed);
        ItemFactory playerItem = CreateItem(_player);

        Assert.IsTrue(TagFilter.Matches(cursedPlayerItem, Tags(_player, _cursed), null));
        Assert.IsFalse(TagFilter.Matches(playerItem, Tags(_player, _cursed), null));
        Assert.IsFalse(TagFilter.Matches(CreateItem(_entity), Tags(_player), null));
    }

    [Test]
    public void Matches_NoExcludedTag()
    {
        Assert.IsTrue(TagFilter.Matches(CreateItem(_player), Tags(_player), Tags(_cursed)));
        Assert.IsFalse(TagFilter.Matches(CreateItem(_player, _cursed), Tags(_player), Tags(_cursed)));
        Assert.IsFalse(TagFilter.Matches(CreateItem(_cursed), null, Tags(_cursed)));
    }

    [Test]
    public void Matches_TagsMatchTheirDescendants()
    {
        GameplayTag cursedWeapon = CreateTag("CursedWeapon", _cursed);
        ItemFactory item = CreateItem(_player, cursedWeapon);

        Assert.IsTrue(TagFilter.Matches(item, Tags(_cursed), null));
        Assert.IsFalse(TagFilter.Matches(item, Tags(_player), Tags(_cursed)));
        // Not the other way around: the parent tag isn't one of its children
        Assert.IsFalse(TagFilter.Matches(CreateItem(_cursed), Tags(cursedWeapon), null));
    }

    [Test]
    public void Matches_MissingItemOrTags_IsSafe()
    {
        Assert.IsFalse(TagFilter.Matches((AItemFactory)null, null, null));
        Assert.IsTrue(TagFilter.Matches(CreateItem(_player, null), Tags(_player), Tags(_cursed)));
        Assert.IsFalse(TagFilter.Matches(CreateItem(_player), Tags((GameplayTag)null), null));
        Assert.IsTrue(TagFilter.Matches(CreateItem(_player), null, Tags((GameplayTag)null)));
    }

    [Test]
    public void HasTag_ByName_TheTagOrOneOfItsParents()
    {
        GameplayTag cursedWeapon = CreateTag("CursedWeapon", _cursed);

        Assert.IsTrue(CreateItem(_cursed).HasTag("Cursed"));
        Assert.IsTrue(CreateItem(_player, cursedWeapon).HasTag("Cursed"));
        Assert.IsFalse(CreateItem(_player).HasTag("Cursed"));
        Assert.IsFalse(CreateItem().HasTag("Cursed"));
        Assert.IsFalse(CreateItem(_player, null).HasTag("Cursed"));
        Assert.IsFalse(CreateItem(_cursed).HasTag(""));
    }

    [Test]
    public void HasTag_ByName_CircularParentsDontLoop()
    {
        GameplayTag first = CreateTag("First");
        GameplayTag second = CreateTag("Second", first);
        TestHelpers.SetPrivateField(first, "_parent", second);

        Assert.IsFalse(CreateItem(first).HasTag("Other"));
    }

    [Test]
    public void HasTag_AnItemHasTheTagsOfItsData()
    {
        ItemFactory factory = CreateItem(_player, _cursed);

        AItem item = factory.GetItem();

        Assert.IsTrue(item.HasTag(_cursed));
        Assert.IsTrue(item.HasTag("Player"));
        Assert.IsFalse(item.HasTag(_entity));
    }

    EntityData CreateEntity(params GameplayTag[] tags)
    {
        EntityData entity = ScriptableObject.CreateInstance<EntityData>();
        entity.tags = new List<GameplayTag>(tags);
        _created.Add(entity);
        return entity;
    }

    [Test]
    public void Matches_Entity_LikeAnItem()
    {
        // e.g. the units the Druid can recruit: tagged Druid and Reward
        GameplayTag druid = CreateTag("Druid");
        GameplayTag reward = CreateTag("Reward");

        Assert.IsTrue(TagFilter.Matches(CreateEntity(druid, reward), Tags(druid, reward), null));
        Assert.IsFalse(TagFilter.Matches(CreateEntity(druid), Tags(druid, reward), null));
        Assert.IsFalse(TagFilter.Matches(CreateEntity(druid, reward, _cursed), Tags(druid, reward), Tags(_cursed)));
        Assert.IsFalse(TagFilter.Matches((EntityData)null, null, null));
    }
}

}
