using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UI
{

// Editor tool: the items of the units and the ones of the player in two catalogs, a click giving a new item like the
// one clicked (the UI copies of the inventory of the player are not made here)
public class ItemCatalogViewTests
{
    readonly List<Object> _created = new List<Object>();
    ItemCatalogView _view;
    readonly List<AItem> _unitItemsGiven = new List<AItem>();
    readonly List<AItem> _playerItemsGiven = new List<AItem>();

    [SetUp]
    public void SetUp()
    {
        GameplayTag playerTag = ScriptableObject.CreateInstance<GameplayTag>();
        playerTag.name = TagNames.Player;
        _created.Add(playerTag);

        List<AItemFactory> items = new List<AItemFactory> { CreateItem("Sword"), CreateItem("Rosary", playerTag), CreateItem("Shield") };
        GameObject go = new GameObject("Catalog");
        _created.Add(go);
        _view = go.AddComponent<ItemCatalogView>();
        _view.Init(null, items, _unitItemsGiven.Add, _playerItemsGiven.Add);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object created in _created)
        {
            Object.DestroyImmediate(created);
        }
        _created.Clear();
        _unitItemsGiven.Clear();
        _playerItemsGiven.Clear();
    }

    ItemFactory CreateItem(string name, params GameplayTag[] tags)
    {
        ItemFactory factory = ScriptableObject.CreateInstance<ItemFactory>();
        factory.data = new ItemData { name = name, tags = new List<GameplayTag>(tags) };
        _created.Add(factory);
        return factory;
    }

    static List<string> Titles(ItemCatalog catalog)
    {
        return catalog.inventoryHandler.items.ConvertAll(itemData => itemData.item.title);
    }

    [Test]
    public void Init_TheItemsOfThePlayerApart()
    {
        CollectionAssert.AreEqual(new[] { "Sword", "Shield" }, Titles(_view.unitItems));
        CollectionAssert.AreEqual(new[] { "Rosary" }, Titles(_view.playerItems));
    }

    // Never dragged onto a unit
    [Test]
    public void Init_TheItemsOfThePlayerLocked()
    {
        Assert.IsTrue(_view.playerItems.inventoryHandler.isLocked);
        Assert.IsFalse(_view.unitItems.inventoryHandler.isLocked);
    }

    [Test]
    public void UnitItemClicked_ANewOneGivenAsAUnitItem()
    {
        AItem sword = _view.unitItems.inventoryHandler.items[0].item;

        _view.unitItems.inventoryHandler.OnItemClicked.Invoke(sword);

        Assert.AreEqual(1, _unitItemsGiven.Count);
        Assert.AreNotSame(sword, _unitItemsGiven[0]);
        Assert.AreEqual("Sword", _unitItemsGiven[0].title);
        CollectionAssert.IsEmpty(_playerItemsGiven);
    }

    [Test]
    public void PlayerItemClicked_ANewOneGivenToTheCharacter()
    {
        AItem rosary = _view.playerItems.inventoryHandler.items[0].item;

        _view.playerItems.inventoryHandler.OnItemClicked.Invoke(rosary);

        Assert.AreEqual(1, _playerItemsGiven.Count);
        Assert.AreNotSame(rosary, _playerItemsGiven[0]);
        Assert.AreEqual("Rosary", _playerItemsGiven[0].title);
        CollectionAssert.IsEmpty(_unitItemsGiven);
    }

    // The item of each slot takes the cell of the catalog, smaller than the one of the inventory of the player
    [Test]
    public void FitSlots_TheItemOfEachSlotTakesTheCell()
    {
        GameObject inventory = new GameObject("Inventory");
        _created.Add(inventory);
        UnityEngine.UI.GridLayoutGroup[] slotGrids = new UnityEngine.UI.GridLayoutGroup[2];
        for (int i = 0; i < slotGrids.Length; i++)
        {
            GameObject slot = new GameObject("Slot", typeof(RectTransform));
            slot.transform.SetParent(inventory.transform);
            slotGrids[i] = slot.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            slotGrids[i].cellSize = new Vector2(75f, 75f);
        }

        ItemCatalogView.FitSlots(inventory.transform, new Vector2(52f, 52f));

        foreach (UnityEngine.UI.GridLayoutGroup slotGrid in slotGrids)
        {
            Assert.AreEqual(new Vector2(52f, 52f), slotGrid.cellSize);
        }
    }
}

}
