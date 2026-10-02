using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Events
{

// The Library offers items tagged Library: the ones that aren't cursed, or the cursed ones in the Dark Library
public class LibraryEventRoomTests
{
    readonly List<Object> _created = new List<Object>();
    FakeEventRoomHost _host;
    LibraryEventRoom _library;
    GameplayTag _playerTag;
    GameplayTag _libraryTag;
    GameplayTag _cursedTag;
    List<AItemFactory> _blessings;
    List<AItemFactory> _cursedItems;

    [SetUp]
    public void SetUp()
    {
        _playerTag = CreateTag("Player");
        _libraryTag = CreateTag(LibraryEventRoom.TagName);
        _cursedTag = CreateTag(CursedTag.Name);

        _blessings = new List<AItemFactory>
        {
            CreateItem("Tome of Haste", "-20% cooldown on your skills", _playerTag, _libraryTag),
            CreateItem("Scroll of Thrift", "-20% mana cost of your skills", _playerTag, _libraryTag),
            CreateItem("Codex of Mending", "+20 Heal Power", _playerTag, _libraryTag),
            CreateItem("Wellspring Manuscript", "+20% max mana", _playerTag, _libraryTag),
        };
        _cursedItems = new List<AItemFactory>
        {
            CreateItem("Hasty Grimoire", "-40% cooldown, +20% mana cost", _playerTag, _libraryTag, _cursedTag),
            CreateItem("Blood Ledger", "-40% mana cost, -20% max mana", _playerTag, _libraryTag, _cursedTag),
            CreateItem("Black Codex", "+40 Heal Power, +20% cooldown", _playerTag, _libraryTag, _cursedTag),
            CreateItem("Abyssal Well", "+40% max mana, -20 Heal Power", _playerTag, _libraryTag, _cursedTag),
        };

        _library = ScriptableObject.CreateInstance<LibraryEventRoom>();
        _created.Add(_library);
        _library.eventName = "Library";
        _library.description = "Dusty shelves.";

        _host = new FakeEventRoomHost();
        // Items of other origins never come from the Library
        _host.items.Add(CreateItem("Sacred Tome", "+10 Heal Power", _playerTag));
        _host.items.Add(CreateItem("Cursed Idol", "Cursed but not a book", _playerTag, _cursedTag));
        _host.items.AddRange(_blessings);
        _host.items.AddRange(_cursedItems);
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

    GameplayTag CreateTag(string name)
    {
        GameplayTag tag = ScriptableObject.CreateInstance<GameplayTag>();
        tag.name = name;
        _created.Add(tag);
        return tag;
    }

    ItemFactory CreateItem(string title, string description, params GameplayTag[] tags)
    {
        ItemFactory item = ScriptableObject.CreateInstance<ItemFactory>();
        item.data = new ItemData { name = title, description = description, tags = new List<GameplayTag>(tags) };
        _created.Add(item);
        return item;
    }

    static List<string> Titles(IEnumerable<AItemFactory> items)
    {
        return items.Select(item => item.title).ToList();
    }

    List<string> ShownLabels()
    {
        return _host.shownChoices.Select(choice => choice.label).ToList();
    }

    [Test]
    public void Tags_NormalTakesTheLibraryItemsThatArentCursed()
    {
        CollectionAssert.AreEqual(new[] { LibraryEventRoom.TagName }, LibraryEventRoom.GetIncludedTags(false));
        CollectionAssert.AreEqual(new[] { CursedTag.Name }, LibraryEventRoom.GetExcludedTags(false));
    }

    [Test]
    public void Tags_DarkTakesTheCursedLibraryItems()
    {
        CollectionAssert.AreEquivalent(new[] { LibraryEventRoom.TagName, CursedTag.Name }, LibraryEventRoom.GetIncludedTags(true));
        CollectionAssert.IsEmpty(LibraryEventRoom.GetExcludedTags(true));
    }

    [Test]
    public void Normal_ThreeDifferentBlessingsWithoutLeave()
    {
        for (int seed = 0; seed < 30; seed++)
        {
            _host.random = new System.Random(seed);

            _library.Play(_host);

            List<string> labels = ShownLabels();
            Assert.AreEqual(3, labels.Count);
            Assert.AreEqual(3, labels.Distinct().Count());
            CollectionAssert.IsSubsetOf(labels, Titles(_blessings));
        }
    }

    [Test]
    public void Normal_EveryBlessingCanBeOffered()
    {
        HashSet<string> offered = new HashSet<string>();
        for (int seed = 0; seed < 30; seed++)
        {
            _host.random = new System.Random(seed);
            _library.Play(_host);
            offered.UnionWith(ShownLabels());
        }

        CollectionAssert.AreEquivalent(Titles(_blessings), offered);
    }

    [Test]
    public void Normal_ChoiceShowsTheItem()
    {
        _library.Play(_host);

        Assert.AreEqual("Library", _host.shownTitle);
        Assert.AreEqual("Dusty shelves.", _host.shownDescription);
        EventChoice first = _host.shownChoices[0];
        Assert.AreEqual(_blessings.First(blessing => blessing.title == first.label).GetItem().description, first.description);
    }

    [Test]
    public void TakingAnItem_GivesItThenEndsTheEvent()
    {
        _library.Play(_host);
        string label = _host.shownChoices[1].label;

        _host.Pick(label);

        Assert.AreEqual(1, _host.addedItems.Count);
        Assert.AreEqual(label, _host.addedItems[0].title);
        Assert.AreEqual(1, _host.endCount);
    }

    [Test]
    public void Dark_ThreeDifferentCursedLibraryItemsThenLeave()
    {
        _library.isDark = true;

        for (int seed = 0; seed < 30; seed++)
        {
            _host.random = new System.Random(seed);

            _library.Play(_host);

            List<string> labels = ShownLabels();
            Assert.AreEqual(4, labels.Count);
            Assert.AreEqual("Leave", labels[3]);
            Assert.AreEqual(3, labels.Take(3).Distinct().Count());
            CollectionAssert.IsSubsetOf(labels.Take(3).ToList(), Titles(_cursedItems));
        }
    }

    [Test]
    public void Dark_TakingACursedItem_GivesIt()
    {
        _library.isDark = true;
        _library.Play(_host);
        string label = _host.shownChoices[0].label;

        _host.Pick(label);

        Assert.AreEqual(1, _host.addedItems.Count);
        Assert.AreEqual(label, _host.addedItems[0].title);
        Assert.AreEqual(1, _host.endCount);
    }

    [Test]
    public void Dark_Leave_EndsWithoutAnyItem()
    {
        _library.isDark = true;
        _library.Play(_host);

        _host.Pick("Leave");

        CollectionAssert.IsEmpty(_host.addedItems);
        Assert.AreEqual(1, _host.endCount);
    }

    [Test]
    public void Dark_FewCursedItems_OffersTheOnesThereAre()
    {
        _library.isDark = true;
        _host.items.RemoveAll(item => _cursedItems.IndexOf(item) > 0);

        _library.Play(_host);

        CollectionAssert.AreEqual(new[] { "Hasty Grimoire", "Leave" }, ShownLabels());
    }

    [Test]
    public void NoItemAtAll_OnlyLeave()
    {
        _host.items.Clear();

        _library.Play(_host);

        CollectionAssert.AreEqual(new[] { "Leave" }, ShownLabels());
    }

    [Test]
    public void ChoiceCount_IsTheNumberOfItemsOffered()
    {
        _library.choiceCount = 2;

        _library.Play(_host);

        Assert.AreEqual(2, _host.shownChoices.Count);
    }
}

}
