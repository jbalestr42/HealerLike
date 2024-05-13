using System.Collections.Generic;
using UnityEngine;

public class SlotInventory : MonoBehaviour
{
    [SerializeField] GameObject _slotInventoryItemPrefab;
    [SerializeField] GameObject _inventoryItemPrefab;
    [SerializeField] Transform _root;
    [SerializeField] int _slotCount = 10;

    InventoryHandler _inventoryHandler;
    public InventoryHandler inventoryHandler { get { return _inventoryHandler; } set { _inventoryHandler = value; } }

    List<SlotInventoryItem> _slotItems = new List<SlotInventoryItem>();

    bool _isInit = false;

    void Start()
    {
        Init();
    }

    public void Init()
    {
        if (_isInit)
        {
            return;
        }

        _isInit = true;
        for (int i = 0; i < _slotCount; i++)
        {
            CreateSlotInventoryItem(i);
        }

        _inventoryHandler.OnItemAdded.AddListener(OnItemAdded);
        _inventoryHandler.OnItemRemoved.AddListener(OnItemRemoved);
    }

    SlotInventoryItem CreateSlotInventoryItem(int index)
    {
        GameObject slotIventoryItemGo = Instantiate(_slotInventoryItemPrefab, transform);
        SlotInventoryItem slotInventoryItem = slotIventoryItemGo.GetComponent<SlotInventoryItem>();
        slotInventoryItem.inventoryHandler = inventoryHandler;
        slotInventoryItem.index = index;
        _slotItems.Add(slotInventoryItem);
        return slotInventoryItem;
    }

    public SlotInventoryItem GetEmptySlot()
    {
        for (int i = 0; i < _slotItems.Count; i++)
        {
            if (_slotItems[i].IsEmpty())
            {
                return _slotItems[i];
            }
        }

        return CreateSlotInventoryItem(_slotItems.Count);
    }

    public void OnItemAdded(InventoryItemData itemData, bool isNewItem)
    {
        if (isNewItem)
        {
            SlotInventoryItem slotInventoryItem = GetEmptySlot();
            itemData.inventoryIndex = slotInventoryItem.index;

            GameObject go = Instantiate(_inventoryItemPrefab, _slotItems[itemData.inventoryIndex].transform);
            InventoryItem inventoryItem = go.GetComponent<InventoryItem>();
            inventoryItem.Init(_root, _inventoryHandler, itemData.item);
        }
    }

    public void OnItemRemoved(InventoryItemData item)
    {
    }

    public void RefreshInventory()
    {
        _slotItems.Clear();
        foreach (Transform child in transform)
        {
            SlotInventoryItem slotItem = child.GetComponent<SlotInventoryItem>();
            slotItem.inventoryHandler = inventoryHandler;

            // Clean current slot
            foreach (Transform slotItemChild in slotItem.transform)
            {
                Destroy(slotItemChild.gameObject);
            }
            _slotItems.Add(slotItem);
        }

        // Fill slots with items
        for (int i = 0; i < _inventoryHandler.items.Count; i++)
        {
            InventoryItemData itemData = _inventoryHandler.items[i];

            if (!itemData.isValidIndex)
            {
                itemData.inventoryIndex = i;
            }
            GameObject go = Instantiate(_inventoryItemPrefab, _slotItems[itemData.inventoryIndex].transform);
            InventoryItem inventoryItem = go.GetComponent<InventoryItem>();
            inventoryItem.Init(_root, _inventoryHandler, _inventoryHandler.items[i].item);
        }
    }
}