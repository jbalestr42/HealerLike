using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI
{

// Editor tool: every item of the game in an inventory, to try them in game. An item taken out is replaced by a new
// one in its slot, the catalog never runs out
public class ItemCatalogTests
{
    readonly List<Object> _created = new List<Object>();
    ItemFactory _sword;
    ItemFactory _shield;

    [SetUp]
    public void SetUp()
    {
        _sword = CreateItem("Sword");
        _shield = CreateItem("Shield");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
    }

    ItemFactory CreateItem(string name)
    {
        ItemFactory factory = ScriptableObject.CreateInstance<ItemFactory>();
        factory.data = new ItemData { name = name };
        _created.Add(factory);
        return factory;
    }

    [Test]
    public void New_AnItemOfEachFactory_EachInItsSlot()
    {
        ItemCatalog catalog = new ItemCatalog(new List<AItemFactory> { _sword, null, _shield });

        List<InventoryItemData> items = catalog.inventoryHandler.items;
        Assert.AreEqual(2, items.Count);
        Assert.AreEqual("Sword", items[0].item.title);
        Assert.AreEqual(0, items[0].inventoryIndex);
        Assert.AreEqual("Shield", items[1].item.title);
        Assert.AreEqual(1, items[1].inventoryIndex);
    }

    // As dragged onto a unit
    [Test]
    public void ItemTakenOut_ANewOneInItsSlot()
    {
        ItemCatalog catalog = new ItemCatalog(new List<AItemFactory> { _sword, _shield });
        AItem sword = catalog.inventoryHandler.items[0].item;
        InventoryHandler unit = new InventoryHandler();

        unit.TransfertItem(sword, 0, catalog.inventoryHandler);

        Assert.AreSame(sword, unit.items[0].item);
        InventoryItemData replacement = catalog.inventoryHandler.items.Find(itemData => itemData.item.title == "Sword");
        Assert.IsNotNull(replacement);
        Assert.AreNotSame(sword, replacement.item);
        Assert.AreEqual(0, replacement.inventoryIndex);
        Assert.AreEqual(2, catalog.inventoryHandler.items.Count);
    }

    // The replacement is taken out in turn
    [Test]
    public void ItemTakenOutTwice_StillThere()
    {
        ItemCatalog catalog = new ItemCatalog(new List<AItemFactory> { _sword });
        InventoryHandler unit = new InventoryHandler();

        unit.TransfertItem(catalog.inventoryHandler.items[0].item, 0, catalog.inventoryHandler);
        unit.TransfertItem(catalog.inventoryHandler.items[0].item, 1, catalog.inventoryHandler);

        Assert.AreEqual(2, unit.items.Count);
        Assert.AreEqual(1, catalog.inventoryHandler.items.Count);
        Assert.AreEqual("Sword", catalog.inventoryHandler.items[0].item.title);
    }

    // As clicked to give it
    [Test]
    public void Copy_ANewItemLikeIt_TheCatalogKeepsItsOwn()
    {
        ItemCatalog catalog = new ItemCatalog(new List<AItemFactory> { _sword });
        AItem sword = catalog.inventoryHandler.items[0].item;

        AItem copy = catalog.Copy(sword);

        Assert.IsNotNull(copy);
        Assert.AreNotSame(sword, copy);
        Assert.AreEqual("Sword", copy.title);
        Assert.AreSame(sword, catalog.inventoryHandler.items[0].item);
    }

    [Test]
    public void Copy_ItemNotInTheCatalog_None()
    {
        ItemCatalog catalog = new ItemCatalog(new List<AItemFactory> { _sword });

        Assert.IsNull(catalog.Copy(_shield.GetItem()));
        Assert.IsNull(catalog.Copy(null));
    }

    [Test]
    public void Locked_ItsInventoryIsLocked()
    {
        Assert.IsTrue(new ItemCatalog(new List<AItemFactory> { _sword }, isLocked: true).inventoryHandler.isLocked);
        Assert.IsFalse(new ItemCatalog(new List<AItemFactory> { _sword }).inventoryHandler.isLocked);
    }
}

}
