using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit
{

public class ToolkitInventoryTransferTests
{
    class FakeItem : AItem
    {
        public int equipCount = 0;
        public int unequipCount = 0;
        public override string title
        {
            get { return "Equipment"; }
        }

        public override Sprite icon
        {
            get { return null; }
        }

        public override List<GameplayTag> tags
        {
            get { return new List<GameplayTag>(); }
        }

        public override void Equip(GameObject target)
        {
            equipCount++;
        }

        public override void Unequip(GameObject target)
        {
            unequipCount++;
        }
    }

    GameObject _go;
    Entity _entity;
    int _addedCount;
    int _removedCount;
    bool _wasNewItem;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Equipment owner");
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() =>
        {
            _entity = _go.AddComponent<Entity>();
        });
        _entity.inventoryHandler.OnItemAdded.AddListener(_entity.OnItemAdded);
        _entity.inventoryHandler.OnItemRemoved.AddListener(_entity.OnItemRemoved);
        _addedCount = 0;
        _removedCount = 0;
        _wasNewItem = true;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    void OnItemAdded(InventoryItemData item, bool isNewItem)
    {
        _addedCount++;
        _wasNewItem = isNewItem;
    }

    void OnItemRemoved(InventoryItemData item)
    {
        _removedCount++;
    }

    [Test]
    public void FirstEmptySlot_FirstAndThirdOccupied_ReturnsSecond()
    {
        InventoryHandler inventory = new InventoryHandler();
        inventory.AddItem(new FakeItem(), 0, false);
        inventory.AddItem(new FakeItem(), 2, false);
        int slot = ToolkitInventoryTransfer.FirstEmptySlot(inventory);
        Assert.AreEqual(1, slot);
    }

    [Test]
    public void EmptySlots_FirstAndThirdOccupied_ReturnsSecondOnly()
    {
        InventoryHandler inventory = new InventoryHandler();
        inventory.AddItem(new FakeItem(), 0, false);
        inventory.AddItem(new FakeItem(), 2, false);
        List<int> slots = ToolkitInventoryTransfer.EmptySlots(inventory);
        CollectionAssert.AreEqual(new int[] { 1 }, slots);
    }

    [Test]
    public void Contains_ItemInInventory_ReturnsTrue()
    {
        InventoryHandler inventory = new InventoryHandler();
        FakeItem item = new FakeItem();
        inventory.AddItem(item, 0, false);
        Assert.IsTrue(ToolkitInventoryTransfer.Contains(inventory, item));
        Assert.IsFalse(ToolkitInventoryTransfer.Contains(inventory, new FakeItem()));
    }

    [Test]
    public void IsOccupied_SlotWithItem_ReturnsTrue()
    {
        InventoryHandler inventory = new InventoryHandler();
        inventory.AddItem(new FakeItem(), 1, false);
        Assert.IsTrue(ToolkitInventoryTransfer.IsOccupied(inventory, 1));
        Assert.IsFalse(ToolkitInventoryTransfer.IsOccupied(inventory, 0));
    }

    [Test]
    public void Transfer_ExplicitSlot_KeepsSlotAndNotifiesBothOwners()
    {
        InventoryHandler source = new InventoryHandler();
        InventoryHandler destination = new InventoryHandler();
        FakeItem item = new FakeItem();
        source.AddItem(item, 0, false);
        source.OnItemRemoved.AddListener(OnItemRemoved);
        destination.OnItemAdded.AddListener(OnItemAdded);
        bool isMoved = ToolkitInventoryTransfer.Transfer(item, source, destination, 2);
        Assert.IsTrue(isMoved);
        Assert.IsEmpty(source.items);
        Assert.AreEqual(2, destination.items[0].inventoryIndex);
        Assert.AreEqual(1, _removedCount);
        Assert.AreEqual(1, _addedCount);
        Assert.IsFalse(_wasNewItem); // a move, not a new reward
    }

    [Test]
    public void Transfer_OccupiedDestinationSlot_KeepsSourceItem()
    {
        InventoryHandler source = new InventoryHandler();
        InventoryHandler destination = new InventoryHandler();
        FakeItem item = new FakeItem();
        source.AddItem(item, 0, false);
        destination.AddItem(new FakeItem(), 0, false);
        bool isMoved = ToolkitInventoryTransfer.Transfer(item, source, destination, 0);
        Assert.IsFalse(isMoved);
        Assert.AreEqual(1, source.items.Count);
    }

    [Test]
    public void Transfer_NegativeSlot_KeepsSourceItem()
    {
        InventoryHandler source = new InventoryHandler();
        FakeItem item = new FakeItem();
        source.AddItem(item, 0, false);
        bool isMoved = ToolkitInventoryTransfer.Transfer(item, source, new InventoryHandler(), -1);
        Assert.IsFalse(isMoved);
        Assert.AreEqual(1, source.items.Count);
    }

    [TestCase(0, 3)]
    [TestCase(1, 2)]
    [TestCase(2, 1)]
    public void Transfer_ToEntitySlot_EquipsWithSlotStrength(int slot, int expectedStacks)
    {
        InventoryHandler stash = new InventoryHandler();
        FakeItem item = new FakeItem();
        stash.AddItem(item, 0, false);
        bool isMoved = ToolkitInventoryTransfer.Transfer(item, stash, _entity.inventoryHandler, slot);
        Assert.IsTrue(isMoved);
        Assert.AreEqual(expectedStacks, item.equipCount);
        Assert.AreEqual(0, item.unequipCount);
    }

    [TestCase(0, 3)]
    [TestCase(1, 2)]
    [TestCase(2, 1)]
    public void Transfer_BackToStash_UnequipsWithSlotStrength(int slot, int expectedStacks)
    {
        InventoryHandler stash = new InventoryHandler();
        FakeItem item = new FakeItem();
        stash.AddItem(item, 0, false);
        ToolkitInventoryTransfer.Transfer(item, stash, _entity.inventoryHandler, slot);
        bool isMoved = ToolkitInventoryTransfer.Transfer(item, _entity.inventoryHandler, stash, 0);
        Assert.IsTrue(isMoved);
        Assert.AreEqual(expectedStacks, item.unequipCount);
        Assert.AreEqual(0, item.equipCount - item.unequipCount);
        Assert.IsEmpty(_entity.inventoryHandler.items);
    }

    [Test]
    public void Transfer_ReturnThenReequip_KeepsOneOwner()
    {
        InventoryHandler stash = new InventoryHandler();
        InventoryHandler inventory = new InventoryHandler();
        FakeItem item = new FakeItem();
        inventory.AddItem(item, 1, false);
        ToolkitInventoryTransfer.Transfer(item, inventory, stash, 0);
        ToolkitInventoryTransfer.Transfer(item, stash, inventory, 0);
        Assert.IsEmpty(stash.items);
        Assert.AreEqual(1, inventory.items.Count);
    }
}
}
