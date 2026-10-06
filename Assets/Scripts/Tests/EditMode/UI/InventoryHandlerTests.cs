using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI
{

public class InventoryHandlerTests
{
    class StubItem : AItem
    {
        public override void Equip(GameObject target) { }
        public override void Unequip(GameObject target) { }
        public override string title => "Stub";
        public override string description => "";
        public override Sprite icon => null;
        public override List<GameplayTag> tags => new List<GameplayTag>();
    }

    [Test]
    public void GetFirstFreeIndex_EmptyInventory_ReturnsZero()
    {
        InventoryHandler inventoryHandler = new InventoryHandler();

        Assert.AreEqual(0, inventoryHandler.GetFirstFreeIndex());
    }

    [Test]
    public void GetFirstFreeIndex_ReturnsTheLowestUnusedIndex()
    {
        InventoryHandler inventoryHandler = new InventoryHandler();
        inventoryHandler.AddItem(new StubItem(), 0);
        inventoryHandler.AddItem(new StubItem(), 2);

        Assert.AreEqual(1, inventoryHandler.GetFirstFreeIndex());
    }

    [Test]
    public void GetFirstFreeIndex_IgnoresItemsWithoutIndex()
    {
        InventoryHandler inventoryHandler = new InventoryHandler();
        inventoryHandler.AddItem(new StubItem(), -1);
        inventoryHandler.AddItem(new StubItem(), 0);

        Assert.AreEqual(1, inventoryHandler.GetFirstFreeIndex());
    }

    [Test]
    public void GetFirstFreeIndex_ReusesTheIndexOfARemovedItem()
    {
        InventoryHandler inventoryHandler = new InventoryHandler();
        StubItem removedItem = new StubItem();
        inventoryHandler.AddItem(removedItem, 0);
        inventoryHandler.AddItem(new StubItem(), 1);
        inventoryHandler.RemoveItem(removedItem);

        Assert.AreEqual(0, inventoryHandler.GetFirstFreeIndex());
    }

    [Test]
    public void TakeAllItems_MovesEveryItemAndEmptiesTheOtherInventory()
    {
        // e.g. the items of a dead unit going back to the player
        InventoryHandler unit = new InventoryHandler();
        InventoryHandler player = new InventoryHandler();
        StubItem first = new StubItem();
        StubItem second = new StubItem();
        StubItem alreadyThere = new StubItem();
        unit.AddItem(first, 0);
        unit.AddItem(second, 1);
        player.AddItem(alreadyThere, 0);

        player.TakeAllItems(unit);

        CollectionAssert.IsEmpty(unit.items);
        CollectionAssert.AreEquivalent(new[] { alreadyThere, first, second }, player.items.ConvertAll(itemData => itemData.item));
    }

    [Test]
    public void TakeAllItems_UnequipsFromTheOtherInventoryAndAddsAsNewItemsWithoutSlot()
    {
        InventoryHandler unit = new InventoryHandler();
        InventoryHandler player = new InventoryHandler();
        StubItem item = new StubItem();
        unit.AddItem(item, 3);
        List<AItem> removed = new List<AItem>();
        List<InventoryItemData> added = new List<InventoryItemData>();
        List<bool> addedAsNew = new List<bool>();
        unit.OnItemRemoved.AddListener(itemData => removed.Add(itemData.item));
        player.OnItemAdded.AddListener((itemData, isNewItem) =>
        {
            added.Add(itemData);
            addedAsNew.Add(isNewItem);
        });

        player.TakeAllItems(unit);

        // The unit's OnItemRemoved unequips it, the player's inventory picks its own slot
        CollectionAssert.AreEqual(new[] { item }, removed);
        Assert.AreEqual(1, added.Count);
        Assert.AreSame(item, added[0].item);
        Assert.AreEqual(-1, added[0].inventoryIndex);
        Assert.IsTrue(addedAsNew[0]);
    }

    [Test]
    public void TakeAllItems_EmptyInventory_AddsNothing()
    {
        InventoryHandler player = new InventoryHandler();
        player.AddItem(new StubItem(), 0);

        player.TakeAllItems(new InventoryHandler());

        Assert.AreEqual(1, player.items.Count);
    }
}

}
