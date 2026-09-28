using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Icon of an item of the player, its name and description shown in the tooltip on hover
public class PlayerItemIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image _icon;

    AItem _item;
    public AItem item => _item;

    bool _isToolTipShown;

    public void Init(AItem item)
    {
        _item = item;
        _icon.sprite = item.icon;
    }

    public static string GetToolTipText(AItem item)
    {
        return string.IsNullOrEmpty(item.description) ? $"<b>{item.title}</b>" : $"<b>{item.title}</b>\n{item.description}";
    }

    void ShowToolTip(bool show)
    {
        _isToolTipShown = show;
        ToolTip toolTip = UIManager.instance.GetView<GameView>(ViewType.Game).toolTip;
        if (show)
        {
            toolTip.SetText(GetToolTipText(_item));
            toolTip.ShowBelow((RectTransform)transform);
        }
        else
        {
            toolTip.Show(false);
        }
    }

    void OnDisable()
    {
        // Removed while hovered: the pointer never exits it
        if (_isToolTipShown)
        {
            ShowToolTip(false);
        }
    }

    #region IPointerEnterHandler, IPointerExitHandler

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowToolTip(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ShowToolTip(false);
    }

    #endregion
}
