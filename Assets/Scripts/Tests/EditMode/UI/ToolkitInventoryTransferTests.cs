using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HealerLike.UI.Toolkit.Tests
{
    public class ToolkitInventoryTransferTests
    {
        sealed class Item : AItem
        {
            public override string title => "Equipment";
            public override Sprite icon => null;
            public override List<GameplayTag> tags => new List<GameplayTag>();
            public int Equipped;
            public int Unequipped;
            public override void Equip(GameObject target) { Equipped++; }
            public override void Unequip(GameObject target) { Unequipped++; }
        }

        [Test]
        public void FirstEmptySlotPreservesOccupiedPrioritySlots()
        {
            var inventory = new InventoryHandler();
            inventory.AddItem(new Item(), 0, false);
            inventory.AddItem(new Item(), 2, false);
            Assert.That(ToolkitInventoryTransfer.FirstEmptySlot(inventory), Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { 1 }, ToolkitInventoryTransfer.EmptySlots(inventory));
        }

        [Test]
        public void TransferPreservesExplicitStackingSlotAndNotifiesBothOwners()
        {
            var source = new InventoryHandler();
            var destination = new InventoryHandler();
            var item = new Item();
            source.AddItem(item, 0, false);
            int removed = 0;
            int added = 0;
            source.OnItemRemoved.AddListener(_ => removed++);
            destination.OnItemAdded.AddListener((entry, isNew) => { added++; Assert.That(isNew, Is.False); });
            Assert.That(ToolkitInventoryTransfer.Transfer(item, source, destination, 2), Is.True);
            Assert.That(source.items, Is.Empty);
            Assert.That(destination.items[0].inventoryIndex, Is.EqualTo(2));
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(added, Is.EqualTo(1));
        }

        [Test]
        public void InvalidOrOccupiedDestinationDoesNotRemoveSourceItem()
        {
            var source = new InventoryHandler();
            var destination = new InventoryHandler();
            var item = new Item();
            source.AddItem(item, 0, false);
            destination.AddItem(new Item(), 0, false);
            Assert.That(ToolkitInventoryTransfer.Transfer(item, source, destination, 0), Is.False);
            Assert.That(ToolkitInventoryTransfer.Transfer(item, source, destination, -1), Is.False);
            Assert.That(source.items.Count, Is.EqualTo(1));
        }

        [TestCase(0, 3)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        public void TransferAppliesAndReversesTheRealEntitySlotStrength(int slot, int expectedStacks)
        {
            var owner = new GameObject("Equipment owner");
            try
            {
                Entity entity = null;
                // Entity.Reset is an Editor message and expects its normal gameplay Init chain.
                TestHelpers.WithLoggingDisabled(() => entity = owner.AddComponent<Entity>());
                var stash = new InventoryHandler();
                var inventory = entity.inventoryHandler;
                inventory.OnItemAdded.AddListener(entity.OnItemAdded);
                inventory.OnItemRemoved.AddListener(entity.OnItemRemoved);
                var item = new Item();
                stash.AddItem(item, 0, false);

                Assert.That(ToolkitInventoryTransfer.Transfer(item, stash, inventory, slot), Is.True);
                Assert.That(item.Equipped, Is.EqualTo(expectedStacks));
                Assert.That(item.Unequipped, Is.Zero);
                Assert.That(ToolkitInventoryTransfer.Transfer(item, inventory, stash, 0), Is.True);
                Assert.That(item.Unequipped, Is.EqualTo(expectedStacks));
                Assert.That(item.Equipped - item.Unequipped, Is.Zero);
                Assert.That(inventory.items, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ReturningToStashThenReequippingKeepsExactlyOneOwner()
        {
            var stash = new InventoryHandler();
            var entity = new InventoryHandler();
            var item = new Item();
            entity.AddItem(item, 1, false);
            Assert.That(ToolkitInventoryTransfer.Transfer(item, entity, stash, 0), Is.True);
            Assert.That(ToolkitInventoryTransfer.Transfer(item, stash, entity, 0), Is.True);
            Assert.That(stash.items, Is.Empty);
            Assert.That(entity.items.Count, Is.EqualTo(1));
        }
    }
}
