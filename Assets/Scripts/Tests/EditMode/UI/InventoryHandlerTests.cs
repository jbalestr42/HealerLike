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
}

}
