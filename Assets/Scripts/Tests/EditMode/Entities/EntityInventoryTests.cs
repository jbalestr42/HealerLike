using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Entities
{

// An item of an entity inventory is equipped once, whatever its slot
public class EntityInventoryTests
{
    class CountingItem : AItem
    {
        public int equipCount;
        public override void Equip(GameObject target) { equipCount++; }
        public override void Unequip(GameObject target) { equipCount--; }
        public override string title => "Counting";
        public override string description => "";
        public override Sprite icon => null;
        public override List<GameplayTag> tags => new List<GameplayTag>();
    }

    GameObject _go;
    Entity _entity;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Entity");
        // Adding Entity triggers Entity.Reset() (NREs without a full Init())
        TestHelpers.WithLoggingDisabled(() => _entity = _go.AddComponent<Entity>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    public void OnItemAdded_AnySlot_EquipsTheItemOnce(int inventoryIndex)
    {
        CountingItem item = new CountingItem();

        _entity.OnItemAdded(new InventoryItemData { item = item, inventoryIndex = inventoryIndex }, true);

        Assert.AreEqual(1, item.equipCount);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(5)]
    public void OnItemRemoved_AnySlot_UnequipsTheItem(int inventoryIndex)
    {
        CountingItem item = new CountingItem();
        InventoryItemData itemData = new InventoryItemData { item = item, inventoryIndex = inventoryIndex };
        _entity.OnItemAdded(itemData, true);

        _entity.OnItemRemoved(itemData);

        Assert.AreEqual(0, item.equipCount);
    }
}

}
