using UnityEngine;
using UnityEngine.UI;

public class InventoryItemDescription : MonoBehaviour
{
    [SerializeField] GameObject _container;
    [SerializeField] Text _descriptionText;
    [SerializeField] Image _icon;

    public void ShowItem(AItem item)
    {
        _container.SetActive(item != null);
        if (item != null)
        {
            _descriptionText.text = item.title;
            _icon.sprite = item.icon;
        }
    }
}
