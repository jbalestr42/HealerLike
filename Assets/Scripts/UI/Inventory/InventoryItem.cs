using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] Text _descriptionText;
    [SerializeField] Image _background;
    [SerializeField] Image _icon;

    Transform _destinationTransform;
    InventoryHandler _inventoryHandler;
    Transform _root;
    AItem _item;

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

    #region IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler

    public void OnBeginDrag(PointerEventData eventData)
    {
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

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.instance.GetView<GameView>(ViewType.Game).itemDescription.ShowItem(_item);
    }

    #endregion
}
