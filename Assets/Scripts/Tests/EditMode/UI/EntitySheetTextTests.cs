using System.Collections.Generic;
using NUnit.Framework;
using Entities;
using UnityEngine;

namespace UI
{

// The texts of the detail sheets: the items of a unit, and the healer's mana, stats, items and active effects
public class EntitySheetTextTests
{
    class StubItem : AItem
    {
        readonly string _title;
        readonly string _description;

        public StubItem(string title, string description)
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

    readonly TestUnits _units = new TestUnits();
    GameObject _go;
    Character _character;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Cleric");
        // Mana of 40 / 100, the max mana is a stat of the character
        ResourceAttribute mana = TestHelpers.CreateResourceAttribute(_go, ResourceType.Mana, AttributeType.ManaMax, 100f);
        mana.SetValue(40f);
        AttributeManager attributeManager = _go.GetComponent<AttributeManager>();
        attributeManager.Add(AttributeType.HealCriticalChance, new Attribute(0.25f));

        // Reset() is called by AddComponent in the Editor and needs the BuffManager of Init()
        TestHelpers.WithLoggingDisabled(() => _character = _go.AddComponent<Character>());
        TestHelpers.SetPrivateField(_character, "_attributeManager", attributeManager);
        TestHelpers.SetPrivateField(_character, "_mana", mana);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        _units.DestroyAll();
    }

    [Test]
    public void BuildCharacterBody_ShowsTheCurrentAndMaxMana()
    {
        StringAssert.Contains("<b>40 / 100</b>", EntitySheetText.BuildCharacterBody(_character));
    }

    [Test]
    public void BuildCharacterBody_ShowsEveryStatOfTheCharacter()
    {
        string body = EntitySheetText.BuildCharacterBody(_character);

        StringAssert.Contains($"{EntityInfoFormatter.GetAttributeName(AttributeType.ManaMax)}: <b>100</b>", body);
        StringAssert.Contains($"{EntityInfoFormatter.GetAttributeName(AttributeType.HealCriticalChance)}: <b>{EntityInfoFormatter.FormatNumber(0.25f)}</b>", body);
    }

    [Test]
    public void BuildCharacterBody_ShowsTheStartingItemsThenTheWonOnes()
    {
        _character.items.Add(new StubItem("Holy Book", "Heals more"));
        _character.inventoryHandler.AddItem(new StubItem("Merchant's Ledger", "+1 reward choice"), -1);

        string body = EntitySheetText.BuildCharacterBody(_character);

        string innate = EntityInfoFormatter.FormatItem(new StubItem("Holy Book", "Heals more"), true);
        string won = EntityInfoFormatter.FormatItem(new StubItem("Merchant's Ledger", "+1 reward choice"), false);
        StringAssert.Contains(innate, body);
        StringAssert.Contains(won, body);
        Assert.Less(body.IndexOf(innate), body.IndexOf(won));
    }

    [Test]
    public void BuildCharacterBody_WithoutItems_ShowsNone()
    {
        StringAssert.Contains("<b>Items</b></color>\n<color=" + EntityInfoFormatter.MutedColor + ">none</color>", EntitySheetText.BuildCharacterBody(_character));
    }

    [Test]
    public void GetPassiveLines_ListsTheItemsTheEntityIsBornWith()
    {
        Entity entity = _units.Create(100f, 100f, "Zealot");
        entity.items.Add(new StubItem("Zeal", "+50% damage while above 70% health"));
        entity.inventoryHandler.AddItem(new StubItem("Iron Plating", "+3 flat armor"), -1);

        CollectionAssert.AreEqual(new[] { "<b>Zeal</b> — +50% damage while above 70% health" }, EntitySheetText.GetPassiveLines(entity));
    }

    [Test]
    public void GetEquippedItemLines_ListsTheItemsPutInItsInventory()
    {
        Entity entity = _units.Create(100f, 100f, "Zealot");
        entity.items.Add(new StubItem("Zeal", "+50% damage while above 70% health"));
        entity.inventoryHandler.AddItem(new StubItem("Iron Plating", "+3 flat armor"), -1);

        CollectionAssert.AreEqual(new[] { "<b>Iron Plating</b> — +3 flat armor" }, EntitySheetText.GetEquippedItemLines(entity));
    }

    [Test]
    public void ItemLines_WithoutItem_AreEmpty()
    {
        Entity entity = _units.Create(100f, 100f, "Zealot");

        Assert.IsEmpty(EntitySheetText.GetPassiveLines(entity));
        Assert.IsEmpty(EntitySheetText.GetEquippedItemLines(entity));
    }
}

}
