using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Ascension
{

// What the ascension levels change in a run: more elites on the map, less mana back before each fight, a smaller
// rest heal and resurrected units coming back wounded
public class AscensionEffectsTests
{
    const string AscensionPath = "Assets/Data/Run/AscensionSettings.asset";

    readonly List<Object> _created = new List<Object>();
    readonly TestUnits _units = new TestUnits();

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
        _units.DestroyAll();
    }

    T Track<T>(T created) where T : Object
    {
        _created.Add(created);
        return created;
    }

    AscensionSettings CreateSettings(params AscensionLevel[] levels)
    {
        AscensionSettings settings = Track(ScriptableObject.CreateInstance<AscensionSettings>());
        settings.levels.AddRange(levels);
        return settings;
    }

    ConsumerFactory CreateConsumer(string name)
    {
        ConsumerFactory consumer = Track(ScriptableObject.CreateInstance<ConsumerFactory>());
        consumer.name = name;
        return consumer;
    }

    static AscensionSettings LoadGameSettings()
    {
        return AssetDatabase.LoadAssetAtPath<AscensionSettings>(AscensionPath);
    }

    [Test]
    public void EliteWeightMultiplier_MultipliesTheOnesOfTheActiveLevels()
    {
        AscensionSettings settings = CreateSettings(
            new AscensionLevel { eliteWeightMultiplier = 1.5f },
            new AscensionLevel(),
            new AscensionLevel { eliteWeightMultiplier = 2f });

        Assert.AreEqual(1f, settings.GetEliteWeightMultiplier(0), 0.0001f);
        Assert.AreEqual(1.5f, settings.GetEliteWeightMultiplier(2), 0.0001f);
        Assert.AreEqual(3f, settings.GetEliteWeightMultiplier(3), 0.0001f);
    }

    [Test]
    public void CharacterItems_AddsTheOnesOfTheActiveLevels()
    {
        ItemFactory first = Track(ScriptableObject.CreateInstance<ItemFactory>());
        ItemFactory second = Track(ScriptableObject.CreateInstance<ItemFactory>());
        AscensionSettings settings = CreateSettings(
            new AscensionLevel { characterItems = new List<AItemFactory> { first } },
            new AscensionLevel { characterItems = new List<AItemFactory> { null, second } });

        CollectionAssert.IsEmpty(settings.GetCharacterItems(0));
        CollectionAssert.AreEqual(new[] { first }, settings.GetCharacterItems(1));
        CollectionAssert.AreEqual(new[] { first, second }, settings.GetCharacterItems(2));
    }

    [Test]
    public void ResurrectConsumers_AddsTheOnesOfTheActiveLevels()
    {
        ConsumerFactory wound = CreateConsumer("Wound");
        AscensionSettings settings = CreateSettings(new AscensionLevel(), new AscensionLevel { resurrectConsumers = new List<AConsumerFactory> { wound } });

        CollectionAssert.IsEmpty(settings.GetResurrectConsumers(1));
        CollectionAssert.AreEqual(new[] { wound }, settings.GetResurrectConsumers(2));
    }

    [Test]
    public void RunMapSettings_WithoutEliteChange_AreTheMapSettings()
    {
        MapGenerationSettings map = Track(ScriptableObject.CreateInstance<MapGenerationSettings>());
        AscensionSettings settings = CreateSettings(new AscensionLevel { eliteWeightMultiplier = 1.5f });

        Assert.AreSame(map, AscensionGameType.GetRunMapSettings(map, settings, 0));
        Assert.AreSame(map, AscensionGameType.GetRunMapSettings(map, null, 1));
    }

    [Test]
    public void RunMapSettings_MoreElites_IsACopyWithOnlyTheEliteWeightChanged()
    {
        MapGenerationSettings map = Track(ScriptableObject.CreateInstance<MapGenerationSettings>());
        float eliteWeight = map.GetRoomType(MapNodeType.Elite).weight;
        float combatWeight = map.GetRoomType(MapNodeType.Combat).weight;
        AscensionSettings settings = CreateSettings(new AscensionLevel { eliteWeightMultiplier = 1.5f });

        MapGenerationSettings runMap = Track(AscensionGameType.GetRunMapSettings(map, settings, 1));

        Assert.AreNotSame(map, runMap);
        Assert.AreEqual(eliteWeight * 1.5f, runMap.GetRoomType(MapNodeType.Elite).weight, 0.0001f);
        Assert.AreEqual(combatWeight, runMap.GetRoomType(MapNodeType.Combat).weight, 0.0001f);
        Assert.AreEqual(map.floorCount, runMap.floorCount);
        Assert.AreEqual(eliteWeight, map.GetRoomType(MapNodeType.Elite).weight, 0.0001f, "The map settings asset must stay untouched");
    }

    [Test]
    public void GameLevel1_ElitesAreHalfMoreFrequent()
    {
        AscensionSettings settings = LoadGameSettings();

        Assert.AreEqual(1f, settings.GetEliteWeightMultiplier(0), 0.0001f);
        Assert.AreEqual(1.5f, settings.GetEliteWeightMultiplier(1), 0.0001f);
    }

    [Test]
    public void GameLevel3_EachFightGivesBack30PercentOfTheManaInsteadOf40()
    {
        AscensionSettings settings = LoadGameSettings();
        Character atLevel2 = CreateCharacter();
        Character atLevel3 = CreateCharacter();

        AscensionGameType.GiveItems(atLevel2, settings.GetCharacterItems(2));
        AscensionGameType.GiveItems(atLevel3, settings.GetCharacterItems(3));

        Assert.AreEqual(0.4f, AscensionGameType.GetAttribute(atLevel2.gameObject, AttributeType.CombatManaRefill), 0.0001f);
        Assert.AreEqual(0.3f, AscensionGameType.GetAttribute(atLevel3.gameObject, AttributeType.CombatManaRefill), 0.0001f);
    }

    [Test]
    public void GameLevel4_RestHeals20PercentOfTheMaxHealthInsteadOf30()
    {
        AscensionSettings settings = LoadGameSettings();
        Character atLevel3 = CreateCharacter();
        Character atLevel4 = CreateCharacter();

        AscensionGameType.GiveItems(atLevel3, settings.GetCharacterItems(3));
        AscensionGameType.GiveItems(atLevel4, settings.GetCharacterItems(4));

        Assert.AreEqual(0.3f, AscensionGameType.GetRestHeal(atLevel3.gameObject), 0.0001f);
        Assert.AreEqual(0.2f, AscensionGameType.GetRestHeal(atLevel4.gameObject), 0.0001f);
    }

    [Test]
    public void GameMaluses_AreItemsShownWithTheOtherItemsOfTheCharacter()
    {
        AscensionSettings settings = LoadGameSettings();
        Character character = CreateCharacter();
        List<AItemFactory> items = settings.GetCharacterItems(settings.maxLevel);

        AscensionGameType.GiveItems(character, items);

        Assert.AreEqual(items.Count, character.inventoryHandler.items.Count);
        foreach (InventoryItemData itemData in character.inventoryHandler.items)
        {
            Assert.IsFalse(string.IsNullOrEmpty(itemData.item.title), "An ascension item has no name");
            Assert.IsFalse(string.IsNullOrEmpty(itemData.item.description), $"{itemData.item.title} has no description");
            Assert.IsNotNull(itemData.item.icon, $"{itemData.item.title} has no icon");
        }
    }

    [Test]
    public void GameMaluses_AreNeverRewards()
    {
        AscensionSettings settings = LoadGameSettings();
        GameData gameData = AssetDatabase.LoadAssetAtPath<GameData>("Assets/Data/TestData.asset");

        foreach (AItemFactory item in settings.GetCharacterItems(settings.maxLevel))
        {
            CollectionAssert.DoesNotContain(gameData.items, item, item.name);
        }
    }

    [Test]
    public void GameLevel8_AResurrectedUnitComesBackWithHalfOfItsHealth()
    {
        AscensionSettings settings = LoadGameSettings();
        Entity resurrected = _units.Create(100f, 100f);

        CollectionAssert.IsEmpty(settings.GetResurrectConsumers(7));
        foreach (AConsumerFactory consumer in settings.GetResurrectConsumers(8))
        {
            AscensionGameType.ApplyConsumer(resurrected, consumer);
        }
        TestUnits.Process(resurrected.health);

        Assert.AreEqual(50f, resurrected.health.Value, 0.0001f);
    }

    Character CreateCharacter()
    {
        CharacterData data = Track(ScriptableObject.CreateInstance<CharacterData>());
        data.attributes = new Dictionary<AttributeType, float> { { AttributeType.ManaMax, 100f } };
        GameObject go = Track(new GameObject("Character"));
        go.AddComponent<AttributeManager>();
        go.AddComponent<BuffManager>();
        Character character = go.AddComponent<Character>();
        character.data = data;
        character.InitAttributes();
        character.InitItems();
        // Equips the items added to its inventory, as its full Init() does
        character.inventoryHandler.OnItemAdded.AddListener(character.OnItemAdded);
        return character;
    }

}

}
