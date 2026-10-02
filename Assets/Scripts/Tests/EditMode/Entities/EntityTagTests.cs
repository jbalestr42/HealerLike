using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// The tags of an entity, from its data or given at runtime (e.g. Summon), matched with their descendants
public class EntityTagTests
{
    GameObject _go;
    Entity _entity;
    readonly List<GameplayTag> _tags = new List<GameplayTag>();

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Entity");
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() => _entity = _go.AddComponent<Entity>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        foreach (GameplayTag tag in _tags)
        {
            Object.DestroyImmediate(tag);
        }
        _tags.Clear();
    }

    GameplayTag CreateTag(string name, GameplayTag parent = null)
    {
        GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
        tag.name = name;
        if (parent != null)
        {
            TestHelpers.SetPrivateField(tag, "_parent", parent);
        }
        _tags.Add(tag);
        return tag;
    }

    [Test]
    public void HasTag_WithoutTags_ReturnsFalse()
    {
        Assert.IsFalse(_entity.HasTag(CreateTag("Summon")));
    }

    [Test]
    public void HasTag_AfterAddTag_ReturnsTrue()
    {
        GameplayTag summon = CreateTag("Summon");

        _entity.AddTag(summon);

        Assert.IsTrue(_entity.HasTag(summon));
    }

    [Test]
    public void HasTag_OtherTag_ReturnsFalse()
    {
        _entity.AddTag(CreateTag("Undead"));

        Assert.IsFalse(_entity.HasTag(CreateTag("Summon")));
    }

    [Test]
    public void HasTag_ParentOfAnEntityTag_ReturnsTrue()
    {
        GameplayTag temporary = CreateTag("Temporary");
        _entity.AddTag(CreateTag("Summon", temporary));

        Assert.IsTrue(_entity.HasTag(temporary));
    }

    [Test]
    public void HasTag_ChildOfAnEntityTag_ReturnsFalse()
    {
        GameplayTag temporary = CreateTag("Temporary");
        _entity.AddTag(temporary);

        Assert.IsFalse(_entity.HasTag(CreateTag("Summon", temporary)));
    }

    [Test]
    public void HasTag_Null_ReturnsFalse()
    {
        _entity.AddTag(CreateTag("Summon"));

        Assert.IsFalse(_entity.HasTag((GameplayTag)null));
    }

    [Test]
    public void AddTag_Twice_IsOnlyAddedOnce()
    {
        GameplayTag summon = CreateTag("Summon");

        _entity.AddTag(summon);
        _entity.AddTag(summon);

        CollectionAssert.AreEqual(new[] { summon }, _entity.runtimeTags);
    }

    [Test]
    public void AddTag_Null_IsIgnored()
    {
        TestHelpers.WithLoggingDisabled(() => _entity.AddTag(null));

        CollectionAssert.IsEmpty(_entity.runtimeTags);
    }

    [Test]
    public void HasTag_TagOfTheData_ReturnsTrue()
    {
        GameplayTag undead = CreateTag("Undead");
        EntityData data = ScriptableObject.CreateInstance<EntityData>();
        data.tags.Add(undead);
        _entity.data = data;
        try
        {
            Assert.IsTrue(_entity.HasTag(undead));
            CollectionAssert.IsEmpty(_entity.runtimeTags);
        }
        finally
        {
            Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void RemoveTag_RemovesTheTag()
    {
        GameplayTag summon = CreateTag("Summon");
        _entity.AddTag(summon);

        _entity.RemoveTag(summon);

        Assert.IsFalse(_entity.HasTag(summon));
    }
}

}
