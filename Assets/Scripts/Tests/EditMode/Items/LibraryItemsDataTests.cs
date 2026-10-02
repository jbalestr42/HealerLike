using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Items
{

// The blessings of the normal Library, event items tagged Library and never offered as regular rewards, and
// the two libraries of the event pool
public class LibraryItemsDataTests
{
    const string EventItems = "Assets/Data/EventItems/";
    const string LibraryEventPath = "Assets/Data/Run/Events/LibraryEvent.asset";
    const string DarkLibraryEventPath = "Assets/Data/Run/Events/DarkLibraryEvent.asset";

    // Path, title, attribute, value expected from a base of 100 max mana, 30 heal power and multipliers of 1
    static readonly object[] Blessings =
    {
        new object[] { "TomeOfHasteItem", "Tome of Haste", AttributeType.SkillCooldownMultiplier, 0.8f },
        new object[] { "ScrollOfThriftItem", "Scroll of Thrift", AttributeType.SkillCostMultiplier, 0.8f },
        new object[] { "CodexOfMendingItem", "Codex of Mending", AttributeType.HealPower, 50f },
        new object[] { "WellspringManuscriptItem", "Wellspring Manuscript", AttributeType.ManaMax, 120f },
    };

    GameObject _go;
    AttributeManager _attributes;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Character");
        _attributes = TestHelpers.CreateAttributeManager(_go);
        _attributes.Add(AttributeType.ManaMax, new Attribute(100f));
        _attributes.Add(AttributeType.HealPower, new Attribute(30f));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    static ItemFactory Load(string folder)
    {
        string path = EventItems + folder + "/" + folder + ".asset";
        ItemFactory item = AssetDatabase.LoadAssetAtPath<ItemFactory>(path);
        Assert.IsNotNull(item, path);
        return item;
    }

    static LibraryEventRoom LoadLibrary(string path)
    {
        LibraryEventRoom library = AssetDatabase.LoadAssetAtPath<LibraryEventRoom>(path);
        Assert.IsNotNull(library, path);
        return library;
    }

    static EventRoomChance FindInEventPool(LibraryEventRoom library)
    {
        MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Data/Run/MapGenerationSettings.asset");
        return settings.eventRooms.Find(eventRoom => eventRoom.eventRoom == library);
    }

    // Adds every buff of the item, as equipping it does
    void Equip(ItemFactory item)
    {
        foreach (ABuffHandlerFactory handler in item.data.buffs)
        {
            foreach (ABuffFactory buffFactory in handler.buffFactoryList)
            {
                buffFactory.GetBuff(null).Add(_go, _go);
            }
        }
    }

    float Get(AttributeType type)
    {
        Attribute attribute = _attributes.GetOrAdd(type);
        attribute.Update();
        return attribute.Value;
    }

    [TestCaseSource(nameof(Blessings))]
    public void Blessing_ChangesItsStat(string folder, string title, AttributeType type, float expected)
    {
        ItemFactory item = Load(folder);
        Assert.AreEqual(title, item.data.name);
        Assert.IsNotEmpty(item.data.description);
        Assert.IsNotNull(item.data.icon, title);
        Assert.IsTrue(item.data.tags.Exists(tag => tag != null && tag.name == "Player"), title);
        Assert.IsTrue(item.data.tags.Exists(tag => tag != null && tag.name == TagNames.Library), title);
        Assert.IsFalse(item.HasTag(TagNames.Cursed), title);

        Equip(item);

        Assert.AreEqual(expected, Get(type), 0.0001f, title);
    }

    [Test]
    public void Library_OffersThreeOfTheBlessings()
    {
        LibraryEventRoom library = LoadLibrary(LibraryEventPath);

        Assert.AreEqual("Library", library.eventName);
        Assert.IsNotEmpty(library.description);
        Assert.IsFalse(library.isDark);
        Assert.AreEqual(3, library.choiceCount);
        EventRoomChance chance = FindInEventPool(library);
        Assert.IsNotNull(chance);
        Assert.Greater(chance.weight, 0f);
    }

    [Test]
    public void DarkLibrary_OffersThreeCursedItemsAndIsInTheEventPool()
    {
        LibraryEventRoom library = LoadLibrary(DarkLibraryEventPath);

        Assert.AreEqual("Dark Library", library.eventName);
        Assert.IsNotEmpty(library.description);
        Assert.IsTrue(library.isDark);
        Assert.AreEqual(3, library.choiceCount);
        Assert.IsNotNull(FindInEventPool(library));
    }

    // Listed in the game items so the Library finds them by tag
    [TestCase("Assets/Data/GameData.asset")]
    [TestCase("Assets/Data/TestData.asset")]
    public void Blessings_AreGameItems(string gameDataPath)
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>(gameDataPath);
        foreach (object[] blessing in Blessings)
        {
            string folder = (string)blessing[0];
            CollectionAssert.Contains(data.items, Load(folder), folder);
        }
    }

    [TestCase("Assets/Data/GameData.asset")]
    [TestCase("Assets/Data/TestData.asset")]
    public void LibraryAndCursedTags_AreRegistered(string gameDataPath)
    {
        GameData data = AssetDatabase.LoadAssetAtPath<GameData>(gameDataPath);

        Assert.IsTrue(data.tags.Exists(tag => tag != null && tag.name == TagNames.Cursed));
        Assert.IsTrue(data.tags.Exists(tag => tag != null && tag.name == TagNames.Library));
    }
}

}
