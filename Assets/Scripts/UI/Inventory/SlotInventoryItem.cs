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

    public void OnDrop(PointerEventData eventData)
    {
        if (IsEmpty())
        {
            GameObject go = eventData.pointerDrag;
            InventoryItem inventoryItem = go.GetComponent<InventoryItem>();
            inventoryItem.ChangeInventory(transform, inventoryHandler, index);
        }
    }

    public bool IsEmpty()
    {
        return transform.childCount == 0;
    }

    #endregion
}