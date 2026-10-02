using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Game.Events
{

public class LibraryEventRoomTests
{
    readonly List<Object> _created = new List<Object>();
    FakeEventRoomHost _host;
    LibraryEventRoom _library;
    List<AItemFactory> _blessings;
    List<AItemFactory> _cursedItems;

    [SetUp]
    public void SetUp()
    {
        _blessings = new List<AItemFactory>
        {
            CreateItem("Tome of Haste", "-20% cooldown on your skills"),
            CreateItem("Scroll of Thrift", "-20% mana cost of your skills"),
            CreateItem("Codex of Mending", "+20 Heal Power"),
            CreateItem("Wellspring Manuscript", "+20% max mana"),
        };
        _cursedItems = new List<AItemFactory>
        {
            CreateItem("Hasty Grimoire", "-40% cooldown on your skills, +20% mana cost of your skills"),
            CreateItem("Blood Ledger", "-40% mana cost of your skills, -20% max mana"),
            CreateItem("Black Codex", "+40 Heal Power, +20% cooldown on your skills"),
            CreateItem("Abyssal Well", "+40% max mana, -20 Heal Power"),
        };

        _library = ScriptableObject.CreateInstance<LibraryEventRoom>();
        _created.Add(_library);
        _library.eventName = "Library";
        _library.description = "Dusty shelves.";
        _library.blessings = _blessings;

        _host = new FakeEventRoomHost();
        _host.itemsPerTag[CursedTag.Name] = _cursedItems;
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

    ItemFactory CreateItem(string title, string description)
    {
        ItemFactory item = ScriptableObject.CreateInstance<ItemFactory>();
        item.data = new ItemData { name = title, description = description };
        _created.Add(item);
        return item;
    }

    static IEnumerable<string> Titles(IEnumerable<AItemFactory> items)
    {
        return items.Select(item => item.title);
    }

    [Test]
    public void Normal_ThreeDifferentBlessingsWithoutLeave()
    {
        for (int seed = 0; seed < 30; seed++)
        {
            _host.random = new System.Random(seed);

            _library.Play(_host);

            List<string> labels = _host.shownChoices.Select(choice => choice.label).ToList();
            Assert.AreEqual(3, labels.Count);
            Assert.AreEqual(3, labels.Distinct().Count());
            CollectionAssert.IsSubsetOf(labels, Titles(_blessings).ToList());
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
            offered.UnionWith(_host.shownChoices.Select(choice => choice.label));
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
    public void Dark_ThreeDifferentCursedItemsThenLeave()
    {
        _library.isDark = true;

        for (int seed = 0; seed < 30; seed++)
        {
            _host.random = new System.Random(seed);

            _library.Play(_host);

            List<string> labels = _host.shownChoices.Select(choice => choice.label).ToList();
            Assert.AreEqual(4, labels.Count);
            Assert.AreEqual("Leave", labels[3]);
            Assert.AreEqual(3, labels.Take(3).Distinct().Count());
            CollectionAssert.IsSubsetOf(labels.Take(3).ToList(), Titles(_cursedItems).ToList());
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
        _host.itemsPerTag[CursedTag.Name] = new List<AItemFactory> { _cursedItems[0], null };

        _library.Play(_host);

        CollectionAssert.AreEqual(new[] { "Hasty Grimoire", "Leave" }, _host.shownChoices.Select(choice => choice.label));
    }

    [Test]
    public void NoItemAtAll_OnlyLeave()
    {
        _library.blessings = new List<AItemFactory>();

        _library.Play(_host);

        CollectionAssert.AreEqual(new[] { "Leave" }, _host.shownChoices.Select(choice => choice.label));
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
