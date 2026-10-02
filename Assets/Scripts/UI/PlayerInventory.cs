using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] SlotInventory _inventory;
    public SlotInventory inventory { get { return _inventory; } set { _inventory = value; } }

    InventoryHandler _inventoryHandler = new InventoryHandler();

    void Awake()
    {
        _inventory.inventoryHandler = _inventoryHandler;
        _inventory.Init();
    }

    public bool IsInventoryVisible()
    {
        return _inventory.gameObject.activeInHierarchy;
    }

    public void ShowInventory()
    {
        _inventory.gameObject.SetActive(true);
    }

    public void HideInventory()
    {
        _inventory.gameObject.SetActive(false);
    }

    public void AddItem(AItem item)
    {
        _inventoryHandler.AddItem(item, -1);
    }

    public void RemoveItem(AItem item)
    {
        _inventoryHandler.RemoveItem(item);
    }

    // The items added to the unit (not its innate ones) come back to the player
    public void TakeItemsOf(Entity entity)
    {
        _inventoryHandler.TakeAllItems(entity.inventoryHandler);
    }
}