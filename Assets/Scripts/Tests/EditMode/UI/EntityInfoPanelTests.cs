using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UI
{

// The sections of the detailed panel of the selected entity
public class EntityInfoPanelTests
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
    readonly List<Object> _objects = new List<Object>();
    Entity _entity;

    [SetUp]
    public void SetUp()
    {
        _entity = _units.Create(100f, 100f, "Zealot");
    }

    [TearDown]
    public void TearDown()
    {
        _units.DestroyAll();
        foreach (Object obj in _objects)
        {
            Object.DestroyImmediate(obj);
        }
        _objects.Clear();
    }

    [Test]
    public void GetPassiveLines_ListsTheItemsTheEntityIsBornWith()
    {
        _entity.items.Add(new StubItem("Zeal", "+50% damage while above 70% health"));
        _entity.inventoryHandler.AddItem(new StubItem("Iron Plating", "+3 flat armor"), -1);

        CollectionAssert.AreEqual(new[] { "<b>Zeal</b> — +50% damage while above 70% health" }, EntityInfoPanel.GetPassiveLines(_entity));
    }

    [Test]
    public void GetEquippedItemLines_ListsTheItemsPutInItsInventory()
    {
        _entity.items.Add(new StubItem("Zeal", "+50% damage while above 70% health"));
        _entity.inventoryHandler.AddItem(new StubItem("Iron Plating", "+3 flat armor"), -1);

        CollectionAssert.AreEqual(new[] { "<b>Iron Plating</b> — +3 flat armor" }, EntityInfoPanel.GetEquippedItemLines(_entity));
    }

    EntityInfoPanel BuildPanel(bool canChangeTargeting, RectTransform inventory = null)
    {
        GameObject go = new GameObject("Entity Info", typeof(RectTransform));
        _objects.Add(go);
        EntityInfoPanel panel = go.AddComponent<EntityInfoPanel>();
        TestHelpers.SetPrivateField(panel, "_buttonPrefab", AssetDatabase.LoadAssetAtPath<SandboxButton>("Assets/Prefabs/UI/SandboxButton.prefab"));
        TestHelpers.SetPrivateField(panel, "_canChangeTargeting", canChangeTargeting);
        TestHelpers.SetPrivateField(panel, "_inventory", inventory);
        TestHelpers.InvokePrivate(panel, "Build");
        return panel;
    }

    [Test]
    public void CanChangeTargeting_TheTargetingButtonCanBeClicked()
    {
        Assert.IsTrue(BuildPanel(true).targetButton.GetComponentInChildren<UnityEngine.UI.Button>().interactable);
    }

    // In the game, the targeting of the unit is shown but the player can't change it
    [Test]
    public void CanNotChangeTargeting_TheTargetingIsShownButCanNotBeClicked()
    {
        SandboxButton targetButton = BuildPanel(false).targetButton;

        Assert.IsNotNull(targetButton);
        Assert.IsFalse(targetButton.GetComponentInChildren<UnityEngine.UI.Button>().interactable);
    }

    // In the game, the slots of the inventory are the first section of the scrolled body, under an "Inventory" title
    [Test]
    public void WithInventory_ItIsTheFirstSectionOfTheBody()
    {
        RectTransform inventory = new GameObject("Inventory", typeof(RectTransform)).GetComponent<RectTransform>();
        _objects.Add(inventory.gameObject);

        EntityInfoPanel panel = BuildPanel(false, inventory);
        RectTransform content = inventory.parent as RectTransform;

        Assert.IsNotNull(content);
        Assert.AreEqual(content, panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>().content);
        TMPro.TMP_Text[] texts = content.GetComponentsInChildren<TMPro.TMP_Text>();
        StringAssert.Contains("Inventory", texts[0].text);
        Assert.Less(texts[0].transform.GetSiblingIndex(), inventory.GetSiblingIndex());
        Assert.Less(inventory.GetSiblingIndex(), texts[texts.Length - 1].transform.GetSiblingIndex());
    }

    // Shown on an entity, the title is on top, then the slots, then the other sections below them
    [Test]
    public void WithInventory_TheSlotsAreLaidOutBetweenItsTitleAndTheOtherSections()
    {
        RectTransform inventory = new GameObject("Inventory", typeof(RectTransform)).GetComponent<RectTransform>();
        _objects.Add(inventory.gameObject);
        inventory.sizeDelta = new Vector2(200f, 80f);

        EntityInfoPanel panel = BuildPanel(false, inventory);
        float height = (float)typeof(EntityInfoPanel).GetMethod("LayoutBody", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, new object[] { "<b>Health</b>\n100 / 100" });

        TMPro.TMP_Text[] texts = inventory.parent.GetComponentsInChildren<TMPro.TMP_Text>();
        RectTransform title = texts[0].rectTransform;
        RectTransform body = texts[texts.Length - 1].rectTransform;
        Assert.LessOrEqual(inventory.anchoredPosition.y, -title.rect.height);
        Assert.LessOrEqual(body.anchoredPosition.y, inventory.anchoredPosition.y - inventory.rect.height);
        Assert.AreEqual(-body.anchoredPosition.y + body.rect.height, height, 0.01f);
    }

    // The sandbox has no inventory: no inventory title in its body
    [Test]
    public void WithoutInventory_TheBodyHasNoInventorySection()
    {
        EntityInfoPanel panel = BuildPanel(true);

        foreach (TMPro.TMP_Text text in panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>().content.GetComponentsInChildren<TMPro.TMP_Text>())
        {
            Assert.IsFalse((text.text ?? "").Contains("Inventory"));
        }
    }

    [Test]
    public void WithoutItem_BothListsAreEmpty()
    {
        Assert.IsEmpty(EntityInfoPanel.GetPassiveLines(_entity));
        Assert.IsEmpty(EntityInfoPanel.GetEquippedItemLines(_entity));
    }
}

}
