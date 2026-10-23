using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Players
{

// The stash of the player takes back the items of a unit (e.g. a dead one)
public class PlayerBehaviourTests
{
    GameObject _playerGo;
    GameObject _unitGo;

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_playerGo);
        TestHelpers.WithLoggingDisabled(() => Object.DestroyImmediate(_unitGo));
    }

    [Test]
    public void TakeItemsOf_TheItemsOfTheUnitGoToTheStash()
    {
        _playerGo = new GameObject("Player");
        PlayerBehaviour player = _playerGo.AddComponent<PlayerBehaviour>();
        _unitGo = new GameObject("Unit");
        _unitGo.SetActive(false);
        Entity unit = null;
        TestHelpers.WithLoggingDisabled(() => unit = _unitGo.AddComponent<Entity>());
        Item ward = new Item { data = new ItemData { name = "Ward", description = "" } };
        unit.inventoryHandler.items.Add(new InventoryItemData { item = ward, inventoryIndex = 2 });

        player.TakeItemsOf(unit);

        Assert.IsEmpty(unit.inventoryHandler.items);
        CollectionAssert.AreEqual(new[] { ward }, player.stash.items.Select(entry => entry.item).ToList());
    }
}

}
