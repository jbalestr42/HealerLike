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
    public void EnemyItems_OnlyInTheRoomsOfTheirLevel()
    {
        ItemFactory normalOnly = Track(ScriptableObject.CreateInstance<ItemFactory>());
        ItemFactory everywhere = Track(ScriptableObject.CreateInstance<ItemFactory>());
        AscensionSettings settings = CreateSettings(
            new AscensionLevel { enemyItems = new List<AItemFactory> { normalOnly }, enemyItemRooms = new List<MapNodeType> { MapNodeType.Combat } },
            new AscensionLevel { enemyItems = new List<AItemFactory> { everywhere } });

        CollectionAssert.IsEmpty(settings.GetEnemyItems(0, MapNodeType.Combat));
        CollectionAssert.AreEqual(new[] { normalOnly }, settings.GetEnemyItems(1, MapNodeType.Combat));
        CollectionAssert.IsEmpty(settings.GetEnemyItems(1, MapNodeType.Elite));
        CollectionAssert.AreEqual(new[] { normalOnly, everywhere }, settings.GetEnemyItems(2, MapNodeType.Combat));
        CollectionAssert.AreEqual(new[] { everywhere }, settings.GetEnemyItems(2, MapNodeType.Boss));
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
    public void GameLevel7_ElitesAreHalfMoreFrequent()
    {
        AscensionSettings settings = LoadGameSettings();

        Assert.AreEqual(1f, settings.GetEliteWeightMultiplier(6), 0.0001f);
        Assert.AreEqual(1.5f, settings.GetEliteWeightMultiplier(7), 0.0001f);
    }

    [Test]
    public void GameLevel2_EachFightGivesBack30PercentOfTheManaInsteadOf40()
    {
        AscensionSettings settings = LoadGameSettings();
        Character atLevel1 = CreateCharacter();
        Character atLevel2 = CreateCharacter();

        AscensionGameType.GiveItems(atLevel1, settings.GetCharacterItems(1));
        AscensionGameType.GiveItems(atLevel2, settings.GetCharacterItems(2));

        Assert.AreEqual(0.4f, AscensionGameType.GetAttribute(atLevel1.gameObject, AttributeType.CombatManaRefill), 0.0001f);
        Assert.AreEqual(0.3f, AscensionGameType.GetAttribute(atLevel2.gameObject, AttributeType.CombatManaRefill), 0.0001f);
    }

    [Test]
    public void GameLevel3_RestHeals20PercentOfTheMaxHealthInsteadOf30()
    {
        AscensionSettings settings = LoadGameSettings();
        Character atLevel2 = CreateCharacter();
        Character atLevel3 = CreateCharacter();

        AscensionGameType.GiveItems(atLevel2, settings.GetCharacterItems(2));
        AscensionGameType.GiveItems(atLevel3, settings.GetCharacterItems(3));

        Assert.AreEqual(0.3f, AscensionGameType.GetRestHeal(atLevel2.gameObject), 0.0001f);
        Assert.AreEqual(0.2f, AscensionGameType.GetRestHeal(atLevel3.gameObject), 0.0001f);
    }

    [Test]
    public void GameLevel1_EnemiesOfNormalFightsHave10PercentMoreHealth()
    {
        AscensionSettings settings = LoadGameSettings();

        Assert.AreEqual(100f, GetWithItems(settings.GetEnemyItems(0, MapNodeType.Combat), AttributeType.HealthMax, 100f), 0.0001f);
        Assert.AreEqual(110f, GetWithItems(settings.GetEnemyItems(1, MapNodeType.Combat), AttributeType.HealthMax, 100f), 0.0001f);
        Assert.AreEqual(100f, GetWithItems(settings.GetEnemyItems(1, MapNodeType.Elite), AttributeType.HealthMax, 100f), 0.0001f);
        Assert.AreEqual(100f, GetWithItems(settings.GetEnemyItems(1, MapNodeType.Boss), AttributeType.HealthMax, 100f), 0.0001f);
    }

    [Test]
    public void GameLevel5_EnemiesOfNormalFightsDeal10PercentMoreDamage()
    {
        AscensionSettings settings = LoadGameSettings();

        Assert.AreEqual(10f, GetWithItems(settings.GetEnemyItems(4, MapNodeType.Combat), AttributeType.Damage, 10f), 0.0001f);
        Assert.AreEqual(11f, GetWithItems(settings.GetEnemyItems(5, MapNodeType.Combat), AttributeType.Damage, 10f), 0.0001f);
        Assert.AreEqual(10f, GetWithItems(settings.GetEnemyItems(5, MapNodeType.Elite), AttributeType.Damage, 10f), 0.0001f);
    }

    [Test]
    public void GameLevel6_SpellsCost10PercentMore()
    {
        AscensionSettings settings = LoadGameSettings();

        Assert.AreEqual(1f, GetWithItems(settings.GetCharacterItems(5), AttributeType.SkillCostMultiplier, 1f), 0.0001f);
        Assert.AreEqual(1.1f, GetWithItems(settings.GetCharacterItems(6), AttributeType.SkillCostMultiplier, 1f), 0.0001f);
    }

    [Test]
    public void GameLevel9_EveryEnemyAttacks10PercentFaster()
    {
        AscensionSettings settings = LoadGameSettings();

        // AttackRate is the time between two attacks: 10% faster divides it by 1.1
        Assert.AreEqual(2f, GetWithItems(settings.GetEnemyItems(8, MapNodeType.Elite), AttributeType.AttackRate, 2f), 0.0001f);
        foreach (MapNodeType room in new[] { MapNodeType.Combat, MapNodeType.Elite, MapNodeType.Boss })
        {
            List<AItemFactory> items = settings.GetEnemyItems(9, room).FindAll(item => item.name == "FrenzyItem");
            Assert.AreEqual(2f / 1.1f, GetWithItems(items, AttributeType.AttackRate, 2f), 0.0001f, room.ToString());
        }
    }

    [Test]
    public void GameLevel10_HealPowerIs10PercentLower()
    {
        AscensionSettings settings = LoadGameSettings();

        Assert.AreEqual(20f, GetWithItems(settings.GetCharacterItems(9), AttributeType.HealPower, 20f), 0.0001f);
        Assert.AreEqual(18f, GetWithItems(settings.GetCharacterItems(10), AttributeType.HealPower, 20f), 0.0001f);
    }

    [Test]
    public void GameEnemyMaluses_AreItemsWithANameADescriptionAndAnIcon()
    {
        AscensionSettings settings = LoadGameSettings();

        foreach (AItemFactory factory in settings.GetEnemyItems(settings.maxLevel, MapNodeType.Combat))
        {
            AItem item = factory.GetItem();
            Assert.IsFalse(string.IsNullOrEmpty(item.title), factory.name);
            Assert.IsFalse(string.IsNullOrEmpty(item.description), factory.name);
            Assert.IsNotNull(item.icon, factory.name);
        }
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

        List<AItemFactory> maluses = settings.GetCharacterItems(settings.maxLevel);
        maluses.AddRange(settings.GetEnemyItems(settings.maxLevel, MapNodeType.Combat));
        foreach (AItemFactory item in maluses)
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

    // The attribute once the items are equipped, on a holder starting at baseValue
    float GetWithItems(List<AItemFactory> items, AttributeType type, float baseValue)
    {
        Character holder = CreateCharacter();
        holder.attributeManager.Add(type, new Attribute(baseValue));
        AscensionGameType.GiveItems(holder, items);
        return holder.attributeManager.Get(type).Value;
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
