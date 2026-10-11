using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// An item in an inventory slot, dragged to another slot; its name and description shown in the tooltip on hover
public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] TMPro.TMP_Text _descriptionText;
    [SerializeField] Image _background;
    [SerializeField] Image _icon;

    Transform _destinationTransform;
    InventoryHandler _inventoryHandler;
    Transform _root;
    AItem _item;
    bool _isToolTipShown;

    public void Init(Transform root, InventoryHandler inventoryHandler, AItem item)
    {
        _root = root;
        _inventoryHandler = inventoryHandler;
        _item = item;

        if (item != null)
        {
            _descriptionText.text = item.title;
            _icon.sprite = item.icon;
        }
    }

    public void ChangeInventory(Transform destinationTransform, InventoryHandler inventoryHandler, int inventoryIndex)
    {
        _destinationTransform = destinationTransform;
        inventoryHandler.TransfertItem(_item, inventoryIndex, _inventoryHandler);
        _inventoryHandler = inventoryHandler;
    }

    void ShowToolTip(bool show)
    {
        _isToolTipShown = show;
        ToolTip toolTip = UIManager.instance.GetView<GameView>(ViewType.Game).toolTip;
        if (show)
        {
            toolTip.SetText(PlayerItemIcon.GetToolTipText(_item));
            toolTip.ShowBelow((RectTransform)transform);
        }
        else
        {
            toolTip.Show(false);
        }
    }

    void OnDisable()
    {
        // Removed while hovered (e.g. the inventory closed): the pointer never exits it
        if (_isToolTipShown)
        {
            ShowToolTip(false);
        }
    }

    #region IBeginDragHandler, IDragHandler, IEndDragHandler

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isToolTipShown)
        {
            ShowToolTip(false);
        }
        _destinationTransform = transform.parent;

        // Change parent to display on top of all other UI component
        transform.SetParent(_root);
        transform.SetAsLastSibling();

        _background.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(_destinationTransform);
        _background.raycastTarget = true;
    }

    #endregion

    #region IPointerEnterHandler, IPointerExitHandler

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_item != null && !eventData.dragging)
        {
            ShowToolTip(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isToolTipShown)
        {
            ShowToolTip(false);
        }
    }

    #endregion
}
