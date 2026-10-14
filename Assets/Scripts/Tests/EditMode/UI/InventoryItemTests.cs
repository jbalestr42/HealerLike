using System.Collections.Generic;
using Entities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{

// The items of an enemy are shown in its inventory, the player can neither take them nor give it any
public class InventoryItemTests
{
    readonly TestUnits _units = new TestUnits();
    GameObject _itemGo;
    GameObject _slotParent;
    GameObject _slotGo;
    InventoryItem _item;
    SlotInventoryItem _slot;
    InventoryHandler _from;
    InventoryHandler _to;

    [SetUp]
    public void SetUp()
    {
        _from = new InventoryHandler();
        _to = new InventoryHandler();
        _from.AddItem(null, 0);

        _slotParent = new GameObject("FromSlot");
        _itemGo = new GameObject("Item", typeof(InventoryItem));
        _itemGo.transform.SetParent(_slotParent.transform);
        _item = _itemGo.GetComponent<InventoryItem>();
        _item.Init(null, _from, null);

        _slotGo = new GameObject("ToSlot", typeof(SlotInventoryItem));
        _slot = _slotGo.GetComponent<SlotInventoryItem>();
        _slot.inventoryHandler = _to;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_slotParent);
        Object.DestroyImmediate(_slotGo);
        _units.DestroyAll();
    }

    PointerEventData Dragging()
    {
        return new PointerEventData(null) { pointerDrag = _itemGo };
    }

    [Test]
    public void EntityType_OnlyThePlayerUnitsHaveAnUnlockedInventory()
    {
        Entity unit = _units.Create(100f, 100f);

        unit.entityType = Entity.EntityType.Computer;
        Assert.IsTrue(unit.inventoryHandler.isLocked, "enemy");
        unit.entityType = Entity.EntityType.Player;
        Assert.IsFalse(unit.inventoryHandler.isLocked, "player");
    }

    [Test]
    public void OnBeginDrag_FromALockedInventory_NoDrag()
    {
        _from.isLocked = true;
        PointerEventData eventData = Dragging();

        _item.OnBeginDrag(eventData);
        _item.OnEndDrag(eventData);

        Assert.IsFalse(_item.canBeMoved);
        Assert.IsNull(eventData.pointerDrag);
        Assert.AreSame(_slotParent.transform, _itemGo.transform.parent, "stays in its slot");
    }

    [Test]
    public void OnDrop_AnItemOfALockedInventory_StaysInIt()
    {
        _from.isLocked = true;

        _slot.OnDrop(Dragging());

        Assert.AreEqual(1, _from.items.Count);
        Assert.IsEmpty(_to.items);
    }

    [Test]
    public void OnDrop_IntoALockedInventory_Refused()
    {
        _to.isLocked = true;

        _slot.OnDrop(Dragging());

        Assert.AreEqual(1, _from.items.Count);
        Assert.IsEmpty(_to.items);
    }

    [Test]
    public void OnDrop_BetweenUnlockedInventories_TheItemMoves()
    {
        _slot.index = 2;

        _slot.OnDrop(Dragging());

        Assert.IsEmpty(_from.items);
        Assert.AreEqual(1, _to.items.Count);
        Assert.AreEqual(2, _to.items[0].inventoryIndex);
    }

    class StubItem : AItem
    {
        public override void Equip(GameObject target) { }
        public override void Unequip(GameObject target) { }
        public override string title => "Stub";
        public override string description => "";
        public override Sprite icon => null;
        public override List<GameplayTag> tags => new List<GameplayTag>();
    }

    // e.g. in the item catalog of the editor, to give it
    [Test]
    public void OnPointerClick_TheInventoryIsTold()
    {
        StubItem stub = new StubItem();
        TestHelpers.SetPrivateField(_item, "_item", stub);
        AItem clicked = null;
        _from.OnItemClicked.AddListener(item => clicked = item);

        _item.OnPointerClick(new PointerEventData(null));

        Assert.AreSame(stub, clicked);
    }

    [Test]
    public void OnPointerClick_WithoutItem_NothingTold()
    {
        bool told = false;
        _from.OnItemClicked.AddListener(item => told = true);

        _item.OnPointerClick(new PointerEventData(null));

        Assert.IsFalse(told);
    }
}

}
