using NUnit.Framework;
using UnityEngine;

namespace UI
{

// The slots of the inventory panel of an entity, the items without title or icon to show (none needed here)
public class SlotInventoryTests
{
    const int SlotCount = 3;

    GameObject _slotPrefab;
    GameObject _itemPrefab;
    GameObject _go;
    SlotInventory _inventory;
    InventoryHandler _handler;

    // Never started, like the panel shown for the first time
    [SetUp]
    public void SetUp()
    {
        _slotPrefab = new GameObject("SlotPrefab", typeof(SlotInventoryItem));
        _itemPrefab = new GameObject("ItemPrefab", typeof(InventoryItem));
        _go = new GameObject("SlotInventory");
        _inventory = _go.AddComponent<SlotInventory>();
        TestHelpers.SetPrivateField(_inventory, "_slotInventoryItemPrefab", _slotPrefab);
        TestHelpers.SetPrivateField(_inventory, "_inventoryItemPrefab", _itemPrefab);
        TestHelpers.SetPrivateField(_inventory, "_slotCount", SlotCount);
        _handler = new InventoryHandler();
        _inventory.inventoryHandler = _handler;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_slotPrefab);
        Object.DestroyImmediate(_itemPrefab);
    }

    SlotInventoryItem GetSlot(int index)
    {
        return _go.transform.GetChild(index).GetComponent<SlotInventoryItem>();
    }

    [Test]
    public void RefreshInventory_BeforeStart_ShowsEachItemInItsOwnSlot()
    {
        _handler.items.Add(new InventoryItemData { item = null, inventoryIndex = -1 });
        _handler.items.Add(new InventoryItemData { item = null, inventoryIndex = -1 });

        _inventory.RefreshInventory();

        Assert.AreEqual(SlotCount, _go.transform.childCount);
        Assert.AreEqual(0, _handler.items[0].inventoryIndex);
        Assert.AreEqual(1, _handler.items[1].inventoryIndex);
        Assert.AreEqual(1, GetSlot(0).transform.childCount);
        Assert.AreEqual(1, GetSlot(1).transform.childCount);
        Assert.AreEqual(0, GetSlot(2).transform.childCount);
    }

    [Test]
    public void RefreshInventory_AnItemWithoutSlot_TakesTheFirstFreeOne_NotTheSlotOfAnother()
    {
        _handler.items.Add(new InventoryItemData { item = null, inventoryIndex = -1 });
        _handler.items.Add(new InventoryItemData { item = null, inventoryIndex = 0 });

        _inventory.RefreshInventory();

        Assert.AreEqual(1, _handler.items[0].inventoryIndex);
        Assert.AreEqual(1, GetSlot(0).transform.childCount);
        Assert.AreEqual(1, GetSlot(1).transform.childCount);
    }

    [Test]
    public void RefreshInventory_MoreItemsThanSlots_AddsSlots()
    {
        for (int i = 0; i < SlotCount + 2; i++)
        {
            _handler.items.Add(new InventoryItemData { item = null, inventoryIndex = -1 });
        }

        _inventory.RefreshInventory();

        Assert.AreEqual(SlotCount + 2, _go.transform.childCount);
        for (int i = 0; i < SlotCount + 2; i++)
        {
            Assert.AreEqual(i, GetSlot(i).index);
            Assert.AreEqual(1, GetSlot(i).transform.childCount, $"slot {i}");
        }
    }
}

}
