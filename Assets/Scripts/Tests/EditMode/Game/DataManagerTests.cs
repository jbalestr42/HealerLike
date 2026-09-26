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

    GameData.WavePool AddWavePool(MapNodeType roomType, int minFloor, int maxFloor, params WavePatternData[] waves)
    {
        GameData.WavePool pool = new GameData.WavePool
        {
            roomType = roomType,
            minFloor = minFloor,
            maxFloor = maxFloor,
            wavePatterns = new List<WavePatternData>(waves),
        };
        _gameData.wavePools.Add(pool);
        return pool;
    }

    [Test]
    public void GetWavePatterns_ReturnsWavesOfPoolsMatchingTypeAndFloor()
    {
        WavePatternData early = CreateTracked<WavePatternData>();
        WavePatternData late = CreateTracked<WavePatternData>();
        WavePatternData elite = CreateTracked<WavePatternData>();
        WavePatternData anyFloor = CreateTracked<WavePatternData>();
        AddWavePool(MapNodeType.Combat, 0, 2, early);
        AddWavePool(MapNodeType.Combat, 3, 5, late);
        AddWavePool(MapNodeType.Elite, 0, 9, elite);
        AddWavePool(MapNodeType.Combat, 0, 9, anyFloor);

        CollectionAssert.AreEquivalent(new[] { early, anyFloor }, _dataManager.GetWavePatterns(MapNodeType.Combat, 2));
        CollectionAssert.AreEquivalent(new[] { late, anyFloor }, _dataManager.GetWavePatterns(MapNodeType.Combat, 3));
        CollectionAssert.AreEquivalent(new[] { elite }, _dataManager.GetWavePatterns(MapNodeType.Elite, 5));
        Assert.IsEmpty(_dataManager.GetWavePatterns(MapNodeType.Combat, 10));
    }

    [Test]
    public void GetWavePatterns_SkipsMissingWaves()
    {
        WavePatternData wave = CreateTracked<WavePatternData>();
        AddWavePool(MapNodeType.Combat, 0, 0, null, wave);

        CollectionAssert.AreEqual(new[] { wave }, _dataManager.GetWavePatterns(MapNodeType.Combat, 0));
    }

    [Test]
    public void GetWavePattern_PicksAmongTheMatchingWaves()
    {
        WavePatternData first = CreateTracked<WavePatternData>();
        WavePatternData second = CreateTracked<WavePatternData>();
        WavePatternData otherFloor = CreateTracked<WavePatternData>();
        AddWavePool(MapNodeType.Combat, 1, 1, first, second);
        AddWavePool(MapNodeType.Combat, 2, 2, otherFloor);
        System.Random random = new System.Random(0);

        HashSet<WavePatternData> picked = new HashSet<WavePatternData>();
        for (int i = 0; i < 50; i++)
        {
            picked.Add(_dataManager.GetWavePattern(MapNodeType.Combat, 1, random));
        }

        CollectionAssert.AreEquivalent(new[] { first, second }, picked);
    }

    [Test]
    public void GetWavePattern_EliteWithoutOwnWaves_FallsBackToCombatWaves()
    {
        WavePatternData combat = CreateTracked<WavePatternData>();
        AddWavePool(MapNodeType.Combat, 4, 4, combat);

        Assert.AreSame(combat, _dataManager.GetWavePattern(MapNodeType.Elite, 4, new System.Random(0)));
    }

    [Test]
    public void GetWavePattern_EliteWithOwnWaves_DoesNotUseCombatWaves()
    {
        WavePatternData combat = CreateTracked<WavePatternData>();
        WavePatternData elite = CreateTracked<WavePatternData>();
        AddWavePool(MapNodeType.Combat, 4, 4, combat);
        AddWavePool(MapNodeType.Elite, 4, 4, elite);

        Assert.AreSame(elite, _dataManager.GetWavePattern(MapNodeType.Elite, 4, new System.Random(0)));
    }

    [Test]
    public void GetWavePattern_NoMatchingWave_ReturnsNull()
    {
        AddWavePool(MapNodeType.Combat, 0, 0, CreateTracked<WavePatternData>());

        WavePatternData wave = null;
        TestHelpers.WithLoggingDisabled(() => wave = _dataManager.GetWavePattern(MapNodeType.Combat, 3, new System.Random(0)));

        Assert.IsNull(wave);
    }
}

}
