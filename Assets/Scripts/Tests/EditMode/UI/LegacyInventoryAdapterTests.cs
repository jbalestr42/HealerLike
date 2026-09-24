using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HealerLike.UI.Toolkit.Tests
{
    public class LegacyInventoryAdapterTests
    {
        GameObject _owner;
        GameObject _slotPrefab;
        GameObject _itemPrefab;
        SlotInventory _inventory;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("Inventory adapter test");
            _slotPrefab = new GameObject("Slot prefab", typeof(SlotInventoryItem));
            _itemPrefab = new GameObject("Item prefab", typeof(InventoryItem));
            _inventory = _owner.AddComponent<SlotInventory>();
            _inventory.inventoryHandler = new InventoryHandler();
            var serialized = new SerializedObject(_inventory);
            serialized.FindProperty("_slotInventoryItemPrefab").objectReferenceValue = _slotPrefab;
            serialized.FindProperty("_inventoryItemPrefab").objectReferenceValue = _itemPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_owner);
            Object.DestroyImmediate(_slotPrefab);
            Object.DestroyImmediate(_itemPrefab);
        }

        [Test]
        public void ExplicitEquipmentIndexGrowsSlotsWithoutChangingLegacyCode()
        {
            _inventory.inventoryHandler.AddItem(null, 12, false);
            LegacyInventoryAdapter.Refresh(_inventory);
            Assert.That(_owner.transform.childCount, Is.EqualTo(13));
            Assert.That(_owner.transform.GetChild(12).childCount, Is.EqualTo(1));
            Assert.That(_inventory.inventoryHandler.items[0].inventoryIndex, Is.EqualTo(12));
            for (int i = 0; i < 13; i++)
            {
                var slot = _owner.transform.GetChild(i).GetComponent<SlotInventoryItem>();
                Assert.That(slot.index, Is.EqualTo(i));
                Assert.That(slot.inventoryHandler, Is.SameAs(_inventory.inventoryHandler));
            }
        }

        [Test]
        public void RepeatedRefreshReplacesVisualsAndKeepsVacatedSlotsAvailable()
        {
            _inventory.inventoryHandler.AddItem(null, 2, false);
            LegacyInventoryAdapter.Refresh(_inventory);
            LegacyInventoryAdapter.Refresh(_inventory);
            Assert.That(_owner.transform.GetChild(2).childCount, Is.EqualTo(1));

            _inventory.inventoryHandler.items.Clear();
            LegacyInventoryAdapter.Refresh(_inventory);
            Assert.That(_owner.transform.GetChild(2).childCount, Is.Zero);
            Assert.That(_inventory.GetEmptySlot().index, Is.Zero);
            Assert.That(_owner.transform.childCount, Is.EqualTo(3));
        }

        [Test]
        public void SynchronizeOnlyRefreshesMatchingOwnerIncludingInactiveViews()
        {
            _owner.SetActive(false);
            _inventory.inventoryHandler.AddItem(null, 4, false);
            LegacyInventoryAdapter.Synchronize(new InventoryHandler());
            Assert.That(_owner.transform.childCount, Is.Zero);
            LegacyInventoryAdapter.Synchronize(_inventory.inventoryHandler);
            Assert.That(_owner.transform.childCount, Is.EqualTo(5));
            Assert.That(_owner.transform.GetChild(4).childCount, Is.EqualTo(1));
        }
    }
}
