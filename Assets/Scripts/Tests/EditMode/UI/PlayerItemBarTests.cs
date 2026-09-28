using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{

// The items of the player shown in the game HUD: the starting ones and the ones won
public class PlayerItemBarTests
{
    class StubItem : AItem
    {
        readonly string _title;
        readonly string _description;

        public StubItem(string title, string description = "")
        {
            _title = title;
            _description = description;
        }

        public override void Equip(GameObject target) { }
        public override void Unequip(GameObject target) { }
        public override string title => _title;
        public override string description => _description;
        public override Sprite icon => null;
        public override List<GameplayTag> tags => new List<GameplayTag>();
    }

    readonly List<GameObject> _objects = new List<GameObject>();
    PlayerItemBar _bar;

    [SetUp]
    public void SetUp()
    {
        GameObject iconPrefab = Track(new GameObject("Icon Prefab"));
        PlayerItemIcon icon = iconPrefab.AddComponent<PlayerItemIcon>();
        TestHelpers.SetPrivateField(icon, "_icon", iconPrefab.AddComponent<Image>());

        _bar = Track(new GameObject("Bar")).AddComponent<PlayerItemBar>();
        TestHelpers.SetPrivateField(_bar, "_iconPrefab", icon);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _objects)
        {
            Object.DestroyImmediate(go);
        }
        _objects.Clear();
    }

    GameObject Track(GameObject go)
    {
        _objects.Add(go);
        return go;
    }

    Character CreateCharacter()
    {
        Character character = null;
        // Adding Character triggers its editor-only Reset(), which NREs without Init()
        TestHelpers.WithLoggingDisabled(() => character = Track(new GameObject("Character")).AddComponent<Character>());
        return character;
    }

    List<AItem> GetShownItems()
    {
        return _bar.icons.ConvertAll(icon => icon.item);
    }

    [Test]
    public void Show_DisplaysTheStartingItemsThenTheItemsWon()
    {
        Character character = CreateCharacter();
        AItem starting = new StubItem("Sacred Tome");
        AItem won = new StubItem("Mana Crystal");
        character.items.Add(starting);
        character.inventoryHandler.AddItem(won, -1);

        _bar.Show(character);

        CollectionAssert.AreEqual(new[] { starting, won }, GetShownItems());
        Assert.AreEqual(2, _bar.transform.childCount);
    }

    [Test]
    public void ItemWon_AfterShow_IsAdded()
    {
        Character character = CreateCharacter();
        _bar.Show(character);
        AItem won = new StubItem("Tithe");

        character.inventoryHandler.AddItem(won, -1);

        CollectionAssert.AreEqual(new[] { won }, GetShownItems());
    }

    [Test]
    public void ItemRemoved_IsNoLongerShown()
    {
        Character character = CreateCharacter();
        AItem kept = new StubItem("Tithe");
        AItem removed = new StubItem("War Drums");
        character.inventoryHandler.AddItem(kept, -1);
        character.inventoryHandler.AddItem(removed, -1);
        _bar.Show(character);

        character.inventoryHandler.RemoveItem(removed);

        CollectionAssert.AreEqual(new[] { kept }, GetShownItems());
        Assert.AreEqual(1, _bar.transform.childCount);
    }

    [Test]
    public void Show_AnotherCharacter_ReplacesTheItemsAndStopsFollowingTheFirstOne()
    {
        Character first = CreateCharacter();
        first.items.Add(new StubItem("Sacred Tome"));
        _bar.Show(first);
        Character second = CreateCharacter();
        AItem verdant = new StubItem("Verdant");
        second.items.Add(verdant);

        _bar.Show(second);
        first.inventoryHandler.AddItem(new StubItem("Tithe"), -1);

        CollectionAssert.AreEqual(new[] { verdant }, GetShownItems());
    }

    [Test]
    public void GetToolTipText_ShowsTheNameThenTheDescription()
    {
        Assert.AreEqual("<b>Tithe</b>\n+3 mana each time an enemy dies", PlayerItemIcon.GetToolTipText(new StubItem("Tithe", "+3 mana each time an enemy dies")));
    }

    [Test]
    public void GetToolTipText_WithoutDescription_ShowsTheNameOnly()
    {
        Assert.AreEqual("<b>Tithe</b>", PlayerItemIcon.GetToolTipText(new StubItem("Tithe")));
    }
}

}
