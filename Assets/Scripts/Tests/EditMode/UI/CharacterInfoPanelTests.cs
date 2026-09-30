using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI
{

// The character panel of the game HUD: mana, stats, items and active effects of the player character
public class CharacterInfoPanelTests
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

    GameObject _go;
    Character _character;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Cleric");
        // Mana of 40 / 100, the max mana is a stat of the character
        ResourceAttribute mana = TestHelpers.CreateResourceAttribute(_go, AttributeType.ManaMax, 100f);
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
    }

    [Test]
    public void BuildBody_ShowsTheCurrentAndMaxMana()
    {
        StringAssert.Contains("<b>40 / 100</b>", CharacterInfoPanel.BuildBody(_character));
    }

    [Test]
    public void BuildBody_ShowsEveryStatOfTheCharacter()
    {
        string body = CharacterInfoPanel.BuildBody(_character);

        StringAssert.Contains($"{EntityInfoFormatter.GetAttributeName(AttributeType.ManaMax)}: <b>100</b>", body);
        StringAssert.Contains($"{EntityInfoFormatter.GetAttributeName(AttributeType.HealCriticalChance)}: <b>{EntityInfoFormatter.FormatNumber(0.25f)}</b>", body);
    }

    [Test]
    public void BuildBody_ShowsTheStartingItemsThenTheWonOnes()
    {
        _character.items.Add(new StubItem("Holy Book", "Heals more"));
        _character.inventoryHandler.AddItem(new StubItem("Merchant's Ledger", "+1 reward choice"), -1);

        string body = CharacterInfoPanel.BuildBody(_character);

        string innate = EntityInfoFormatter.FormatItem(new StubItem("Holy Book", "Heals more"), true);
        string won = EntityInfoFormatter.FormatItem(new StubItem("Merchant's Ledger", "+1 reward choice"), false);
        StringAssert.Contains(innate, body);
        StringAssert.Contains(won, body);
        Assert.Less(body.IndexOf(innate), body.IndexOf(won));
    }

    [Test]
    public void BuildBody_WithoutItems_ShowsNone()
    {
        StringAssert.Contains("<b>Items</b></color>\n<color=" + EntityInfoFormatter.MutedColor + ">none</color>", CharacterInfoPanel.BuildBody(_character));
    }
}

}
