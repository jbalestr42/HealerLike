using UnityEngine;
using UnityEngine.EventSystems;

public class SlotInventoryItem : MonoBehaviour, IDropHandler
{
    [SerializeField] int _index;
    public int index { get { return _index; } set { _index = value; } }

    InventoryHandler _inventoryHandler;
    public InventoryHandler inventoryHandler { get { return _inventoryHandler; } set { _inventoryHandler = value; } }

    InventoryItem _inventoryItem;
    public InventoryItem inventoryItem { get { return _inventoryItem; } set { _inventoryItem = value; } } 

    #region IDropHandler

    // Never into a locked inventory (e.g. the one of an enemy), nor an item that can't be moved
    public void OnDrop(PointerEventData eventData)
    {
        if (!IsEmpty() || (inventoryHandler != null && inventoryHandler.isLocked))
        {
            return;
        }
        GameObject go = eventData.pointerDrag;
        InventoryItem inventoryItem = go != null ? go.GetComponent<InventoryItem>() : null;
        if (inventoryItem != null && inventoryItem.canBeMoved)
        {
            inventoryItem.ChangeInventory(transform, inventoryHandler, index);
        }
    }

    public bool IsEmpty()
    {
        return transform.childCount == 0;
    }

    #endregion
}