using UnityEngine;
using UnityEngine.EventSystems;

public class Inventory : MonoBehaviour, IDropHandler
{
    [SerializeField] GameObject _inventoryItemPrefab;
    [SerializeField] Transform _root;

    InventoryHandler _inventoryHandler;
    public InventoryHandler inventoryHandler { get { return _inventoryHandler; } set { _inventoryHandler = value; } }

    public void RefreshInventory()
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        foreach (InventoryItemData itemData in _inventoryHandler.items)
        {
            GameObject go = Instantiate(_inventoryItemPrefab);
            InventoryItem inventoryItem = go.GetComponent<InventoryItem>();
            inventoryItem.Init(_root, _inventoryHandler, itemData.item);
            inventoryItem.transform.SetParent(transform);
        }
    }

    #region IDropHandler

    public void OnDrop(PointerEventData eventData)
    {
        GameObject go = eventData.pointerDrag;
        InventoryItem inventoryItem = go.GetComponent<InventoryItem>();
        inventoryItem.ChangeInventory(transform, _inventoryHandler, 0);
    }

    #endregion
}