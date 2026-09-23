using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game
{

public class DataManagerTests
{
    GameObject _go;
    DataManager _dataManager;
    GameData _gameData;
    readonly List<Object> _scriptableObjects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        // Standalone component, never going through DataManager.instance
        _go = new GameObject("DataManager");
        _dataManager = _go.AddComponent<DataManager>();
        _gameData = CreateTracked<GameData>();
        _dataManager.data = _gameData;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        foreach (Object scriptableObject in _scriptableObjects)
        {
            Object.DestroyImmediate(scriptableObject);
        }
        _scriptableObjects.Clear();
    }

    T CreateTracked<T>() where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        _scriptableObjects.Add(instance);
        return instance;
    }

    GameplayTag CreateTag(string name, GameplayTag parent = null)
    {
        GameplayTag tag = CreateTracked<GameplayTag>();
        tag.name = name;
        TestHelpers.SetPrivateField(tag, "_parent", parent);
        _gameData.tags.Add(tag);
        return tag;
    }

    ItemFactory CreateItemFactory(params GameplayTag[] tags)
    {
        ItemFactory itemFactory = CreateTracked<ItemFactory>();
        itemFactory.data = new ItemData { tags = new List<GameplayTag>(tags) };
        return itemFactory;
    }

    [Test]
    public void GetItemsWithTag_ReturnsOnlyItemsHavingTheTag()
    {
        GameplayTag entityTag = CreateTag("Entity");
        GameplayTag playerTag = CreateTag("Player");
        ItemFactory entityItem = CreateItemFactory(entityTag);
        ItemFactory playerItem = CreateItemFactory(playerTag);
        ItemFactory untaggedItem = CreateItemFactory();
        _gameData.items = new List<AItemFactory> { entityItem, playerItem, untaggedItem };

        CollectionAssert.AreEqual(new[] { entityItem }, _dataManager.GetItemsWithTag(entityTag));
        CollectionAssert.AreEqual(new[] { playerItem }, _dataManager.GetItemsWithTag(playerTag));
    }

    [Test]
    public void GetItemsWithTag_ByName_ResolvesTheRegisteredTag()
    {
        GameplayTag entityTag = CreateTag("Entity");
        GameplayTag playerTag = CreateTag("Player");
        ItemFactory entityItem = CreateItemFactory(entityTag);
        ItemFactory playerItem = CreateItemFactory(playerTag);
        _gameData.items = new List<AItemFactory> { entityItem, playerItem };

        CollectionAssert.AreEqual(new[] { entityItem }, _dataManager.GetItemsWithTag("Entity"));
        CollectionAssert.AreEqual(new[] { playerItem }, _dataManager.GetItemsWithTag("Player"));
    }

    [Test]
    public void GetItemsWithTag_IncludesItemsTaggedWithADescendant()
    {
        GameplayTag parentTag = CreateTag("Parent");
        GameplayTag childTag = CreateTag("Child", parentTag);
        ItemFactory childItem = CreateItemFactory(childTag);
        _gameData.items = new List<AItemFactory> { childItem };

        CollectionAssert.AreEqual(new[] { childItem }, _dataManager.GetItemsWithTag(parentTag));
        Assert.IsEmpty(_dataManager.GetItemsWithTag(CreateTag("Other")));
    }

    [Test]
    public void GetItemsWithTag_SkipsMissingItems()
    {
        GameplayTag tag = CreateTag("Entity");
        ItemFactory item = CreateItemFactory(tag);
        _gameData.items = new List<AItemFactory> { null, item };

        CollectionAssert.AreEqual(new[] { item }, _dataManager.GetItemsWithTag(tag));
    }

    [Test]
    public void GetRandomItemWithTag_ReturnsAnItemFromTheMatchingFactory()
    {
        GameplayTag playerTag = CreateTag("Player");
        ItemFactory entityItem = CreateItemFactory(CreateTag("Entity"));
        ItemFactory playerItem = CreateItemFactory(playerTag);
        _gameData.items = new List<AItemFactory> { entityItem, playerItem };

        AItem item = _dataManager.GetRandomItemWithTag("Player");

        CollectionAssert.AreEqual(new[] { playerTag }, item.tags);
    }
}

}
