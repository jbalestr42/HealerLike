using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI
{

// The sandbox lists the player items apart from the entity items
public class SandboxPanelTests
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

    ItemFactory CreateItem(params string[] tagNames)
    {
        ItemFactory item = ScriptableObject.CreateInstance<ItemFactory>();
        _objects.Add(item);
        item.data = new ItemData();
        foreach (string tagName in tagNames)
        {
            GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
            tag.name = tagName;
            _objects.Add(tag);
            item.data.tags.Add(tag);
        }
        return item;
    }

    [Test]
    public void IsPlayerItem_WithThePlayerTag_IsTrue()
    {
        Assert.IsTrue(SandboxPanel.IsPlayerItem(CreateItem("Player")));
    }

    [Test]
    public void IsPlayerItem_AnEntityItem_IsFalse()
    {
        Assert.IsFalse(SandboxPanel.IsPlayerItem(CreateItem("Entity")));
    }

    [Test]
    public void IsPlayerItem_WithoutTag_IsFalse()
    {
        Assert.IsFalse(SandboxPanel.IsPlayerItem(CreateItem()));
    }
}

}
