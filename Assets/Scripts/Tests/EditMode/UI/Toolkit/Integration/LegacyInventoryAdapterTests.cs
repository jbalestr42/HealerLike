using NUnit.Framework;
using UnityEngine;

namespace UI.Toolkit.Integration
{

public class LegacyInventoryAdapterTests
{
    GameObject _go;
    GameObject _slotPrefab;
    GameObject _itemPrefab;
    SlotInventory _inventory;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Inventory");
        _slotPrefab = new GameObject("Slot prefab", typeof(SlotInventoryItem));
        _itemPrefab = new GameObject("Item prefab", typeof(InventoryItem));
        _inventory = _go.AddComponent<SlotInventory>();
        _inventory.inventoryHandler = new InventoryHandler();
        TestHelpers.SetPrivateField(_inventory, "_slotInventoryItemPrefab", _slotPrefab);
        TestHelpers.SetPrivateField(_inventory, "_inventoryItemPrefab", _itemPrefab);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
        Object.DestroyImmediate(_slotPrefab);
        Object.DestroyImmediate(_itemPrefab);
    }

    [Test]
    public void Refresh_ItemInSlotTwelve_GrowsToThirteenIndexedSlots()
    {
        _inventory.inventoryHandler.AddItem(null, 12, false);

        LegacyInventoryAdapter.Refresh(_inventory);

        Assert.AreEqual(13, _go.transform.childCount);
        Assert.AreEqual(1, _go.transform.GetChild(12).childCount);
        Assert.AreEqual(12, _inventory.inventoryHandler.items[0].inventoryIndex);
        for (int i = 0; i < 13; i++)
        {
            SlotInventoryItem slot = _go.transform.GetChild(i).GetComponent<SlotInventoryItem>();
            Assert.AreEqual(i, slot.index);
            Assert.AreSame(_inventory.inventoryHandler, slot.inventoryHandler);
        }
    }

    [Test]
    public void Refresh_Repeated_KeepsOneItemVisual()
    {
        _inventory.inventoryHandler.AddItem(null, 2, false);

        LegacyInventoryAdapter.Refresh(_inventory);
        LegacyInventoryAdapter.Refresh(_inventory);

        Assert.AreEqual(1, _go.transform.GetChild(2).childCount);
    }

    [Test]
    public void Refresh_ItemsCleared_FreesVacatedSlots()
    {
        _inventory.inventoryHandler.AddItem(null, 2, false);
        LegacyInventoryAdapter.Refresh(_inventory);

        _inventory.inventoryHandler.items.Clear();
        LegacyInventoryAdapter.Refresh(_inventory);

        Assert.AreEqual(0, _go.transform.GetChild(2).childCount);
        Assert.AreEqual(0, _inventory.GetEmptySlot().index);
        Assert.AreEqual(3, _go.transform.childCount);
    }

    [Test]
    public void Synchronize_OtherOwner_LeavesInventoryUntouched()
    {
        _go.SetActive(false);
        _inventory.inventoryHandler.AddItem(null, 4, false);

        LegacyInventoryAdapter.Synchronize(new InventoryHandler());

        Assert.AreEqual(0, _go.transform.childCount);
    }

    [Test]
    public void Synchronize_InactiveMatchingOwner_RefreshesSlots()
    {
        _go.SetActive(false);
        _inventory.inventoryHandler.AddItem(null, 4, false);

        LegacyInventoryAdapter.Synchronize(_inventory.inventoryHandler);

        Assert.AreEqual(5, _go.transform.childCount);
        Assert.AreEqual(1, _go.transform.GetChild(4).childCount);
    }
}
}
