using System.Collections.Generic;
using Entities;
using NUnit.Framework;
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

    EntityInfoPanel BuildPanel(bool canChangeTargeting)
    {
        GameObject go = new GameObject("Entity Info", typeof(RectTransform));
        _objects.Add(go);
        EntityInfoPanel panel = go.AddComponent<EntityInfoPanel>();
        TestHelpers.SetPrivateField(panel, "_buttonPrefab", AssetDatabase.LoadAssetAtPath<SandboxButton>("Assets/Prefabs/UI/SandboxButton.prefab"));
        TestHelpers.SetPrivateField(panel, "_canChangeTargeting", canChangeTargeting);
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

    [Test]
    public void WithoutItem_BothListsAreEmpty()
    {
        Assert.IsEmpty(EntityInfoPanel.GetPassiveLines(_entity));
        Assert.IsEmpty(EntityInfoPanel.GetEquippedItemLines(_entity));
    }
}

}
